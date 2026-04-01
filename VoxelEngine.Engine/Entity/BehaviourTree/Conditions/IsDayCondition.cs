using VoxelEngine.Api;
using VoxelEngine.Api.Entity;

namespace VoxelEngine.Entity.BehaviourTree.Conditions;

public sealed class IsDayCondition : IBehaviourNode
{
    public NodeResult Tick(IEntity entity, IModContext context, double deltaTime)
        => context.IsDay ? NodeResult.Success : NodeResult.Failure;
}

