using VoxelEngine.Api;
using VoxelEngine.Api.Entity;

namespace VoxelEngine.Entity.BehaviourTree.Conditions;

public sealed class IsNightCondition : IBehaviourNode
{
    public NodeResult Tick(IEntity entity, IModContext context, double deltaTime)
        => context.IsNight ? NodeResult.Success : NodeResult.Failure;
}

