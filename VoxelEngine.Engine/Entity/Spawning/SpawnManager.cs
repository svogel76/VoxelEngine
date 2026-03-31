using System.Numerics;
using VoxelEngine.Core;
using VoxelEngine.Entity.Components;
using VoxelEngine.Entity.Models;
using VoxelEngine.World;

namespace VoxelEngine.Entity.Spawning;

public sealed class SpawnManager
{
    private readonly global::VoxelEngine.World.World _world;
    private readonly EntityManager _entityManager;
    private readonly WorldGenerator _generator;
    private readonly WorldTime _worldTime;
    private readonly IEntityModelLibrary _entityModels;
    private readonly Func<Vector3> _playerPositionProvider;
    private readonly Func<ViewFrustum>? _frustumProvider;
    private readonly EngineSettings _settings;
    private readonly float _maxSpawnDistance;
    private readonly float _spawnTickInterval;
    private readonly int _spawnPlacementAttempts;
    private readonly float _despawnProtectionRadius;
    private readonly Random _random;
    private readonly Dictionary<(string ZoneId, string EntityId), double> _spawnTimers = new();
    private readonly Dictionary<Entity, ManagedSpawnState> _managedSpawnStates = new();
    private bool? _lastIsDay;
    private double _spawnTickAccumulator;

    public SpawnManager(
        global::VoxelEngine.World.World world,
        EntityManager entityManager,
        EngineSettings settings,
        WorldGenerator generator,
        WorldTime worldTime,
        IEntityModelLibrary entityModels,
        Func<Vector3> playerPositionProvider,
        Func<ViewFrustum>? frustumProvider = null,
        Random? random = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(entityManager);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(generator);
        ArgumentNullException.ThrowIfNull(worldTime);
        ArgumentNullException.ThrowIfNull(entityModels);
        ArgumentNullException.ThrowIfNull(playerPositionProvider);

        if (settings.MaxSpawnDistance < 0f)
            throw new ArgumentOutOfRangeException(nameof(settings), "Maximum spawn distance must be non-negative.");
        if (settings.SpawnTickInterval < 0f)
            throw new ArgumentOutOfRangeException(nameof(settings), "Spawn tick interval must be non-negative.");
        if (settings.EntitySpawnPlacementAttempts < 0)
            throw new ArgumentOutOfRangeException(nameof(settings), "Entity spawn placement attempts must be non-negative.");
        if (settings.EntityDespawnProtectionRadius < 0f)
            throw new ArgumentOutOfRangeException(nameof(settings), "Entity despawn protection radius must be non-negative.");

        _world                  = world;
        _entityManager          = entityManager;
        _settings               = settings;
        _generator              = generator;
        _worldTime              = worldTime;
        _entityModels           = entityModels;
        _playerPositionProvider = playerPositionProvider;
        _frustumProvider        = frustumProvider;
        _maxSpawnDistance       = settings.MaxSpawnDistance;
        _spawnTickInterval      = settings.SpawnTickInterval;
        _spawnPlacementAttempts = settings.EntitySpawnPlacementAttempts;
        _despawnProtectionRadius = settings.EntityDespawnProtectionRadius;
        _random                 = random ?? new Random(settings.Terrain.Seed ^ 0x5F3759DF);
        _lastIsDay              = worldTime.IsDay;
    }

    public void Tick(double deltaTime)
    {
        HandleDayNightTransition();
        HandleDeferredBurrowTransitions();

        if (_spawnPlacementAttempts == 0 || _maxSpawnDistance <= 0f)
            return;

        if (_spawnTickInterval <= 0f)
        {
            RunPeriodicChecks(deltaTime);
            return;
        }

        _spawnTickAccumulator += deltaTime;
        while (_spawnTickAccumulator >= _spawnTickInterval)
        {
            RunPeriodicChecks(_spawnTickInterval);
            _spawnTickAccumulator -= _spawnTickInterval;
        }
    }

