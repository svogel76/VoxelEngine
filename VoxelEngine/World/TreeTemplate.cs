namespace VoxelEngine.World;

public sealed class TreeTemplate
{
    private static readonly TreeTemplate OakTemplate = CreateOak();
    private static readonly TreeTemplate SpruceTemplate = CreateSpruce();
    private static readonly TreeTemplate Cactus3Template = CreateCactus(3);
    private static readonly TreeTemplate Cactus4Template = CreateCactus(4);
    private static readonly TreeTemplate Cactus5Template = CreateCactus(5);
    private static readonly TreeTemplate PalmTemplate = CreatePalm();
    private static readonly TreeTemplate AcaciaTemplate = CreateAcacia();
    private static readonly TreeTemplate ShrubTemplate = CreateShrub();

    private readonly List<TreeTemplateBlock> _filledBlocks;

    public string Name { get; }
    public byte[,,] Blocks { get; }
    public (int X, int Y, int Z) Pivot { get; }
    public int Width => Blocks.GetLength(0);
    public int Height => Blocks.GetLength(1);
    public int Depth => Blocks.GetLength(2);
    public IReadOnlyList<TreeTemplateBlock> FilledBlocks => _filledBlocks;

    private TreeTemplate(string name, byte[,,] blocks, (int X, int Y, int Z) pivot)
    {
        Name = name;
        Blocks = blocks;
        Pivot = pivot;
        _filledBlocks = [];

        for (int x = 0; x < Width; x++)
        for (int y = 0; y < Height; y++)
        for (int z = 0; z < Depth; z++)
        {
            byte block = blocks[x, y, z];
            if (block != BlockType.Air)
                _filledBlocks.Add(new TreeTemplateBlock(x, y, z, block));
        }
    }

    public byte GetBlock(int x, int y, int z)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height || z < 0 || z >= Depth)
            return BlockType.Air;

        return Blocks[x, y, z];
    }

    public static TreeTemplate Oak() => OakTemplate;
    public static TreeTemplate Spruce() => SpruceTemplate;
    public static TreeTemplate Cactus() => Cactus4Template;
    public static TreeTemplate Cactus(int height) => height switch
    {
        3 => Cactus3Template,
        4 => Cactus4Template,
        5 => Cactus5Template,
        _ => Cactus4Template
    };
    public static TreeTemplate Palm() => PalmTemplate;
    public static TreeTemplate Acacia() => AcaciaTemplate;
    public static TreeTemplate Shrub() => ShrubTemplate;

    private static TreeTemplate CreateOak()
    {
        var blocks = CreateVolume(5, 8, 5);
        AddTrunk(blocks, 2, 2, 0, 5);
        AddLeafLayer(blocks, 1, 3, 1, 3, 5);
        AddLeafLayer(blocks, 0, 4, 0, 4, 6, trimCorners: true);
        AddLeafLayer(blocks, 1, 3, 1, 3, 7);
        return new TreeTemplate("oak", blocks, (2, 0, 2));
    }

    private static TreeTemplate CreateSpruce()
    {
        var blocks = CreateVolume(5, 10, 5);
        AddTrunk(blocks, 2, 2, 0, 7);
        AddLeafLayer(blocks, 1, 3, 1, 3, 4);
        AddLeafLayer(blocks, 0, 4, 0, 4, 5, trimCorners: true);
        AddLeafLayer(blocks, 1, 3, 1, 3, 6);
        AddLeafLayer(blocks, 0, 4, 0, 4, 7, trimCorners: true);
        AddLeafLayer(blocks, 1, 3, 1, 3, 8);
        SetBlock(blocks, 2, 9, 2, BlockType.Leaves);
        return new TreeTemplate("spruce", blocks, (2, 0, 2));
    }

    private static TreeTemplate CreateCactus(int height)
    {
        var blocks = CreateVolume(1, height, 1);
        AddTrunk(blocks, 0, 0, 0, height);
        return new TreeTemplate("cactus", blocks, (0, 0, 0));
    }

    private static TreeTemplate CreatePalm()
    {
        var blocks = CreateVolume(7, 8, 7);
        AddTrunk(blocks, 3, 3, 0, 7);

        for (int i = 0; i < 7; i++)
        {
            SetBlock(blocks, i, 7, 3, BlockType.Leaves);
            SetBlock(blocks, 3, 7, i, BlockType.Leaves);
        }

        SetBlock(blocks, 2, 7, 2, BlockType.Leaves);
        SetBlock(blocks, 4, 7, 2, BlockType.Leaves);
        SetBlock(blocks, 2, 7, 4, BlockType.Leaves);
        SetBlock(blocks, 4, 7, 4, BlockType.Leaves);
        return new TreeTemplate("palm", blocks, (3, 0, 3));
    }

    private static TreeTemplate CreateAcacia()
    {
        var blocks = CreateVolume(7, 5, 7);
        AddTrunk(blocks, 3, 3, 0, 4);
        AddLeafLayer(blocks, 0, 6, 0, 6, 4, trimCorners: true);
        return new TreeTemplate("acacia", blocks, (3, 0, 3));
    }

    private static TreeTemplate CreateShrub()
    {
        var blocks = CreateVolume(3, 3, 3);
        AddTrunk(blocks, 1, 1, 0, 1);
        AddLeafLayer(blocks, 0, 2, 0, 2, 1);
        SetBlock(blocks, 1, 2, 1, BlockType.Leaves);
        return new TreeTemplate("shrub", blocks, (1, 0, 1));
    }

    private static byte[,,] CreateVolume(int width, int height, int depth)
    {
        var blocks = new byte[width, height, depth];
        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
        for (int z = 0; z < depth; z++)
            blocks[x, y, z] = BlockType.Air;
        return blocks;
    }

    private static void AddTrunk(byte[,,] blocks, int x, int z, int startY, int height)
    {
        for (int y = startY; y < startY + height; y++)
            SetBlock(blocks, x, y, z, BlockType.Wood);
    }

    private static void AddLeafLayer(byte[,,] blocks, int minX, int maxX, int minZ, int maxZ, int y, bool trimCorners = false)
    {
        for (int x = minX; x <= maxX; x++)
        for (int z = minZ; z <= maxZ; z++)
        {
            bool isCorner = (x == minX || x == maxX) && (z == minZ || z == maxZ);
            if (trimCorners && isCorner)
                continue;

            SetBlock(blocks, x, y, z, BlockType.Leaves);
        }
    }

    private static void SetBlock(byte[,,] blocks, int x, int y, int z, byte block)
    {
        if (x < 0 || x >= blocks.GetLength(0) ||
            y < 0 || y >= blocks.GetLength(1) ||
            z < 0 || z >= blocks.GetLength(2))
            return;

        blocks[x, y, z] = block;
    }
}

public readonly record struct TreeTemplateBlock(int X, int Y, int Z, byte Block);
