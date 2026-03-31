using System.Text.Json;
using System.Text.Json.Serialization;
using VoxelEngine.World;

namespace VoxelEngine.Game.Blocks;

public sealed class BlockDefinitionLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly string _assetRoot;

    public BlockDefinitionLoader(string assetRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assetRoot);
        _assetRoot = assetRoot;
    }

    public void LoadInto(IBlockRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        string assetRoot = ResolveAssetRoot(_assetRoot);
        string blocksDirectory = Path.Combine(assetRoot, "Blocks");
        string textureManifestPath = Path.Combine(assetRoot, "Textures", "blocks.manifest.json");

        if (!Directory.Exists(blocksDirectory))
            throw new DirectoryNotFoundException($"Block directory '{blocksDirectory}' was not found.");

        if (!File.Exists(textureManifestPath))
            throw new FileNotFoundException($"Texture manifest '{textureManifestPath}' was not found.", textureManifestPath);

        IReadOnlyDictionary<string, int> textureLayers = LoadTextureManifest(textureManifestPath);
        string[] blockFiles = Directory.GetFiles(blocksDirectory, "*.json", SearchOption.TopDirectoryOnly)
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var seenIds = new Dictionary<byte, string>();
        foreach (string blockFile in blockFiles)
        {
            var document = DeserializeBlockFile(blockFile);
            byte id = RequireId(document.Id, blockFile);
            string name = RequireText(document.Name, "name", blockFile);

            if (seenIds.TryGetValue(id, out var existingFile))
                throw new InvalidOperationException($"Duplicate block ID {id} in '{Path.GetFileName(blockFile)}'; already defined in '{existingFile}'.");

            seenIds[id] = Path.GetFileName(blockFile);
            registry.Register(CreateDefinition(document, textureLayers, id, name, blockFile));
        }
    }

    private static BlockDefinitionFile DeserializeBlockFile(string blockFile)
    {
        var document = JsonSerializer.Deserialize<BlockDefinitionFile>(File.ReadAllText(blockFile), JsonOptions);
        return document ?? throw new InvalidOperationException($"Block definition '{Path.GetFileName(blockFile)}' is empty or invalid JSON.");
    }

    private static IReadOnlyDictionary<string, int> LoadTextureManifest(string textureManifestPath)
    {
        var manifest = JsonSerializer.Deserialize<TextureManifestFile>(File.ReadAllText(textureManifestPath), JsonOptions)
            ?? throw new InvalidOperationException($"Texture manifest '{Path.GetFileName(textureManifestPath)}' is empty or invalid JSON.");

        if (manifest.Layers is null)
            throw new InvalidOperationException($"Texture manifest '{Path.GetFileName(textureManifestPath)}' is missing required field 'layers'.");

        var textureLayers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < manifest.Layers.Count; i++)
        {
            string textureName = RequireText(manifest.Layers[i], $"layers[{i}]", textureManifestPath);
            if (!textureLayers.TryAdd(textureName, i))
                throw new InvalidOperationException($"Texture manifest '{Path.GetFileName(textureManifestPath)}' contains duplicate texture '{textureName}'.");
        }

        return textureLayers;
    }

    private static BlockDefinition CreateDefinition(
        BlockDefinitionFile document,
        IReadOnlyDictionary<string, int> textureLayers,
        byte id,
        string name,
        string blockFile)
    {
        if (document.Textures is null)
            throw new InvalidOperationException($"Block definition '{Path.GetFileName(blockFile)}' is missing required field 'textures'.");

        var properties = document.Properties ?? new BlockPropertiesFile();

        return new BlockDefinition
        {
            Id = id,
            Name = name,
            TopTextureIndex = ResolveTextureLayer(textureLayers, name, "top", document.Textures.Top, blockFile),
            SideTextureIndex = ResolveTextureLayer(textureLayers, name, "side", document.Textures.Side, blockFile),
            BottomTextureIndex = ResolveTextureLayer(textureLayers, name, "bottom", document.Textures.Bottom, blockFile),
            Solid = properties.Solid,
            Transparent = properties.Transparent,
            Cutout = properties.Cutout,
            CollidesWithPlayer = properties.CollidesWithPlayer ?? true,
            RenderBackfaces = properties.RenderBackfaces,
            Replaceable = properties.Replaceable,
            Luminance = properties.Luminance,
            Tags = properties.Tags ?? [],
            MaxStackSize = properties.MaxStackSize ?? 64
        };
    }

    private static int ResolveTextureLayer(
        IReadOnlyDictionary<string, int> textureLayers,
        string blockName,
        string textureKey,
        string? textureName,
        string blockFile)
    {
        string requiredTextureName = RequireText(textureName, $"textures.{textureKey}", blockFile);
        if (textureLayers.TryGetValue(requiredTextureName, out int layer))
            return layer;

        throw new InvalidOperationException($"Unknown texture reference '{requiredTextureName}' for block '{blockName}' at key '{textureKey}'.");
    }

    private static byte RequireId(int? id, string blockFile)
    {
        if (id is null)
            throw new InvalidOperationException($"Block definition '{Path.GetFileName(blockFile)}' is missing required field 'id'.");

        if (id < byte.MinValue || id > byte.MaxValue)
            throw new InvalidOperationException($"Block definition '{Path.GetFileName(blockFile)}' has ID {id} outside byte range.");

        return (byte)id.Value;
    }

    private static string RequireText(string? value, string fieldName, string sourcePath)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Definition '{Path.GetFileName(sourcePath)}' is missing required field '{fieldName}'.");

        return value;
    }

    private static string ResolveAssetRoot(string assetRoot)
    {
        if (Path.IsPathRooted(assetRoot))
            return assetRoot;

        string currentDirectoryPath = Path.GetFullPath(assetRoot);
        if (Directory.Exists(currentDirectoryPath))
            return currentDirectoryPath;

        string baseDirectoryPath = Path.Combine(AppContext.BaseDirectory, assetRoot);
        return Path.GetFullPath(baseDirectoryPath);
    }

    private sealed class BlockDefinitionFile
    {
        public int? Id { get; init; }
        public string? Name { get; init; }
        public BlockTexturesFile? Textures { get; init; }
        public BlockPropertiesFile? Properties { get; init; }
        public string? Behaviour { get; init; }
    }

    private sealed class BlockTexturesFile
    {
        public string? Top { get; init; }
        public string? Side { get; init; }
        public string? Bottom { get; init; }
    }

    private sealed class BlockPropertiesFile
    {
        public bool Solid { get; init; }
        public bool Transparent { get; init; }
        public bool Cutout { get; init; }

        [JsonPropertyName("collides_with_player")]
        public bool? CollidesWithPlayer { get; init; }

        [JsonPropertyName("render_backfaces")]
        public bool RenderBackfaces { get; init; }

        public bool Replaceable { get; init; }
        public int Luminance { get; init; }
        public string[]? Tags { get; init; }

        [JsonPropertyName("max_stack")]
        public int? MaxStackSize { get; init; }
    }

    private sealed class TextureManifestFile
    {
        public List<string?>? Layers { get; init; }
    }
}
