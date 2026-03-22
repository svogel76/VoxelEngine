namespace VoxelEngine.World;

public static class BlockTextures
{
    // Tile-Indizes pro Block-Typ: (Top, Bottom, Side)
    private static readonly Dictionary<byte, (int Top, int Bottom, int Side)> _textures = new()
    {
        { BlockType.Grass, (Top: 0, Bottom: 1, Side: 3) },
        { BlockType.Dirt,  (Top: 1, Bottom: 1, Side: 1) },
        { BlockType.Stone, (Top: 2, Bottom: 2, Side: 2) },
        { BlockType.Sand,  (Top: 4, Bottom: 4, Side: 4)  },
        { BlockType.Water, (Top: 8, Bottom: 8, Side: 8)  },
        { BlockType.Glass, (Top: 9, Bottom: 9, Side: 9)  },
        { BlockType.Ice,   (Top: 10, Bottom: 10, Side: 10) },
    };

    public static int GetTileIndex(byte blockType, FaceDirection face)
    {
        if (!_textures.TryGetValue(blockType, out var tex))
            return 2; // Stein als Fallback

        return face switch
        {
            FaceDirection.Top    => tex.Top,
            FaceDirection.Bottom => tex.Bottom,
            _                    => tex.Side,
        };
    }
}