    private void RunPeriodicChecks(double deltaTime)
    {
        Vector3 playerPosition = _playerPositionProvider();
        ViewFrustum? frustum   = _frustumProvider?.Invoke();

        DespawnDistantEntities(playerPosition, frustum);

        foreach (var zone in _generator.ClimateZones)
        foreach (var spawn in zone.Spawns)
        {
            if (!IsActivityActive(spawn.Activity, _worldTime))
                continue;

            if (!AdvanceSpawnTimer(zone.Id, spawn.EntityId, deltaTime, spawn.SpawnInterval))
                continue;

            TrySpawnEntity(zone, spawn, playerPosition, frustum);
        }
    }

    private bool AdvanceSpawnTimer(string zoneId, string entityId, double deltaTime, float spawnInterval)
    {
        if (spawnInterval <= 0f)
            return true;

        var key   = (zoneId, entityId);
        double timer = _spawnTimers.GetValueOrDefault(key) + deltaTime;
        if (timer < spawnInterval)
        {
            _spawnTimers[key] = timer;
            return false;
        }

        _spawnTimers[key] = 0d;
        return true;
    }

    private void TrySpawnEntity(ClimateZone zone, ClimateSpawnDefinition spawn, Vector3 playerPosition, ViewFrustum? frustum)
    {
        if (CountManagedSpawns(zone.Id, spawn.EntityId) >= spawn.MaxCount)
            return;

        IVoxelModelDefinition model;
        try
        {
            model = _entityModels.GetModel(spawn.EntityId);
        }
        catch (KeyNotFoundException)
        {
            return;
        }

        if (model.Metadata.Behaviour is null)
            return;
        if (GetTimeOfDayActivity(model.Metadata.Behaviour, _worldTime) == EntityTimeOfDayActivity.Burrow)
            return;

        if (!TryFindSpawnPosition(zone, spawn, model, playerPosition, frustum, out var spawnPosition))
            return;

        var entity = BuildEntityFromModel(model, spawnPosition, new Random(_random.Next()));
        entity.GetComponent<AIComponent>()?.ApplyTimeOfDay(_worldTime.IsDay);
        entity.GetComponent<PhysicsComponent>()?.SyncPhysics(entity);

        _entityManager.Add(entity);
        _managedSpawnStates[entity] = new ManagedSpawnState(zone.Id, spawn.EntityId);
    }

    private Entity BuildEntityFromModel(IVoxelModelDefinition model, Vector3 spawnPosition, Random random)
    {
        var entity = new Entity(model.Id, spawnPosition);

        // PhysicsComponent
        float width  = model.PlacementBounds.Max.X - model.PlacementBounds.Min.X;
        float height = model.PlacementBounds.Max.Y - model.PlacementBounds.Min.Y;
        var phys = new PhysicsComponent(
            _world,
            width,
            height,
            _settings.Gravity,
            _settings.MaxFallSpeed,
            _settings.FallDamageThreshold,
            _settings.FallDamageMultiplier);
        entity.AddComponent(phys);

        // AIComponent (needs PhysicsComponent first so Update order is correct)
        if (model.Metadata.Behaviour is not null)
        {
            var ai = new AIComponent(
                _world,
                model.Metadata.Behaviour,
                _playerPositionProvider,
                yawRadians: 0f,
                random: random);
            entity.AddComponent(ai);
        }

        // HealthComponent
        entity.AddComponent(new HealthComponent(model.Metadata.Behaviour is not null ? 8f : 1f));

        // DropComponent
        if (model.Metadata.Drops is { Count: > 0 })
        {
            var drops = model.Metadata.Drops
                .Select(d => new DropEntry(d.Item ?? "", d.Count ?? 1, 1f))
                .ToList();
            entity.AddComponent(new DropComponent(drops));
        }

        // RenderComponent
        entity.AddComponent(new RenderComponent(model.Id, model.Metadata.Display?.Scale ?? 1f));

        entity.IsActive = true;
        return entity;
    }

