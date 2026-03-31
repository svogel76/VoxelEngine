namespace VoxelEngine.Api.Entity;

public interface IComponent
{
    string ComponentId { get; }
    void Update(IEntity entity, IModContext context, double deltaTime);
}
