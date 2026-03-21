using VoxelEngine.World;

namespace VoxelEngine.Rendering;

public static class ChunkMeshBuilder
{
    private static readonly (int X, int Y, int Z)[] Directions =
    [
        ( 0,  1,  0),  // Top
        ( 0, -1,  0),  // Bottom
        ( 0,  0,  1),  // Front
        ( 0,  0, -1),  // Back
        ( 1,  0,  0),  // Right
        (-1,  0,  0),  // Left
    ];

    // 4 vertex offsets per face × 6 faces  (order: Top, Bottom, Front, Back, Right, Left)
    private static readonly (int Ox, int Oy, int Oz)[,] FaceOffsets =
    {
        { (0,1,1),(1,1,1),(1,1,0),(0,1,0) },  // Top
        { (0,0,0),(1,0,0),(1,0,1),(0,0,1) },  // Bottom
        { (0,0,1),(1,0,1),(1,1,1),(0,1,1) },  // Front
        { (1,0,0),(0,0,0),(0,1,0),(1,1,0) },  // Back
        { (1,0,1),(1,0,0),(1,1,0),(1,1,1) },  // Right
        { (0,0,0),(0,0,1),(0,1,1),(0,1,0) },  // Left
    };

    private static readonly (float U, float V)[] UVs =
        [(0f, 0f), (1f, 0f), (1f, 1f), (0f, 1f)];

    public static (float[] vertices, uint[] indices) Build(Chunk chunk, World.World world)
    {
        var vertices = new List<float>();
        var indices  = new List<uint>();

        for (int x = 0; x < Chunk.Width;  x++)
        for (int y = 0; y < Chunk.Height; y++)
        for (int z = 0; z < Chunk.Depth;  z++)
        {
            if (chunk.GetBlock(x, y, z) == BlockType.Air)
                continue;

            int worldX = chunk.ChunkPosition.X * Chunk.Width + x;
            int worldZ = chunk.ChunkPosition.Z * Chunk.Depth + z;

            for (int face = 0; face < 6; face++)
            {
                var (dx, dy, dz) = Directions[face];
                if (GetNeighbor(chunk, world, x, y, z, dx, dy, dz) != BlockType.Air)
                    continue;

                uint baseIndex = (uint)(vertices.Count / 5);

                for (int vi = 0; vi < 4; vi++)
                {
                    var (ox, oy, oz) = FaceOffsets[face, vi];
                    var (tu, tv)     = UVs[vi];
                    vertices.Add(worldX + ox);
                    vertices.Add(y      + oy);
                    vertices.Add(worldZ + oz);
                    vertices.Add(tu);
                    vertices.Add(tv);
                }

                indices.Add(baseIndex);
                indices.Add(baseIndex + 1);
                indices.Add(baseIndex + 2);
                indices.Add(baseIndex + 2);
                indices.Add(baseIndex + 3);
                indices.Add(baseIndex);
            }
        }

        if (vertices.Count == 0)
            return (Array.Empty<float>(), Array.Empty<uint>());

        return (vertices.ToArray(), indices.ToArray());
    }

    private static byte GetNeighbor(Chunk chunk, World.World world,
                                    int x, int y, int z,
                                    int dx, int dy, int dz)
    {
        int nx = x + dx;
        int ny = y + dy;
        int nz = z + dz;

        if (nx >= 0 && nx < Chunk.Width && nz >= 0 && nz < Chunk.Depth)
            return chunk.GetBlock(nx, ny, nz);

        int worldX = chunk.ChunkPosition.X * Chunk.Width + nx;
        int worldZ = chunk.ChunkPosition.Z * Chunk.Depth + nz;
        return world.GetBlock(worldX, ny, worldZ);
    }
}
