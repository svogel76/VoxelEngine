using System.Numerics;
using VoxelEngine.Core;
using VoxelEngine.Entity.Models;
using VoxelEngine.World;

namespace VoxelEngine.Entity;

public sealed class EntityManager
{
    private const float BoundsEpsilon = 0.001f;

    private readonly global::VoxelEngine.World.World _world;
    private readonly float _cellSize;
    private readonly EntityPhysicsSettings _physicsSettings;
    private readonly WorldGenerator? _generator;
    private readonly WorldTime? _worldTime;
    private readonly IEntityModelLibrary? _entityModels;
    private readonly Func<Vector3>? _playerPositionProvider;
    private readonly Func<ViewFrustum>? _frustumProvider;
    private readonly float _spawnRadius;
    private readonly int _spawnPlacementAttempts;
    private readonly float _despawnProtectionRadius;
    private readonly Random _spawnRandom;
    private readonly List<Entity> _entities = new();
    private readonly Dictionary<Entity, TrackedEntityState> _trackedEntities = new();
    private readonly Dictionary<(int X, int Y, int Z), HashSet<Entity>> _spatialHash = new();
    private readonly Dictionary<(string ZoneId, string EntityId), double> _spawnTimers = new();
    private readonly Dictionary<Entity, ManagedSpawnState> _managedSpawnStates = new();
    private bool? _lastIsDay;

    public int Count => _entities.Count;

    public EntityManager(
        global::VoxelEngine.World.World world,
        EngineSettings settings,
        WorldGenerator? generator = null,
        WorldTime? worldTime = null,
        IEntityModelLibrary? entityModels = null,
        Func<Vector3>? playerPositionProvider = null,
        Func<ViewFrustum>? frustumProvider = null,
        Random? random = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(settings);

        if (settings.EntitySpatialHashCellSize <= 0f)
            throw new ArgumentOutOfRangeException(nameof(settings), "Entity spatial hash cell size must be greater than zero.");
        if (settings.EntitySpawnRadius < 0f)
            throw new ArgumentOutOfRangeException(nameof(settings), "Entity spawn radius must be non-negative.");
        if (settings.EntitySpawnPlacementAttempts < 0)
            throw new ArgumentOutOfRangeException(nameof(settings), "Entity spawn placement attempts must be non-negative.");
        if (settings.EntityDespawnProtectionRadius < 0f)
            throw new ArgumentOutOfRangeException(nameof(settings), "Entity despawn protection radius must be non-negative.");

        _world = world;
        _cellSize = settings.EntitySpatialHashCellSize;
        _physicsSettings = new EntityPhysicsSettings(settings.Gravity, settings.MaxFallSpeed);
        _generator = generator;
        _worldTime = worldTime;
        _entityModels = entityModels;
        _playerPositionProvider = playerPositionProvider;
        _frustumProvider = frustumProvider;
        _spawnRadius = settings.EntitySpawnRadius;
        _spawnPlacementAttempts = settings.EntitySpawnPlacementAttempts;
        _despawnProtectionRadius = settings.EntityDespawnProtectionRadius;
        _spawnRandom = random ?? new Random(settings.Terrain.Seed ^ 0x5F3759DF);
        _lastIsDay = worldTime?.IsDay;
    }

    public T Create<T>(Func<T> factory) where T : Entity
    {
        ArgumentNullException.ThrowIfNull(factory);

        var entity = factory();
        Add(entity);
        return entity;
    }

    public bool Add(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (_trackedEntities.ContainsKey(entity))
            return false;

        var state = CreateTrackedState(entity);
        _entities.Add(entity);
        _trackedEntities.Add(entity, state);
        AddToSpatialHash(entity, state.Cells);
        return true;
    }

    public bool Remove(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (!_trackedEntities.TryGetValue(entity, out var state))
            return false;

        _trackedEntities.Remove(entity);
        RemoveFromSpatialHash(entity, state.Cells);
        _entities.Remove(entity);
        _managedSpawnStates.Remove(entity);
        return true;
    }

    public IReadOnlyList<Entity> GetAll()
        => _entities;

    public IReadOnlyList<T> GetAll<T>() where T : Entity
        => _entities.OfType<T>().ToList();

    public IReadOnlyList<Entity> GetNearby(Vector3 position, float radius)
    {
        if (radius < 0f)
            throw new ArgumentOutOfRangeException(nameof(radius));

        RefreshAllTrackedEntities();

        var result = new List<Entity>();
        var candidates = new HashSet<Entity>();

        foreach (var cell in GetCellsForSphere(position, radius))
        {
            if (!_spatialHash.TryGetValue(cell, out var bucket))
                continue;

            foreach (var entity in bucket)
                candidates.Add(entity);
        }

        foreach (var entity in candidates)
        {
            var bounds = _trackedEntities[entity].Bounds;
            if (IntersectsSphere(bounds, position, radius))
                result.Add(entity);
        }

        return result;
    }

