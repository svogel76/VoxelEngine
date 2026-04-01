namespace VoxelEngine.Api.Entity;

public interface IBehaviourNode
{
    NodeResult Tick(IEntity entity, IModContext context, double deltaTime);
}
