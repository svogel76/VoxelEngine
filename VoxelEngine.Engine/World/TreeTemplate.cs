namespace VoxelEngine.World;

public sealed class TreeTemplate
{
    // ── Temperate ────────────────────────────────────────────────────────
    private static readonly TreeTemplate OakTemplate       = CreateOak();
    private static readonly TreeTemplate LargeOakTemplate  = CreateLargeOak();

    // ── Taiga ─────────────────────────────────────────────────────────────
    private static readonly TreeTemplate SpruceTemplate    = CreateSpruce();
    private static readonly TreeTemplate TallSpruceTemplate = CreateTallSpruce();

    // ── Tropics ───────────────────────────────────────────────────────────
    private static readonly TreeTemplate PalmTemplate      = CreatePalm();
    private static readonly TreeTemplate TropicalTemplate  = CreateTropical();
    private static readonly TreeTemplate MegaTropicalTemplate = CreateMegaTropical();

    // ── Savanna ───────────────────────────────────────────────────────────
    private static readonly TreeTemplate AcaciaTemplate    = CreateAcacia();

    // ── Steppe / Shrub ────────────────────────────────────────────────────
    private static readonly TreeTemplate ShrubTemplate     = CreateShrub();

    // ── Desert ────────────────────────────────────────────────────────────
    private static readonly TreeTemplate Cactus3Template   = CreateCactus(3);
    private static readonly TreeTemplate Cactus5Template   = CreateCactus(5);
    private static readonly TreeTemplate Cactus7Template   = CreateCactus(7);
    private static readonly TreeTemplate Cactus9Template   = CreateCactus(9);

    private readonly List<TreeTemplateBlock> _filledBlocks;

    public string Name { get; }
    public byte[,,] Blocks { get; }
    public (int X, int Y, int Z) Pivot { get; }
    public int Width  => Blocks.GetLength(0);
    public int Height => Blocks.GetLength(1);
    public int Depth  => Blocks.GetLength(2);
    public IReadOnlyList<TreeTemplateBlock> FilledBlocks => _filledBlocks;