    public IReadOnlyList<T> GetNearby<T>(Vector3 position, float radius) where T : Entity
        => GetNearby(position, radius).OfType<T>().ToList();

    public IReadOnlyList<Entity> GetVisible(ViewFrustum frustum)
    {
        ArgumentNullException.ThrowIfNull(frustum);

        RefreshAllTrackedEntities();

        var visible = new List<Entity>();
        foreach (var entity in _entities)
        {
            if (frustum.IsVisible(_trackedEntities[entity].Bounds))
                visible.Add(entity);
        }

        return visible;
    }

    public void Update(double deltaTime)
    {
        foreach (var entity in _entities)
        {
            if (IsInLoadedChunk(entity))
            {
                if (entity is TerrainPhysicsEntity terrainPhysicsEntity)
                    terrainPhysicsEntity.ApplyTerrainPhysics(_world, _physicsSettings, deltaTime);

                if (entity is IEntityUpdatable updatable)
                    updatable.Update(deltaTime);
            }

            RefreshTrackedEntity(entity);
        }

        HandleDayNightTransition();
        RunClimateSpawning(deltaTime);
    }

    private void RunClimateSpawning(double deltaTime)
    {
        if (_generator is null ||
            _worldTime is null ||
            _entityModels is null ||
            _playerPositionProvider is null ||
            _spawnPlacementAttempts == 0 ||
            _spawnRadius <= 0f)
        {
            return;
        }

        foreach (var zone in _generator.ClimateZones)
        foreach (var spawn in zone.Spawns)
        {
            if (!IsActivityActive(spawn.Activity, _worldTime))
            {
                if (spawn.SpawnInterval > 0f)
                {
                    var inactiveKey = (zone.Id, spawn.EntityId);
                    double inactiveTimer = _spawnTimers.GetValueOrDefault(inactiveKey);
                    _spawnTimers[inactiveKey] = Math.Min(inactiveTimer + deltaTime, spawn.SpawnInterval);
                }

                continue;
            }

            if (spawn.SpawnInterval <= 0f)
            {
                TrySpawnEntity(zone, spawn, _playerPositionProvider());
                continue;
            }

            var key = (zone.Id, spawn.EntityId);
            double timer = _spawnTimers.GetValueOrDefault(key);
            timer = Math.Min(timer + deltaTime, spawn.SpawnInterval);
            _spawnTimers[key] = timer;

            if (timer < spawn.SpawnInterval)
                continue;

            TrySpawnEntity(zone, spawn, _playerPositionProvider());
            _spawnTimers[key] = 0d;
        }
    }

    private void TrySpawnEntity(ClimateZone zone, ClimateSpawnDefinition spawn, Vector3 playerPosition)
    {
        if (CountManagedSpawns(zone.Id, spawn.EntityId) >= spawn.MaxCount)
            return;

        IVoxelModelDefinition model;
        try
        {
            model = _entityModels!.GetModel(spawn.EntityId);
        }
        catch (KeyNotFoundException)
        {
            return;
        }

        if (model.Metadata.Behaviour is null)
            return;

        if (!TryFindSpawnPosition(zone, spawn, model, playerPosition, out var spawnPosition))
            return;

        var entity = new AnimalEntity(
            spawnPosition,
            model,
            _world,
            _playerPositionProvider,
            yawRadians: 0f,
            random: new Random(_spawnRandom.Next()));

        entity.SyncTerrainPhysics(_world);
        Add(entity);
        _managedSpawnStates[entity] = new ManagedSpawnState(zone.Id, spawn.EntityId, spawn.Activity);
    }

