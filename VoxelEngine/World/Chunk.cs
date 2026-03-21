namespace VoxelEngine.World;

public class Chunk
{
    public const int Width  = 16;
    public const int Height = 256;
    public const int Depth  = 16;

    private readonly byte[,,] _blocks = new byte[Width, Height, Depth];

    public (int X, int Z) ChunkPosition { get; }

    public Chunk(int x, int z)
    {
        ChunkPosition = (x, z);
    }

    public byte GetBlock(int x, int y, int z)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height || z < 0 || z >= Depth)
            return BlockType.Air;
        return _blocks[x, y, z];
    }

    public void SetBlock(int x, int y, int z, byte type)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height || z < 0 || z >= Depth)
            return;
        _blocks[x, y, z] = type;
    }

    public bool IsEmpty()
    {
        for (int x = 0; x < Width;  x++)
        for (int y = 0; y < Height; y++)
        for (int z = 0; z < Depth;  z++)
        {
            if (_blocks[x, y, z] != BlockType.Air)
                return false;
        }
        return true;
    }
}
