using System.Collections.Concurrent;

namespace VoxelEngine.World;

public class World : IWorldAccess
{
    private readonly ConcurrentDictionary<(int X, int Z), Chunk> _chunks = new();

    public int LoadedChunkCount => _chunks.Count;

    public static int WorldToChunk(int worldCoord)
        => (int)Math.Floor(worldCoord / (float)Chunk.Width);

    public static int WorldToLocal(int worldCoord)
        => ((worldCoord % Chunk.Width) + Chunk.Width) % Chunk.Width;

    public Chunk? GetChunk(int chunkX, int chunkZ)
        => _chunks.TryGetValue((chunkX, chunkZ), out var chunk) ? chunk : null;

    public Chunk GetOrCreateChunk(int chunkX, int chunkZ)
        => _chunks.GetOrAdd((chunkX, chunkZ), key => new Chunk(key.X, key.Z));

    public void AddChunk(Chunk chunk) =>
        _chunks.TryAdd((chunk.ChunkPosition.X, chunk.ChunkPosition.Z), chunk);

    public byte GetBlock(int worldX, int worldY, int worldZ)
    {
        var chunk = GetChunk(WorldToChunk(worldX), WorldToChunk(worldZ));
        if (chunk is null)
            return BlockType.Air;
        return chunk.GetBlock(WorldToLocal(worldX), worldY, WorldToLocal(worldZ));
    }

    public byte GetSkyLight(int worldX, int worldY, int worldZ)
    {
        var chunk = GetChunk(WorldToChunk(worldX), WorldToChunk(worldZ));
        if (chunk is null)
            return 0;

        return chunk.GetSkyLight(WorldToLocal(worldX), worldY, WorldToLocal(worldZ));
    }

    public void SetBlock(int worldX, int worldY, int worldZ, byte type)
    {
        int localX = WorldToLocal(worldX);
        int localZ = WorldToLocal(worldZ);
        var chunk = GetOrCreateChunk(WorldToChunk(worldX), WorldToChunk(worldZ));
        chunk.SetBlock(localX, worldY, localZ, type);
        chunk.RecordEdit(localX, worldY, localZ, type);
    }

    public void RemoveChunk(int chunkX, int chunkZ)
        => _chunks.TryRemove((chunkX, chunkZ), out _);

    public IEnumerable<Chunk> GetAllChunks() => _chunks.Values;
}
