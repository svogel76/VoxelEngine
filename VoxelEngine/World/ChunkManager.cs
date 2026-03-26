using VoxelEngine.Core;

namespace VoxelEngine.World;

public sealed class ChunkManager : IDisposable
{
    private readonly World _world;
    private readonly EngineSettings _settings;
    private readonly ChunkWorker _worker;

    private int _lastPlayerChunkX = int.MinValue;
    private int _lastPlayerChunkZ = int.MinValue;

    private readonly HashSet<(int X, int Z)> _enqueuedChunks = new();
    private readonly HashSet<(int X, int Z)> _discardedChunks = new();
    private readonly List<(int, int)> _unloadedThisUpdate = new();
    private bool _disposed;

    public int RenderDistance { get; set; }
    public int UnloadDistance => _settings.UnloadDistance;
    public int PendingChunks => _enqueuedChunks.Count;
    public IReadOnlyList<(int chunkX, int chunkZ)> UnloadedThisUpdate => _unloadedThisUpdate;

    public ChunkManager(World world, WorldGenerator generator, EngineSettings settings)
    {
        _world = world;
        _settings = settings;
        _worker = new ChunkWorker(world, generator);
        RenderDistance = settings.RenderDistance;
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
            int dx = chunk.ChunkPosition.X - playerChunkX;
            int dz = chunk.ChunkPosition.Z - playerChunkZ;
            int dist = Math.Max(Math.Abs(dx), Math.Abs(dz));

            if (dist > UnloadDistance)
            {
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
            int dx = key.X - playerChunkX;
            int dz = key.Z - playerChunkZ;
            int dist = Math.Max(Math.Abs(dx), Math.Abs(dz));

            if (dist > UnloadDistance)
                _discardedChunks.Add(key);
        }

        if (playerChunkX == _lastPlayerChunkX && playerChunkZ == _lastPlayerChunkZ)
            return;

        _lastPlayerChunkX = playerChunkX;
        _lastPlayerChunkZ = playerChunkZ;

        for (int dx = -RenderDistance; dx <= RenderDistance; dx++)
        for (int dz = -RenderDistance; dz <= RenderDistance; dz++)
        {
            int dist = Math.Max(Math.Abs(dx), Math.Abs(dz));
            if (dist > RenderDistance)
                continue;

            int cx = playerChunkX + dx;
            int cz = playerChunkZ + dz;
            var key = (cx, cz);

            _discardedChunks.Remove(key);

            if (_world.GetChunk(cx, cz) is not null || _enqueuedChunks.Contains(key))
                continue;

            _worker.EnqueueJob(cx, cz);
            _enqueuedChunks.Add(key);
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
                _world.RemoveChunk(result.ChunkX, result.ChunkZ);
                continue;
            }

            if (!_enqueuedChunks.Remove(key))
            {
                _world.RemoveChunk(result.ChunkX, result.ChunkZ);
                continue;
            }

            return true;
        }

        result = null!;
        return false;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _worker.Dispose();
        GC.SuppressFinalize(this);
    }
}
