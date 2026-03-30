using VoxelEngine.World;

namespace VoxelEngine.Entity;

public interface IEntityBoundsProvider
{
    BoundingBox Bounds { get; }
}
