using VoxelEngine.Api.Math;

namespace VoxelEngine.Api.Entity;

public interface IEntity
{
    string Id { get; }
    Vector3D<float> Position { get; set; }
}
