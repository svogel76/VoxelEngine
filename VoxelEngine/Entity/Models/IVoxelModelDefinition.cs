using VoxelEngine.World;

namespace VoxelEngine.Entity.Models;

public interface IVoxelModelDefinition
{
    string Id { get; }
    float VoxelSize { get; }
    BoundingBox PlacementBounds { get; }
    IReadOnlyList<VoxelModelVoxel> Voxels { get; }
}
