namespace VoxelEngine.World;

public static class BlockRegistry
{
    private const int MaxBlockTypes = byte.MaxValue + 1;

    private static readonly BlockDefinition?[] ById = new BlockDefinition?[MaxBlockTypes];
    private static readonly Dictionary<string, BlockDefinition> ByName = new(StringComparer.OrdinalIgnoreCase);
    private static readonly List<BlockDefinition> Definitions = [];

    public static IReadOnlyList<BlockDefinition> All => Definitions;

    public static void Clear()
    {
        Array.Clear(ById);
        ByName.Clear();
        Definitions.Clear();
    }

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

    public static BlockDefinition? TryGet(int id)
    {
        if (id < 0 || id >= MaxBlockTypes)
            return null;

        return ById[id];
    }

    public static BlockDefinition Get(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return ByName.TryGetValue(name, out var definition)
            ? definition
            : throw new KeyNotFoundException($"Block '{name}' is not registered.");
    }

    public static bool IsTransparent(byte id) => Get(id).Transparent;

    public static bool IsSolid(byte id) => Get(id).Solid;

    public static bool IsCutout(byte id) => Get(id).Cutout;

    public static bool CollidesWithPlayer(byte id) => Get(id).CollidesWithPlayer;

    public static bool RendersBackfaces(byte id) => Get(id).RenderBackfaces;

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