    private bool TryFindSpawnPosition(
        ClimateZone zone,
        ClimateSpawnDefinition spawn,
        IVoxelModelDefinition model,
        Vector3 playerPosition,
        out Vector3 spawnPosition)
    {
        spawnPosition = default;

        var candidateChunks = _world.GetAllChunks()
            .Where(chunk =>
            {
                float chunkCenterX = chunk.ChunkPosition.X * Chunk.Width + Chunk.Width * 0.5f;
                float chunkCenterZ = chunk.ChunkPosition.Z * Chunk.Depth + Chunk.Depth * 0.5f;
                Vector2 offset = new(chunkCenterX - playerPosition.X, chunkCenterZ - playerPosition.Z);
                return offset.Length() <= _spawnRadius + Chunk.Width;
            })
            .ToArray();

        if (candidateChunks.Length == 0)
            return false;

        float modelHeight = model.PlacementBounds.Max.Y - model.PlacementBounds.Min.Y;
        int requiredHeadroom = Math.Max(1, (int)MathF.Ceiling(modelHeight));

        for (int attempt = 0; attempt < _spawnPlacementAttempts; attempt++)
        {
            Chunk chunk = candidateChunks[_spawnRandom.Next(candidateChunks.Length)];
            int worldX = chunk.ChunkPosition.X * Chunk.Width + _spawnRandom.Next(Chunk.Width);
            int worldZ = chunk.ChunkPosition.Z * Chunk.Depth + _spawnRandom.Next(Chunk.Depth);

            ClimateSample sample = _generator!.SampleClimate(worldX, worldZ);
            if (!string.Equals(sample.PrimaryZone.Id, zone.Id, StringComparison.OrdinalIgnoreCase))
                continue;

            Vector2 horizontalOffset = new(worldX + 0.5f - playerPosition.X, worldZ + 0.5f - playerPosition.Z);
            if (horizontalOffset.Length() < spawn.MinSpawnDistance || horizontalOffset.Length() > _spawnRadius)
                continue;

            int surfaceY = _generator.GetSurfaceHeight(worldX, worldZ);
            if (surfaceY < 1 || surfaceY >= Chunk.Height - requiredHeadroom - 1)
                continue;

            byte groundBlock = _world.GetBlock(worldX, surfaceY, worldZ);
            if (!BlockRegistry.IsSolid(groundBlock) || groundBlock == BlockType.Water)
                continue;

            if (!HasClearance(worldX, surfaceY, worldZ, requiredHeadroom))
                continue;

            spawnPosition = new Vector3(worldX + 0.5f, surfaceY + 1f, worldZ + 0.5f);
            if (GetNearby(spawnPosition, 1.5f).Count > 0)
                continue;

            return true;
        }

        return false;
    }

    private bool HasClearance(int worldX, int surfaceY, int worldZ, int requiredHeadroom)
    {
        for (int step = 1; step <= requiredHeadroom; step++)
        {
            byte block = _world.GetBlock(worldX, surfaceY + step, worldZ);
            if (block != BlockType.Air)
                return false;
        }

        return true;
    }

