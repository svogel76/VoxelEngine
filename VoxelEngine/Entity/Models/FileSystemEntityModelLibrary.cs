namespace VoxelEngine.Entity.Models;

public sealed class FileSystemEntityModelLibrary : IEntityModelLibrary
{
    public const int EntityAtlasTileColumns = 4;
    public const int EntityAtlasTileRows = 2;

    private readonly Dictionary<string, IVoxelModelDefinition> _models;

    public EntityAtlasDefinition Atlas { get; }

    private FileSystemEntityModelLibrary(EntityAtlasDefinition atlas, Dictionary<string, IVoxelModelDefinition> models)
    {
        Atlas = atlas;
        _models = models;
    }

    public static FileSystemEntityModelLibrary LoadFromDirectory(string assetDirectory, float voxelScale = 1.0f)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assetDirectory);
        if (voxelScale <= 0f)
            throw new ArgumentOutOfRangeException(nameof(voxelScale));

        string atlasPath = Path.Combine(assetDirectory, "entity_atlas.png");
        if (!File.Exists(atlasPath))
            throw new FileNotFoundException("Entity atlas not found.", atlasPath);

        var atlas = new EntityAtlasDefinition(atlasPath, tileColumns: EntityAtlasTileColumns, tileRows: EntityAtlasTileRows);
        var models = new Dictionary<string, IVoxelModelDefinition>(StringComparer.OrdinalIgnoreCase);

        foreach (string file in Directory.EnumerateFiles(assetDirectory, "*.*", SearchOption.TopDirectoryOnly))
        {
            string extension = Path.GetExtension(file);
            if (!string.Equals(extension, ".vxm", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(extension, ".vox", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var model = LoadModel(file, extension, voxelScale);
            if (models.ContainsKey(model.Id))
                throw new InvalidOperationException($"Duplicate entity model id '{model.Id}'.");

            models.Add(model.Id, model);
        }

        return new FileSystemEntityModelLibrary(atlas, models);
    }

    public IReadOnlyCollection<IVoxelModelDefinition> GetAllModels()
        => _models.Values;

    public IVoxelModelDefinition GetModel(string modelId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelId);

        if (_models.TryGetValue(modelId, out var model))
            return model;

        throw new KeyNotFoundException($"Entity model '{modelId}' was not found.");
    }

    private static IVoxelModelDefinition LoadModel(string path, string extension, float voxelScale)
        => extension.ToLowerInvariant() switch
        {
            ".vxm" => VxmModelLoader.LoadFromFile(path, voxelScale),
            ".vox" => VoxModelLoader.LoadFromFile(path, voxelScale),
            _ => throw new NotSupportedException($"Unsupported entity model format '{extension}'.")
        };
}
