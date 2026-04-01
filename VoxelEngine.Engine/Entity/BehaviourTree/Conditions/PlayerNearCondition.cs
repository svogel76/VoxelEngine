using VoxelEngine.Api;
using VoxelEngine.Api.Entity;

namespace VoxelEngine.Entity.BehaviourTree.Conditions;

public sealed class PlayerNearCondition : IBehaviourNode
{
    private readonly float _radius;

    public PlayerNearCondition(float radius)
    {
        if (radius < 0f)
            throw new ArgumentOutOfRangeException(nameof(radius));

        _radius = radius;
    }

    public NodeResult Tick(IEntity entity, IModContext context, double deltaTime)
    {
        float distance = BehaviourNodeUtilities.GetHorizontalDistance(
            BehaviourNodeUtilities.GetEntityPosition(entity),
            BehaviourNodeUtilities.GetPlayerPosition(context));

        return distance <= _radius ? NodeResult.Success : NodeResult.Failure;
    }
}

