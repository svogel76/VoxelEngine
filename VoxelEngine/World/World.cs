namespace VoxelEngine.World;

public class World
{
    private readonly Dictionary<(int X, int Z), Chunk> _chunks = new();

    public int LoadedChunkCount => _chunks.Count;

    private static int WorldToChunk(int worldCoord)
        => (int)Math.Floor(worldCoord / (float)Chunk.Width);

    private static int WorldToLocal(int worldCoord)
        => ((worldCoord % Chunk.Width) + Chunk.Width) % Chunk.Width;

    public Chunk? GetChunk(int chunkX, int chunkZ)
        => _chunks.TryGetValue((chunkX, chunkZ), out var chunk) ? chunk : null;

    public Chunk GetOrCreateChunk(int chunkX, int chunkZ)
    {
        if (_chunks.TryGetValue((chunkX, chunkZ), out var chunk))
            return chunk;
        chunk = new Chunk(chunkX, chunkZ);
        _chunks[(chunkX, chunkZ)] = chunk;
        return chunk;
    }

    public byte GetBlock(int worldX, int worldY, int worldZ)
    {
        var chunk = GetChunk(WorldToChunk(worldX), WorldToChunk(worldZ));
        if (chunk is null)
            return BlockType.Air;
        return chunk.GetBlock(WorldToLocal(worldX), worldY, WorldToLocal(worldZ));
    }

    public void SetBlock(int worldX, int worldY, int worldZ, byte type)
    {
        var chunk = GetOrCreateChunk(WorldToChunk(worldX), WorldToChunk(worldZ));
        chunk.SetBlock(WorldToLocal(worldX), worldY, WorldToLocal(worldZ), type);
    }

    public IEnumerable<Chunk> GetAllChunks() => _chunks.Values;
}
