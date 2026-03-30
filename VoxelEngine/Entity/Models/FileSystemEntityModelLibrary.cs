using System.Numerics;
using System.Text.Json;
using VoxelEngine.World;

namespace VoxelEngine.Entity.Models;

public sealed class FileSystemEntityModelLibrary : IEntityModelLibrary
{
    public const int EntityAtlasTileColumns = 4;
    public const int EntityAtlasTileRows = 2;
    private static readonly JsonSerializerOptions MetadataJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

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
        var loadedModelPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string metadataPath in Directory.EnumerateFiles(assetDirectory, "*.json", SearchOption.TopDirectoryOnly))
        {
            string entityId = Path.GetFileNameWithoutExtension(metadataPath);
            var metadata = LoadMetadata(metadataPath) ?? EntityModelMetadata.Empty;
            string modelPath = ResolveModelPath(assetDirectory, entityId, metadata);
            string fullModelPath = Path.GetFullPath(modelPath);

            var definition = BuildModelDefinition(entityId, fullModelPath, voxelScale, metadata);
            if (models.ContainsKey(definition.Id))
                throw new InvalidOperationException($"Duplicate entity model id '{definition.Id}'.");

            models.Add(definition.Id, definition);
            loadedModelPaths.Add(fullModelPath);
        }

        foreach (string file in Directory.EnumerateFiles(assetDirectory, "*.*", SearchOption.TopDirectoryOnly))
        {
            string extension = Path.GetExtension(file);
            if (!string.Equals(extension, ".vxm", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(extension, ".vox", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string fullPath = Path.GetFullPath(file);
            if (loadedModelPaths.Contains(fullPath))
                continue;

            string modelId = Path.GetFileNameWithoutExtension(file);
            var metadata = TryLoadCompanionMetadata(file);
            var definition = BuildModelDefinition(modelId, fullPath, voxelScale, metadata);

            if (models.ContainsKey(definition.Id))
                throw new InvalidOperationException($"Duplicate entity model id '{definition.Id}'.");

            models.Add(definition.Id, definition);
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

    private static EntityModelMetadata? LoadMetadata(string metadataPath)
    {
        if (!File.Exists(metadataPath))
            return null;

        try
        {
            string json = File.ReadAllText(metadataPath);
            return JsonSerializer.Deserialize<EntityModelMetadata>(json, MetadataJsonOptions);
        }
        catch (JsonException exception)
        {
            throw new FormatException($"Invalid entity model metadata in '{metadataPath}'.", exception);
        }
    }

    private static EntityModelMetadata? TryLoadCompanionMetadata(string modelPath)
        => LoadMetadata(Path.ChangeExtension(modelPath, ".json"));

    private static IVoxelModelDefinition BuildModelDefinition(string entityId, string modelPath, float defaultScale, EntityModelMetadata? metadata)
    {
        string extension = Path.GetExtension(modelPath);
        var effectiveMetadata = metadata ?? EntityModelMetadata.Empty;
        float effectiveScale = effectiveMetadata.GetScaleOverride() ?? defaultScale;
        if (effectiveScale <= 0f)
            throw new FormatException($"Entity model metadata for '{modelPath}' must define a scale greater than zero.");

        var model = LoadModel(modelPath, extension, effectiveScale);
        model = VoxelModelTransform.ApplyRotation(model, effectiveMetadata.GetRotationOverride());

        BoundingBox? boundsOverride = CreateBoundsOverride(model, effectiveMetadata);
        bool requiresWrapper = !string.Equals(model.Id, entityId, StringComparison.OrdinalIgnoreCase)
                               || boundsOverride is not null
                               || !ReferenceEquals(effectiveMetadata, EntityModelMetadata.Empty);

        return requiresWrapper
            ? new ConfigurableVoxelModelDefinition(model, entityId, boundsOverride, effectiveMetadata)
            : model;
    }

    private static string ResolveModelPath(string assetDirectory, string entityId, EntityModelMetadata metadata)
    {
        if (!string.IsNullOrWhiteSpace(metadata.Display.Model))
        {
            string explicitPath = Path.GetFullPath(Path.Combine(assetDirectory, metadata.Display.Model));
            if (!File.Exists(explicitPath))
                throw new FileNotFoundException($"Entity model file '{metadata.Display.Model}' for '{entityId}' was not found.", explicitPath);

            return explicitPath;
        }

        foreach (string extension in new[] { ".vox", ".vxm" })
        {
            string candidate = Path.Combine(assetDirectory, entityId + extension);
            if (File.Exists(candidate))
                return candidate;
        }

        throw new FileNotFoundException($"No entity model file found for '{entityId}'. Expected '{entityId}.vox' or '{entityId}.vxm'.");
    }

    private static BoundingBox? CreateBoundsOverride(IVoxelModelDefinition model, EntityModelMetadata metadata)
    {
        var boundingBox = metadata.GetBoundingBoxOverride();
        if (boundingBox is not null)
        {
            if (boundingBox.Width <= 0f || boundingBox.Height <= 0f)
                throw new FormatException($"Entity model '{model.Id}' must define a bounding box with width and height greater than zero.");

            float halfWidth = boundingBox.Width * 0.5f;
            return new BoundingBox(
                new Vector3(-halfWidth, 0f, -halfWidth),
                new Vector3(halfWidth, boundingBox.Height, halfWidth));
        }

        return CreateLegacyBoundsOverride(model, metadata.GetLegacyBoundsOverride());
    }

    private static BoundingBox? CreateLegacyBoundsOverride(IVoxelModelDefinition model, EntityModelBounds? bounds)
    {
        if (bounds is null)
            return null;

        var placementBounds = new BoundingBox(
            new Vector3(bounds.Min.X, bounds.Min.Y, bounds.Min.Z),
            new Vector3(bounds.Max.X, bounds.Max.Y, bounds.Max.Z));

        if (placementBounds.Max.X <= placementBounds.Min.X ||
            placementBounds.Max.Y <= placementBounds.Min.Y ||
            placementBounds.Max.Z <= placementBounds.Min.Z)
        {
            throw new FormatException($"Entity model '{model.Id}' must define bounds with max greater than min on all axes.");
        }

        return placementBounds;
    }
}
