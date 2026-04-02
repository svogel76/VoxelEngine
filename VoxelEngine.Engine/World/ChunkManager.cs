using VoxelEngine.Core;
using VoxelEngine.Persistence;

namespace VoxelEngine.World;

public sealed class ChunkManager : IDisposable
{
    private readonly World             _world;
    private readonly EngineSettings    _settings;
    private readonly ChunkWorker       _worker;
    private readonly IWorldPersistence _persistence;

    private int _lastPlayerChunkX = int.MinValue;
    private int _lastPlayerChunkZ = int.MinValue;

    private readonly HashSet<(int X, int Z)>    _enqueuedChunks = new();
    private readonly HashSet<(int X, int Z)>    _discardedChunks = new();
    private readonly List<(int, int)>           _unloadedThisUpdate = new();
    private bool _disposed;

    public int RenderDistance  { get; set; }
    public int UnloadDistance  => _settings.UnloadDistance;
    public int PendingChunks   => _enqueuedChunks.Count;
    public IReadOnlyList<(int chunkX, int chunkZ)> UnloadedThisUpdate => _unloadedThisUpdate;

    public ChunkManager(World world, WorldGenerator generator, EngineSettings settings, IWorldPersistence persistence)
    {
        _world       = world;
        _settings    = settings;
        _persistence = persistence;
        _worker      = new ChunkWorker(world, generator, persistence, settings.MinSkyLightAmbient);
        RenderDistance = settings.RenderDistance;
    }

    public void PrimeInitialChunks(float playerWorldX, float playerWorldZ, int radius)
    {
        int playerChunkX = (int)Math.Floor(playerWorldX / Chunk.Width);
        int playerChunkZ = (int)Math.Floor(playerWorldZ / Chunk.Depth);

        foreach (var key in EnumerateChunkCoords(playerChunkX, playerChunkZ, radius))
        {
            _discardedChunks.Remove(key);

            if (_world.GetChunk(key.X, key.Z) is not null || _enqueuedChunks.Contains(key))
                continue;

            _worker.EnqueueJob(key.X, key.Z);
            _enqueuedChunks.Add(key);
        }
    }

    public void EnqueueChunkRebuild(int chunkX, int chunkZ)
    {
        var key = (chunkX, chunkZ);
        if (_world.GetChunk(chunkX, chunkZ) is null || _enqueuedChunks.Contains(key))
            return;

        _discardedChunks.Remove(key);
        _worker.EnqueueRebuild(chunkX, chunkZ);
        _enqueuedChunks.Add(key);
    }

    public void EnqueueBlockUpdate(int worldX, int worldZ)
    {
        int chunkX = World.WorldToChunk(worldX);
        int chunkZ = World.WorldToChunk(worldZ);
        int localX = World.WorldToLocal(worldX);
        int localZ = World.WorldToLocal(worldZ);

        EnqueueChunkRebuild(chunkX, chunkZ);

        if (localX == 0)
            EnqueueChunkRebuild(chunkX - 1, chunkZ);
        else if (localX == Chunk.Width - 1)
            EnqueueChunkRebuild(chunkX + 1, chunkZ);

        if (localZ == 0)
            EnqueueChunkRebuild(chunkX, chunkZ - 1);
        else if (localZ == Chunk.Depth - 1)
            EnqueueChunkRebuild(chunkX, chunkZ + 1);
    }

