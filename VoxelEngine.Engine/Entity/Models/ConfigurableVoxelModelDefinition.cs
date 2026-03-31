using VoxelEngine.World;

namespace VoxelEngine.Entity.Models;

public sealed class ConfigurableVoxelModelDefinition : IVoxelModelDefinition
{
    private readonly IVoxelModelDefinition _inner;

    public ConfigurableVoxelModelDefinition(
        IVoxelModelDefinition inner,
        string? modelId = null,
        BoundingBox? placementBounds = null,
        EntityModelMetadata? metadata = null)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        Id = string.IsNullOrWhiteSpace(modelId) ? _inner.Id : modelId;
        PlacementBounds = placementBounds ?? _inner.PlacementBounds;
        Metadata = metadata ?? _inner.Metadata;
    }

    public string Id { get; }
    public float VoxelSize => _inner.VoxelSize;
    public BoundingBox PlacementBounds { get; }
    public EntityModelMetadata Metadata { get; }
    public IReadOnlyList<VoxelModelVoxel> Voxels => _inner.Voxels;
}
