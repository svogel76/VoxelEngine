using VoxelEngine.Api.Math;

namespace VoxelEngine.Api.Entity;

public interface IEntity
{
    string Id { get; }
    Vector3D<float> Position { get; set; }
    Vector3D<float> Velocity { get; set; }
    bool IsActive { get; set; }

    void AddComponent(IComponent component);
    T? GetComponent<T>() where T : class, IComponent;
    bool HasComponent<T>() where T : class, IComponent;
    IReadOnlyList<IComponent> Components { get; }
    void Update(IModContext context, double deltaTime);
}
