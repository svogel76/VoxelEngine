using VoxelEngine.Api;
using VoxelEngine.Api.Entity;
using VoxelEngine.Entity.Components;

namespace VoxelEngine.Entity.BehaviourTree.Conditions;

public sealed class HealthLowCondition : IBehaviourNode
{
    private readonly float _threshold;

    public HealthLowCondition(float threshold)
    {
        if (threshold < 0f || threshold > 1f)
            throw new ArgumentOutOfRangeException(nameof(threshold));

        _threshold = threshold;
    }

    public NodeResult Tick(IEntity entity, IModContext context, double deltaTime)
    {
        var health = entity.GetComponent<HealthComponent>();
        if (health is null)
            return NodeResult.Failure;

        if (health.MaxHp <= 0f)
            return NodeResult.Failure;

        float ratio = health.CurrentHp / health.MaxHp;
        return ratio < _threshold ? NodeResult.Success : NodeResult.Failure;
    }
}