    private TreeTemplate(string name, byte[,,] blocks, (int X, int Y, int Z) pivot)
    {
        Name = name;
        Blocks = blocks;
        Pivot = pivot;
        _filledBlocks = [];

        for (int x = 0; x < Width;  x++)
        for (int y = 0; y < Height; y++)
        for (int z = 0; z < Depth;  z++)
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

    // ── Factory accessors ─────────────────────────────────────────────────
    public static TreeTemplate Oak()         => OakTemplate;
    public static TreeTemplate LargeOak()    => LargeOakTemplate;
    public static TreeTemplate Spruce()      => SpruceTemplate;
    public static TreeTemplate TallSpruce()  => TallSpruceTemplate;
    public static TreeTemplate Cactus()      => Cactus5Template;
    public static TreeTemplate Cactus(int height) => height switch
    {
        3 => Cactus3Template,
        5 => Cactus5Template,
        7 => Cactus7Template,
        9 => Cactus9Template,
        _ => Cactus5Template
    };
    public static TreeTemplate Palm()        => PalmTemplate;
    public static TreeTemplate Tropical()    => TropicalTemplate;
    public static TreeTemplate MegaTropical() => MegaTropicalTemplate;
    public static TreeTemplate Acacia()      => AcaciaTemplate;
    public static TreeTemplate Shrub()       => ShrubTemplate;

    // ── Temperate ─────────────────────────────────────────────────────────

    /// Standard-Eiche: 1×1-Stamm, 11 Blöcke hoch, Krone Ø9 — mittelgroßer Baum.
    private static TreeTemplate CreateOak()
    {
        // Stamm: 1×1, Höhe 7. Krone: 3 Ebenen, max. Ø7 (beschnitten).
        var blocks = CreateVolume(7, 11, 7);
        // Stamm zentriert auf (3,3)
        AddTrunk(blocks, 3, 3, 0, 7);
        // Kronenetagen
        AddLeafLayer(blocks, 1, 5, 1, 5,  7, trimCorners: true);  // Ø5 auf y=7
        AddLeafLayer(blocks, 0, 6, 0, 6,  8, trimCorners: true);  // Ø7 auf y=8
        AddLeafLayer(blocks, 1, 5, 1, 5,  9, trimCorners: true);  // Ø5 auf y=9
        AddLeafLayer(blocks, 2, 4, 2, 4, 10);                     // Ø3 Spitze
        return new TreeTemplate("oak", blocks, (3, 0, 3));
    }

    /// Große Eiche: 2×2-Stamm, 16 Blöcke hoch, Krone Ø11 — imposanter Einzelbaum.
    private static TreeTemplate CreateLargeOak()
    {
        var blocks = CreateVolume(11, 16, 11);
        // 2×2-Stamm (Zellen 4,5 in X und Z)
        AddTrunk2x2(blocks, 4, 5, 4, 5, 0, 10);
        // Kronenetagen
        AddLeafLayer(blocks, 2, 8, 2, 8, 10, trimCorners: true);
        AddLeafLayer(blocks, 1, 9, 1, 9, 11, trimCorners: true);
        AddLeafLayer(blocks, 0,10, 0,10, 12, trimCorners: true);
        AddLeafLayer(blocks, 1, 9, 1, 9, 13, trimCorners: true);
        AddLeafLayer(blocks, 3, 7, 3, 7, 14);
        AddLeafLayer(blocks, 4, 6, 4, 6, 15);
        return new TreeTemplate("large_oak", blocks, (5, 0, 5));
    }

    // ── Taiga ─────────────────────────────────────────────────────────────

    /// Fichte: 1×1-Stamm, 14 Blöcke hoch, konische Krone — typische Taiga-Fichte.
    private static TreeTemplate CreateSpruce()
    {
        var blocks = CreateVolume(7, 14, 7);
        AddTrunk(blocks, 3, 3, 0, 10);
        // Konische Krone (breiter unten, schmaler oben)
        AddLeafLayer(blocks, 1, 5, 1, 5,  7, trimCorners: true);
        AddLeafLayer(blocks, 0, 6, 0, 6,  8, trimCorners: true);
        AddLeafLayer(blocks, 1, 5, 1, 5,  9, trimCorners: true);
        AddLeafLayer(blocks, 0, 6, 0, 6, 10, trimCorners: true);
        AddLeafLayer(blocks, 1, 5, 1, 5, 11, trimCorners: true);
        AddLeafLayer(blocks, 2, 4, 2, 4, 12);
        SetBlock(blocks, 3, 13, 3, BlockType.Leaves);
        return new TreeTemplate("spruce", blocks, (3, 0, 3));
    }

    /// Riesenbaum/Taiga-Urwald: 2×2-Stamm, 22 Blöcke hoch — dominiert die Landschaft.
    private static TreeTemplate CreateTallSpruce()
    {
        var blocks = CreateVolume(11, 22, 11);
        AddTrunk2x2(blocks, 4, 5, 4, 5, 0, 17);
        // Ausladende konische Krone
        AddLeafLayer(blocks, 2, 8, 2, 8, 13, trimCorners: true);
        AddLeafLayer(blocks, 1, 9, 1, 9, 14, trimCorners: true);
        AddLeafLayer(blocks, 0,10, 0,10, 15, trimCorners: true);
        AddLeafLayer(blocks, 1, 9, 1, 9, 16, trimCorners: true);
        AddLeafLayer(blocks, 0,10, 0,10, 17, trimCorners: true);
        AddLeafLayer(blocks, 2, 8, 2, 8, 18, trimCorners: true);
        AddLeafLayer(blocks, 3, 7, 3, 7, 19);
        AddLeafLayer(blocks, 4, 6, 4, 6, 20);
        SetBlock(blocks, 5, 21, 5, BlockType.Leaves);
        return new TreeTemplate("tall_spruce", blocks, (5, 0, 5));
    }

    // ── Tropics ───────────────────────────────────────────────────────────

    /// Tropische Palme: 1×1-Stamm (leicht schräg), 12 Blöcke hoch, sternförmige Wedel.
    private static TreeTemplate CreatePalm()
    {
        var blocks = CreateVolume(11, 12, 11);
        // Gerader Stamm zentriert auf (5,5)
        for (int y = 0; y < 11; y++)
            SetBlock(blocks, 5, y, 5, BlockType.Wood);
        // Große Fächerwedel oben — reichen 5 Blöcke vom Stamm weg
        for (int i = 0; i < 11; i++)
        {
            SetBlock(blocks, i, 11, 5, BlockType.Leaves); // West-Ost
            SetBlock(blocks, 5, 11, i, BlockType.Leaves); // Süd-Nord
        }
        // Diagonale Wedel
        for (int i = 1; i <= 4; i++)
        {
            SetBlock(blocks, 5 - i, 11, 5 - i, BlockType.Leaves);
            SetBlock(blocks, 5 + i, 11, 5 - i, BlockType.Leaves);
            SetBlock(blocks, 5 - i, 11, 5 + i, BlockType.Leaves);
            SetBlock(blocks, 5 + i, 11, 5 + i, BlockType.Leaves);
        }
        // Kleine Mittelkrone
        AddLeafLayer(blocks, 4, 6, 4, 6, 10);
        return new TreeTemplate("palm", blocks, (5, 0, 5));
    }

    /// Tropischer Regenwald-Baum: 2×2-Stamm, 18 Blöcke hoch, breite Schirmkrone Ø13.
    private static TreeTemplate CreateTropical()
    {
        var blocks = CreateVolume(13, 18, 13);
        AddTrunk2x2(blocks, 5, 6, 5, 6, 0, 14);
        // Breite, flache Schirmkrone
        AddLeafLayer(blocks, 2,10, 2,10, 14, trimCorners: true);
        AddLeafLayer(blocks, 1,11, 1,11, 15, trimCorners: true);
        AddLeafLayer(blocks, 0,12, 0,12, 16, trimCorners: true);
        AddLeafLayer(blocks, 2,10, 2,10, 17);
        return new TreeTemplate("tropical", blocks, (6, 0, 6));
    }

    /// Mega-Tropenbaum: 3×3-Stamm, 28 Blöcke hoch, gigantische Schirmkrone Ø17.
    private static TreeTemplate CreateMegaTropical()
    {
        var blocks = CreateVolume(17, 28, 17);
        // 3×3-Stamm, zentriert auf (7,8,9) in X und Z
        for (int y = 0; y < 22; y++)
        for (int dx = 0; dx < 3; dx++)
        for (int dz = 0; dz < 3; dz++)
            SetBlock(blocks, 7 + dx, y, 7 + dz, BlockType.Wood);
        // Schirmkrone
        AddLeafLayer(blocks,  3, 13,  3, 13, 22, trimCorners: true);
        AddLeafLayer(blocks,  2, 14,  2, 14, 23, trimCorners: true);
        AddLeafLayer(blocks,  1, 15,  1, 15, 24, trimCorners: true);
        AddLeafLayer(blocks,  0, 16,  0, 16, 25, trimCorners: true);
        AddLeafLayer(blocks,  2, 14,  2, 14, 26, trimCorners: true);
        AddLeafLayer(blocks,  4, 12,  4, 12, 27);
        return new TreeTemplate("mega_tropical", blocks, (8, 0, 8));
    }

    // ── Savanna ───────────────────────────────────────────────────────────

    /// Akazie: 2×2-Stamm, 8 Blöcke hoch, breite flache Schirmkrone Ø11.
    private static TreeTemplate CreateAcacia()
    {
        var blocks = CreateVolume(11, 8, 11);
        AddTrunk2x2(blocks, 4, 5, 4, 5, 0, 6);
        // Flache, ausladende Schirmkrone
        AddLeafLayer(blocks, 1, 9, 1, 9, 6, trimCorners: true);
        AddLeafLayer(blocks, 0,10, 0,10, 7, trimCorners: true);
        return new TreeTemplate("acacia", blocks, (5, 0, 5));
    }

    // ── Steppe ────────────────────────────────────────────────────────────

    /// Busch: 1×1-Stamm, 3 Blöcke hoch, kompakter Laubball — niedriger, dichter Bewuchs.
    private static TreeTemplate CreateShrub()
    {
        var blocks = CreateVolume(5, 4, 5);
        AddTrunk(blocks, 2, 2, 0, 2);
        AddLeafLayer(blocks, 1, 3, 1, 3, 2);
        AddLeafLayer(blocks, 0, 4, 0, 4, 3, trimCorners: true);
        return new TreeTemplate("shrub", blocks, (2, 0, 2));
    }

    // ── Desert ────────────────────────────────────────────────────────────
    private static TreeTemplate CreateCactus(int height)
    {
        var blocks = CreateVolume(1, height, 1);
        for (int y = 0; y < height; y++)
            SetBlock(blocks, 0, y, 0, BlockType.Wood);
        return new TreeTemplate("cactus", blocks, (0, 0, 0));
    }

    // ── Builder helpers ───────────────────────────────────────────────────

    private static byte[,,] CreateVolume(int width, int height, int depth)
    {
        var blocks = new byte[width, height, depth];
        for (int x = 0; x < width;  x++)
        for (int y = 0; y < height; y++)
        for (int z = 0; z < depth;  z++)
            blocks[x, y, z] = BlockType.Air;
        return blocks;
    }

    private static void AddTrunk(byte[,,] blocks, int x, int z, int startY, int height)
    {
        for (int y = startY; y < startY + height; y++)
            SetBlock(blocks, x, y, z, BlockType.Wood);
    }

    private static void AddTrunk2x2(byte[,,] blocks, int x0, int x1, int z0, int z1, int startY, int height)
    {
        for (int y = startY; y < startY + height; y++)
        {
            SetBlock(blocks, x0, y, z0, BlockType.Wood);
            SetBlock(blocks, x1, y, z0, BlockType.Wood);
            SetBlock(blocks, x0, y, z1, BlockType.Wood);
            SetBlock(blocks, x1, y, z1, BlockType.Wood);
        }
    }

    private static void AddLeafLayer(byte[,,] blocks, int minX, int maxX, int minZ, int maxZ, int y, bool trimCorners = false)
    {
        for (int x = minX; x <= maxX; x++)
        for (int z = minZ; z <= maxZ; z++)
        {
            bool isCorner = (x == minX || x == maxX) && (z == minZ || z == maxZ);
            if (trimCorners && isCorner) continue;
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
