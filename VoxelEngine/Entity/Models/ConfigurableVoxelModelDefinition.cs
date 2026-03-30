using VoxelEngine.World;

namespace VoxelEngine.Entity.Models;

public sealed class ConfigurableVoxelModelDefinition : IVoxelModelDefinition
{
    private readonly IVoxelModelDefinition _inner;

    public ConfigurableVoxelModelDefinition(IVoxelModelDefinition inner, BoundingBox placementBounds)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        PlacementBounds = placementBounds;
    }

    public string Id => _inner.Id;
    public float VoxelSize => _inner.VoxelSize;
    public BoundingBox PlacementBounds { get; }
    public IReadOnlyList<VoxelModelVoxel> Voxels => _inner.Voxels;
}
