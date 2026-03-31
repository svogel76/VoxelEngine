using System.Numerics;
using VoxelEngine.Core;
using VoxelEngine.Entity.Models;
using VoxelEngine.Entity.Spawning;
using VoxelEngine.World;

namespace VoxelEngine.Entity;

public sealed class EntityManager
{
    private const float BoundsEpsilon = 0.001f;

    private readonly global::VoxelEngine.World.World _world;
    private readonly float _cellSize;
    private readonly EntityPhysicsSettings _physicsSettings;
    private readonly List<Entity> _entities = new();
    private readonly Dictionary<Entity, TrackedEntityState> _trackedEntities = new();
    private readonly Dictionary<(int X, int Y, int Z), HashSet<Entity>> _spatialHash = new();

    public SpawnManager? SpawnManager { get; set; }
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

        _ = generator;
        _ = worldTime;
        _ = entityModels;
        _ = playerPositionProvider;
        _ = frustumProvider;
        _ = random;

        _world = world;
        _cellSize = settings.EntitySpatialHashCellSize;
        _physicsSettings = new EntityPhysicsSettings(settings.Gravity, settings.MaxFallSpeed);
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
        foreach (var entity in _entities.ToArray())
        {
            if (IsInLoadedChunk(entity))
            {
                if (entity is TerrainPhysicsEntity terrainPhysicsEntity)
                    terrainPhysicsEntity.ApplyTerrainPhysics(_world, _physicsSettings, deltaTime);

                if (entity is IEntityUpdatable updatable)
                    updatable.Update(deltaTime);
            }

            if (_trackedEntities.ContainsKey(entity))
                RefreshTrackedEntity(entity);
        }

        SpawnManager?.Tick(deltaTime);
    }

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
}
