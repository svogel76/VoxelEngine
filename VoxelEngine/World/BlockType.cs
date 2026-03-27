namespace VoxelEngine.World;

public static class BlockType
{
    public const byte Air   = 0;
    public const byte Grass = 1;
    public const byte Dirt  = 2;
    public const byte Stone = 3;
    public const byte Sand  = 4;
    public const byte Water = 5;
    public const byte Glass = 6;
    public const byte Ice   = 7;
    public const byte DryGrass = 8;
    public const byte Snow = 9;

    public static bool IsTransparent(byte blockType) =>
        BlockRegistry.IsTransparent(blockType);

    public static bool IsSolid(byte blockType) =>
        BlockRegistry.IsSolid(blockType);

    public static bool IsReplaceable(byte blockType) =>
        BlockRegistry.IsReplaceable(blockType);

    public static string GetName(byte blockType) =>
        BlockRegistry.Get(blockType).Name;
}
