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

    private static readonly FaceDirection[] FaceDirections =
    [
        FaceDirection.Top,
        FaceDirection.Bottom,
        FaceDirection.Front,
        FaceDirection.Back,
        FaceDirection.Right,
        FaceDirection.Left,
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

    public static (float[] vertices, uint[] indices) Build(
        Chunk chunk, World.World world, AtlasTexture atlas)
    {
        var vertices = new List<float>();
        var indices  = new List<uint>();

        for (int x = 0; x < Chunk.Width;  x++)
        for (int y = 0; y < Chunk.Height; y++)
        for (int z = 0; z < Chunk.Depth;  z++)
        {
            byte blockType = chunk.GetBlock(x, y, z);
            if (blockType == BlockType.Air)
                continue;

            int worldX = chunk.ChunkPosition.X * Chunk.Width + x;
            int worldZ = chunk.ChunkPosition.Z * Chunk.Depth + z;

            for (int face = 0; face < 6; face++)
            {
                var (dx, dy, dz) = Directions[face];
                if (GetNeighbor(chunk, world, x, y, z, dx, dy, dz) != BlockType.Air)
                    continue;

                int tileIndex = BlockTextures.GetTileIndex(blockType, FaceDirections[face]);
                var (u, v)    = atlas.GetTileUV(tileIndex);
                float uEnd    = u + AtlasTexture.TileUV;
                float vEnd    = v + AtlasTexture.TileUV;

                // UV-Ecken: links-unten, rechts-unten, rechts-oben, links-oben
                (float U, float V)[] uvCorners =
                [
                    (u,    vEnd),
                    (uEnd, vEnd),
                    (uEnd, v   ),
                    (u,    v   ),
                ];

                uint baseIndex = (uint)(vertices.Count / 5);

                for (int vi = 0; vi < 4; vi++)
                {
                    var (ox, oy, oz) = FaceOffsets[face, vi];
                    var (tu, tv)     = uvCorners[vi];
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