    private bool TryFindSpawnPosition(
        ClimateZone zone,
        ClimateSpawnDefinition spawn,
        IVoxelModelDefinition model,
        Vector3 playerPosition,
        ViewFrustum? frustum,
        out Vector3 spawnPosition)
    {
        spawnPosition = default;

        float minDistance        = Math.Clamp(spawn.MinSpawnDistance, 0f, _maxSpawnDistance);
        float minDistanceSquared = minDistance * minDistance;
        float maxDistanceSquared = _maxSpawnDistance * _maxSpawnDistance;
        float modelHeight        = model.PlacementBounds.Max.Y - model.PlacementBounds.Min.Y;
        int   requiredHeadroom   = Math.Max(1, (int)MathF.Ceiling(modelHeight));

        for (int attempt = 0; attempt < _spawnPlacementAttempts; attempt++)
        {
            Vector2 offset = SampleRingOffset(minDistanceSquared, maxDistanceSquared);
            int worldX = (int)MathF.Floor(playerPosition.X + offset.X);
            int worldZ = (int)MathF.Floor(playerPosition.Z + offset.Y);
            int chunkX = global::VoxelEngine.World.World.WorldToChunk(worldX);
            int chunkZ = global::VoxelEngine.World.World.WorldToChunk(worldZ);

            if (_world.GetChunk(chunkX, chunkZ) is null)
                continue;

            ClimateSample sample = _generator.SampleClimate(worldX, worldZ);
            if (!string.Equals(sample.PrimaryZone.Id, zone.Id, StringComparison.OrdinalIgnoreCase))
                continue;

            int surfaceY = _generator.GetSurfaceHeight(worldX, worldZ);
            if (surfaceY < 1 || surfaceY >= Chunk.Height - requiredHeadroom - 1)
                continue;

            byte groundBlock = _world.GetBlock(worldX, surfaceY, worldZ);
            if (!BlockRegistry.IsSolid(groundBlock) || groundBlock == BlockType.Water)
                continue;

            if (!HasClearance(worldX, surfaceY, worldZ, requiredHeadroom))
                continue;

            Vector3 candidatePosition = new(worldX + 0.5f, surfaceY + 1f, worldZ + 0.5f);
            if (_entityManager.GetNearby(candidatePosition, 1.5f).Count > 0)
                continue;

            BoundingBox candidateBounds = model.PlacementBounds.Translate(candidatePosition.X, candidatePosition.Y, candidatePosition.Z);
            if (frustum is not null && frustum.IsVisible(candidateBounds))
                continue;

            spawnPosition = candidatePosition;
            return true;
        }

        return false;
    }

