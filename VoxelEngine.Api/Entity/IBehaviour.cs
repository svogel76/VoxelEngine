namespace VoxelEngine.Api.Entity;

public interface IBehaviour
{
    string BehaviourId { get; }
    void Update(IEntity entity, IModContext context, double deltaTime);
}
