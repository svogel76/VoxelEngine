using VoxelEngine.Api;
using VoxelEngine.Api.Entity;

namespace VoxelEngine.Entity.BehaviourTree.Actions;

public sealed class IdleAction : IBehaviourNode
{
    private readonly float _durationSeconds;
    private float _remainingSeconds;
    private bool _active;

    public IdleAction(float durationSeconds)
    {
        if (durationSeconds < 0f)
            throw new ArgumentOutOfRangeException(nameof(durationSeconds));

        _durationSeconds = durationSeconds;
    }

    public NodeResult Tick(IEntity entity, IModContext context, double deltaTime)
    {
        if (!_active)
        {
            _remainingSeconds = _durationSeconds;
            _active = true;
        }

        BehaviourNodeUtilities.Stop(entity, deltaTime);
        _remainingSeconds -= (float)deltaTime;
        if (_remainingSeconds > 0f)
            return NodeResult.Running;

        _active = false;
        _remainingSeconds = 0f;
        return NodeResult.Success;
    }
}