    private Vector2 SampleRingOffset(float minDistanceSquared, float maxDistanceSquared)
    {
        float angle         = _random.NextSingle() * MathF.Tau;
        float radiusSquared = minDistanceSquared + (maxDistanceSquared - minDistanceSquared) * _random.NextSingle();
        float radius        = MathF.Sqrt(radiusSquared);
        return new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
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

    private void DespawnDistantEntities(Vector3 playerPosition, ViewFrustum? frustum)
    {
        float despawnDistance        = _maxSpawnDistance + _despawnProtectionRadius;
        float despawnDistanceSquared = despawnDistance * despawnDistance;

        foreach (var entity in _managedSpawnStates.Keys.ToArray())
        {
            if (!ShouldDespawn(entity, playerPosition, frustum, despawnDistanceSquared))
                continue;

            _managedSpawnStates.Remove(entity);
            _entityManager.Remove(entity);
        }
    }

    private bool ShouldDespawn(Entity entity, Vector3 playerPosition, ViewFrustum? frustum, float despawnDistanceSquared)
    {
        BoundingBox bounds = GetEntityBounds(entity);
        Vector3 center = new(
            (bounds.Min.X + bounds.Max.X) * 0.5f,
            (bounds.Min.Y + bounds.Max.Y) * 0.5f,
            (bounds.Min.Z + bounds.Max.Z) * 0.5f);

        Vector3 offset = center - playerPosition;
        float horizontalDistanceSquared = offset.X * offset.X + offset.Z * offset.Z;
        if (horizontalDistanceSquared <= despawnDistanceSquared)
            return false;

        if (frustum is not null && frustum.IsVisible(bounds))
            return false;

        return !IntersectsSphere(bounds, playerPosition, _despawnProtectionRadius);
    }

    private void HandleDayNightTransition()
    {
        bool isDay = _worldTime.IsDay;
        if (_lastIsDay is null) { _lastIsDay = isDay; return; }
        if (_lastIsDay == isDay) return;

        _lastIsDay = isDay;
        ViewFrustum? frustum       = _frustumProvider?.Invoke();
        Vector3 playerPosition     = _playerPositionProvider();

        foreach (var entity in _managedSpawnStates.Keys.ToArray())
            ApplyManagedEntityTimeOfDay(entity, frustum, playerPosition, isDay);
    }

    private void HandleDeferredBurrowTransitions()
    {
        ViewFrustum? frustum   = _frustumProvider?.Invoke();
        Vector3 playerPosition = _playerPositionProvider();

        foreach (var entity in _managedSpawnStates.Keys.ToArray())
        {
            var ai = entity.GetComponent<AIComponent>();
            if (ai is null) continue;
            if (ai.GetTimeOfDayActivity(_worldTime.IsDay) != EntityTimeOfDayActivity.Burrow)
                continue;

            ApplyManagedEntityTimeOfDay(entity, frustum, playerPosition, _worldTime.IsDay);
        }
    }

    private void ApplyManagedEntityTimeOfDay(Entity entity, ViewFrustum? frustum, Vector3 playerPosition, bool isDay)
    {
        var ai = entity.GetComponent<AIComponent>();
        if (ai is null) return;

        EntityTimeOfDayActivity desiredActivity = ai.GetTimeOfDayActivity(isDay);
        if (desiredActivity == EntityTimeOfDayActivity.Burrow)
        {
            BoundingBox bounds    = GetEntityBounds(entity);
            bool        isVisible = frustum is not null && frustum.IsVisible(bounds);
            bool        isProtected = IntersectsSphere(bounds, playerPosition, _despawnProtectionRadius);
            if (isVisible || isProtected) return;

            ai.ApplyTimeOfDay(isDay);
            _managedSpawnStates.Remove(entity);
            _entityManager.Remove(entity);
            return;
        }

        ai.ApplyTimeOfDay(isDay);
    }

    private static BoundingBox GetEntityBounds(Entity entity)
    {
        var phys = entity.GetComponent<PhysicsComponent>();
        if (phys is not null) return phys.Bounds;
        return new BoundingBox(entity.InternalPosition, entity.InternalPosition);
    }

    private static EntityTimeOfDayActivity GetTimeOfDayActivity(EntityBehaviourMetadata behaviour, WorldTime worldTime)
        => worldTime.IsDay ? behaviour.DayActivity : behaviour.NightActivity;

    private static bool IsActivityActive(SpawnActivity activity, WorldTime worldTime)
        => activity switch
        {
            SpawnActivity.Any      => true,
            SpawnActivity.Diurnal  => worldTime.IsDay,
            SpawnActivity.Nocturnal => worldTime.IsNight,
            _                      => true
        };

    private static bool IntersectsSphere(BoundingBox bounds, Vector3 center, float radius)
    {
        float dx = center.X < bounds.Min.X ? bounds.Min.X - center.X : center.X > bounds.Max.X ? center.X - bounds.Max.X : 0f;
        float dy = center.Y < bounds.Min.Y ? bounds.Min.Y - center.Y : center.Y > bounds.Max.Y ? center.Y - bounds.Max.Y : 0f;
        float dz = center.Z < bounds.Min.Z ? bounds.Min.Z - center.Z : center.Z > bounds.Max.Z ? center.Z - bounds.Max.Z : 0f;
        return dx * dx + dy * dy + dz * dz <= radius * radius;
    }

    private readonly record struct ManagedSpawnState(string ZoneId, string EntityId);
}