    private int CountManagedSpawns(string zoneId, string entityId)
        => _managedSpawnStates.Values.Count(state =>
            string.Equals(state.ZoneId, zoneId, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(state.EntityId, entityId, StringComparison.OrdinalIgnoreCase));

    private void HandleDayNightTransition()
    {
        if (_worldTime is null || _playerPositionProvider is null)
            return;

        bool isDay = _worldTime.IsDay;
        if (_lastIsDay is null)
        {
            _lastIsDay = isDay;
            return;
        }

        if (_lastIsDay == isDay)
            return;

        _lastIsDay = isDay;
        ViewFrustum? frustum = _frustumProvider?.Invoke();
        Vector3 playerPosition = _playerPositionProvider();

        foreach (var entity in _managedSpawnStates.Keys.ToArray())
        {
            ManagedSpawnState state = _managedSpawnStates[entity];
            if (IsActivityActive(state.Activity, _worldTime))
                continue;

            BoundingBox bounds = _trackedEntities[entity].Bounds;
            bool isVisible = frustum is not null && frustum.IsVisible(bounds);
            bool isProtected = IntersectsSphere(bounds, playerPosition, _despawnProtectionRadius);
            if (isVisible || isProtected)
                continue;

            Remove(entity);
        }
    }

    private static bool IsActivityActive(SpawnActivity activity, WorldTime worldTime)
        => activity switch
        {
            SpawnActivity.Any => true,
            SpawnActivity.Diurnal => worldTime.IsDay,
            SpawnActivity.Nocturnal => worldTime.IsNight,
            _ => true
        };

    private void RefreshAllTrackedEntities()
    {
        foreach (var entity in _entities)
            RefreshTrackedEntity(entity);
    }

    private void RefreshTrackedEntity(Entity entity)
    {
        var currentState = _trackedEntities[entity];
        var nextState = CreateTrackedState(entity);

        if (currentState.Bounds.Equals(nextState.Bounds) && currentState.Cells.SetEquals(nextState.Cells))
            return;

        RemoveFromSpatialHash(entity, currentState.Cells);
        AddToSpatialHash(entity, nextState.Cells);
        _trackedEntities[entity] = nextState;
    }

    private bool IsInLoadedChunk(Entity entity)
    {
        var bounds = _trackedEntities.TryGetValue(entity, out var state)
            ? state.Bounds
            : GetEntityBounds(entity);

        int minChunkX = global::VoxelEngine.World.World.WorldToChunk((int)MathF.Floor(bounds.Min.X));
        int maxChunkX = global::VoxelEngine.World.World.WorldToChunk((int)MathF.Floor(GetUpperBoundCoordinate(bounds.Max.X, bounds.Min.X)));
        int minChunkZ = global::VoxelEngine.World.World.WorldToChunk((int)MathF.Floor(bounds.Min.Z));
        int maxChunkZ = global::VoxelEngine.World.World.WorldToChunk((int)MathF.Floor(GetUpperBoundCoordinate(bounds.Max.Z, bounds.Min.Z)));

        for (int chunkX = minChunkX; chunkX <= maxChunkX; chunkX++)
        for (int chunkZ = minChunkZ; chunkZ <= maxChunkZ; chunkZ++)
        {
            if (_world.GetChunk(chunkX, chunkZ) is not null)
                return true;
        }

        return false;
    }

    private TrackedEntityState CreateTrackedState(Entity entity)
    {
        var bounds = GetEntityBounds(entity);
        var cells = GetCellsForBounds(bounds);
        return new TrackedEntityState(bounds, cells);
    }

    private static BoundingBox GetEntityBounds(Entity entity)
    {
        if (entity is IEntityBoundsProvider boundsProvider)
            return boundsProvider.Bounds;

        return new BoundingBox(entity.Position, entity.Position);
    }

    private HashSet<(int X, int Y, int Z)> GetCellsForBounds(BoundingBox bounds)
    {
        int minX = ToCell(bounds.Min.X);
        int maxX = ToCell(GetUpperBoundCoordinate(bounds.Max.X, bounds.Min.X));
        int minY = ToCell(bounds.Min.Y);
        int maxY = ToCell(GetUpperBoundCoordinate(bounds.Max.Y, bounds.Min.Y));
        int minZ = ToCell(bounds.Min.Z);
        int maxZ = ToCell(GetUpperBoundCoordinate(bounds.Max.Z, bounds.Min.Z));

        var cells = new HashSet<(int X, int Y, int Z)>();
        for (int x = minX; x <= maxX; x++)
        for (int y = minY; y <= maxY; y++)
        for (int z = minZ; z <= maxZ; z++)
            cells.Add((x, y, z));

        return cells;
    }

    private IEnumerable<(int X, int Y, int Z)> GetCellsForSphere(Vector3 position, float radius)
    {
        int minX = ToCell(position.X - radius);
        int maxX = ToCell(position.X + radius);
        int minY = ToCell(position.Y - radius);
        int maxY = ToCell(position.Y + radius);
        int minZ = ToCell(position.Z - radius);
        int maxZ = ToCell(position.Z + radius);

        for (int x = minX; x <= maxX; x++)
        for (int y = minY; y <= maxY; y++)
        for (int z = minZ; z <= maxZ; z++)
            yield return (x, y, z);
    }

    private int ToCell(float coordinate)
        => (int)MathF.Floor(coordinate / _cellSize);

    private void AddToSpatialHash(Entity entity, HashSet<(int X, int Y, int Z)> cells)
    {
        foreach (var cell in cells)
        {
            if (!_spatialHash.TryGetValue(cell, out var bucket))
            {
                bucket = new HashSet<Entity>();
                _spatialHash.Add(cell, bucket);
            }

            bucket.Add(entity);
        }
    }

    private void RemoveFromSpatialHash(Entity entity, HashSet<(int X, int Y, int Z)> cells)
    {
        foreach (var cell in cells)
        {
            if (!_spatialHash.TryGetValue(cell, out var bucket))
                continue;

            bucket.Remove(entity);
            if (bucket.Count == 0)
                _spatialHash.Remove(cell);
        }
    }

    private static bool IntersectsSphere(BoundingBox bounds, Vector3 center, float radius)
    {
        float dx = center.X < bounds.Min.X ? bounds.Min.X - center.X : center.X > bounds.Max.X ? center.X - bounds.Max.X : 0f;
        float dy = center.Y < bounds.Min.Y ? bounds.Min.Y - center.Y : center.Y > bounds.Max.Y ? center.Y - bounds.Max.Y : 0f;
        float dz = center.Z < bounds.Min.Z ? bounds.Min.Z - center.Z : center.Z > bounds.Max.Z ? center.Z - bounds.Max.Z : 0f;

        float distanceSquared = dx * dx + dy * dy + dz * dz;
        return distanceSquared <= radius * radius;
    }

    private static float GetUpperBoundCoordinate(float max, float min)
        => max > min ? max - BoundsEpsilon : max;

    private readonly record struct TrackedEntityState(BoundingBox Bounds, HashSet<(int X, int Y, int Z)> Cells);
    private readonly record struct ManagedSpawnState(string ZoneId, string EntityId, SpawnActivity Activity);
}
