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

    public static bool IsTransparent(byte blockType) => blockType switch
    {
        Water => true,
        Glass => true,
        Ice   => true,
        _     => false,
    };

    public static bool IsSolid(byte blockType) =>
        blockType != Air && !IsTransparent(blockType);

    public static string GetName(byte blockType) => blockType switch
    {
        Air => "Air",
        Grass => "Grass",
        Dirt => "Dirt",
        Stone => "Stone",
        Sand => "Sand",
        Water => "Water",
        Glass => "Glass",
        Ice => "Ice",
        _ => $"Block {blockType}"
    };
}