    public void Update(float playerWorldX, float playerWorldZ)
    {
        int playerChunkX = (int)Math.Floor(playerWorldX / Chunk.Width);
        int playerChunkZ = (int)Math.Floor(playerWorldZ / Chunk.Depth);

        _unloadedThisUpdate.Clear();

        foreach (var chunk in _world.GetAllChunks().ToList())
        {
            int dx   = chunk.ChunkPosition.X - playerChunkX;
            int dz   = chunk.ChunkPosition.Z - playerChunkZ;
            int dist = Math.Max(Math.Abs(dx), Math.Abs(dz));

            if (dist > UnloadDistance)
            {
                if (chunk.IsDirty)
                    _persistence.SaveChunkEditsAsync(chunk.ChunkPosition.X, chunk.ChunkPosition.Z, chunk.PlayerEdits)
                                .GetAwaiter().GetResult();
                _world.RemoveChunk(chunk.ChunkPosition.X, chunk.ChunkPosition.Z);
                if (_enqueuedChunks.Contains(chunk.ChunkPosition))
                    _discardedChunks.Add(chunk.ChunkPosition);
                else
                    _discardedChunks.Remove(chunk.ChunkPosition);
                _unloadedThisUpdate.Add(chunk.ChunkPosition);
            }
        }

        foreach (var key in _enqueuedChunks.ToList())
        {
            int dx   = key.X - playerChunkX;
            int dz   = key.Z - playerChunkZ;
            int dist = Math.Max(Math.Abs(dx), Math.Abs(dz));

            if (dist > UnloadDistance)
                _discardedChunks.Add(key);
        }

        if (playerChunkX == _lastPlayerChunkX && playerChunkZ == _lastPlayerChunkZ)
            return;

        _lastPlayerChunkX = playerChunkX;
        _lastPlayerChunkZ = playerChunkZ;

        int enqueuedThisUpdate = 0;
        foreach (var key in EnumerateChunkCoords(playerChunkX, playerChunkZ, RenderDistance))
        {
            if (enqueuedThisUpdate >= _settings.MaxChunksLoadedPerFrame)
                break;

            _discardedChunks.Remove(key);

            if (_world.GetChunk(key.X, key.Z) is not null || _enqueuedChunks.Contains(key))
                continue;

            _worker.EnqueueJob(key.X, key.Z);
            _enqueuedChunks.Add(key);
            enqueuedThisUpdate++;
        }
    }

    public bool TryDequeueResult(out ChunkResult result)
    {
        while (_worker.TryDequeueResult(out result))
        {
            var key = (result.ChunkX, result.ChunkZ);
            if (_discardedChunks.Remove(key))
            {
                _enqueuedChunks.Remove(key);
                if (result.Chunk.IsDirty)
                    _persistence.SaveChunkEditsAsync(result.ChunkX, result.ChunkZ, result.Chunk.PlayerEdits)
                                .GetAwaiter().GetResult();
                _world.RemoveChunk(result.ChunkX, result.ChunkZ);
                continue;
            }

            if (!_enqueuedChunks.Remove(key))
            {
                _world.RemoveChunk(result.ChunkX, result.ChunkZ);
                continue;
            }

            if (result.JobKind == ChunkJobKind.Generate)
                EnqueueNeighborRebuilds(result.ChunkX, result.ChunkZ);

            return true;
        }

        result = null!;
        return false;
    }

    private void EnqueueNeighborRebuilds(int chunkX, int chunkZ)
    {
        EnqueueChunkRebuild(chunkX - 1, chunkZ);
        EnqueueChunkRebuild(chunkX + 1, chunkZ);
        EnqueueChunkRebuild(chunkX, chunkZ - 1);
        EnqueueChunkRebuild(chunkX, chunkZ + 1);
    }
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _worker.Dispose();
        GC.SuppressFinalize(this);
    }

    private static IEnumerable<(int X, int Z)> EnumerateChunkCoords(int centerChunkX, int centerChunkZ, int radius)
    {
        return Enumerable.Range(-radius, radius * 2 + 1)
            .SelectMany(dx => Enumerable.Range(-radius, radius * 2 + 1)
                .Select(dz => (X: centerChunkX + dx, Z: centerChunkZ + dz, Dist: Math.Max(Math.Abs(dx), Math.Abs(dz)))))
            .Where(entry => entry.Dist <= radius)
            .OrderBy(entry => entry.Dist)
            .ThenBy(entry => Math.Abs(entry.X - centerChunkX) + Math.Abs(entry.Z - centerChunkZ))
            .Select(entry => (entry.X, entry.Z));
    }
}



