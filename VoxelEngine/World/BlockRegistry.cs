namespace VoxelEngine.World;

public static class BlockRegistry
{
    private const int MaxBlockTypes = byte.MaxValue + 1;

    private static readonly BlockDefinition?[] ById = new BlockDefinition?[MaxBlockTypes];
    private static readonly Dictionary<string, BlockDefinition> ByName = new(StringComparer.OrdinalIgnoreCase);
    private static readonly List<BlockDefinition> Definitions = [];

    static BlockRegistry()
    {
        Register(new BlockDefinition
        {
            Id = BlockType.Air,
            Name = "air",
            TopTextureIndex = 2,
            SideTextureIndex = 2,
            BottomTextureIndex = 2,
            Solid = false,
            Transparent = false,
            Replaceable = true,
            Luminance = 0,
            Tags = ["empty"]
        });

        Register(new BlockDefinition
        {
            Id = BlockType.Grass,
            Name = "grass",
            TopTextureIndex = 0,
            SideTextureIndex = 3,
            BottomTextureIndex = 1,
            Solid = true,
            Transparent = false,
            Replaceable = false,
            Luminance = 0,
            Tags = ["natural", "soil"]
        });

        Register(new BlockDefinition
        {
            Id = BlockType.Dirt,
            Name = "dirt",
            TopTextureIndex = 1,
            SideTextureIndex = 1,
            BottomTextureIndex = 1,
            Solid = true,
            Transparent = false,
            Replaceable = false,
            Luminance = 0,
            Tags = ["natural", "soil"]
        });

        Register(new BlockDefinition
        {
            Id = BlockType.Stone,
            Name = "stone",
            TopTextureIndex = 2,
            SideTextureIndex = 2,
            BottomTextureIndex = 2,
            Solid = true,
            Transparent = false,
            Replaceable = false,
            Luminance = 0,
            Tags = ["natural", "rock"]
        });

        Register(new BlockDefinition
        {
            Id = BlockType.Sand,
            Name = "sand",
            TopTextureIndex = 4,
            SideTextureIndex = 4,
            BottomTextureIndex = 4,
            Solid = true,
            Transparent = false,
            Replaceable = false,
            Luminance = 0,
            Tags = ["natural", "sand"]
        });

        Register(new BlockDefinition
        {
            Id = BlockType.Water,
            Name = "water",
            TopTextureIndex = 8,
            SideTextureIndex = 8,
            BottomTextureIndex = 8,
            Solid = false,
            Transparent = true,
            Replaceable = true,
            Luminance = 0,
            Tags = ["natural", "liquid"]
        });

        Register(new BlockDefinition
        {
            Id = BlockType.Glass,
            Name = "glass",
            TopTextureIndex = 9,
            SideTextureIndex = 9,
            BottomTextureIndex = 9,
            Solid = false,
            Transparent = true,
            Replaceable = false,
            Luminance = 0,
            Tags = ["crafted", "transparent"]
        });

        Register(new BlockDefinition
        {
            Id = BlockType.Ice,
            Name = "ice",
            TopTextureIndex = 10,
            SideTextureIndex = 10,
            BottomTextureIndex = 10,
            Solid = false,
            Transparent = true,
            Replaceable = false,
            Luminance = 0,
            Tags = ["natural", "frozen"]
        });

        Register(new BlockDefinition
        {
            Id = BlockType.DryGrass,
            Name = "dry_grass",
            TopTextureIndex = 11,
            SideTextureIndex = 12,
            BottomTextureIndex = 1,
            Solid = true,
            Transparent = false,
            Replaceable = false,
            Luminance = 0,
            Tags = ["natural", "soil", "dry"]
        });

        Register(new BlockDefinition
        {
            Id = BlockType.Snow,
            Name = "snow",
            TopTextureIndex = 5,
            SideTextureIndex = 5,
            BottomTextureIndex = 5,
            Solid = true,
            Transparent = false,
            Replaceable = false,
            Luminance = 0,
            Tags = ["natural", "cold"]
        });
    }

    public static IReadOnlyList<BlockDefinition> All => Definitions;

    public static void Register(BlockDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (ById[definition.Id] is not null)
            throw new InvalidOperationException($"Block ID {definition.Id} is already registered.");

        if (ByName.ContainsKey(definition.Name))
            throw new InvalidOperationException($"Block name '{definition.Name}' is already registered.");

        ById[definition.Id] = definition;
        ByName[definition.Name] = definition;
        Definitions.Add(definition);
        Definitions.Sort(static (left, right) => left.Id.CompareTo(right.Id));
    }

    public static BlockDefinition Get(byte id) =>
        ById[id] ?? throw new KeyNotFoundException($"Block ID {id} is not registered.");

    public static BlockDefinition Get(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return ByName.TryGetValue(name, out var definition)
            ? definition
            : throw new KeyNotFoundException($"Block '{name}' is not registered.");
    }

    public static bool IsTransparent(byte id) => Get(id).Transparent;

    public static bool IsSolid(byte id) => Get(id).Solid;

    public static bool IsReplaceable(byte id) => Get(id).Replaceable;

    public static int GetRequiredTextureLayerCount()
    {
        if (Definitions.Count == 0)
            return 0;

        int maxTextureIndex = Definitions.Max(static definition => Math.Max(
            definition.TopTextureIndex,
            Math.Max(definition.SideTextureIndex, definition.BottomTextureIndex)));

        return maxTextureIndex + 1;
    }

    public static IEnumerable<int> GetUsedTextureLayers() =>
        Definitions
            .SelectMany(static definition => new[]
            {
                definition.TopTextureIndex,
                definition.SideTextureIndex,
                definition.BottomTextureIndex
            })
            .Distinct()
            .OrderBy(static layer => layer);
}
