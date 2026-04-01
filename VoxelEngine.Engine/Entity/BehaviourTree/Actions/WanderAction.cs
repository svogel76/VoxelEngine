using System.Numerics;
using VoxelEngine.Api;
using VoxelEngine.Api.Entity;

namespace VoxelEngine.Entity.BehaviourTree.Actions;

public sealed class WanderAction : IBehaviourNode
{
    private readonly float _speed;
    private readonly float _radius;
    private readonly float _pauseSeconds;
    private readonly Random _random;
    private Vector3? _target;
    private float _pauseRemaining;

    public WanderAction(float speed, float radius, float pauseSeconds, Random? random = null)
    {
        if (speed < 0f)
            throw new ArgumentOutOfRangeException(nameof(speed));
        if (radius < 0f)
            throw new ArgumentOutOfRangeException(nameof(radius));
        if (pauseSeconds < 0f)
            throw new ArgumentOutOfRangeException(nameof(pauseSeconds));

        _speed = speed;
        _radius = radius;
        _pauseSeconds = pauseSeconds;
        _random = random ?? Random.Shared;
    }

    public NodeResult Tick(IEntity entity, IModContext context, double deltaTime)
    {
        Vector3 position = BehaviourNodeUtilities.GetEntityPosition(entity);

        if (_pauseRemaining > 0f)
        {
            BehaviourNodeUtilities.Stop(entity, deltaTime);
            _pauseRemaining = MathF.Max(0f, _pauseRemaining - (float)deltaTime);
            return NodeResult.Running;
        }

        if (_target is null)
            _target = position + BehaviourNodeUtilities.RandomHorizontalOffset(_random, _radius);

        Vector3 target = _target.Value;
        if (BehaviourNodeUtilities.HasReachedTarget(position, target))
        {
            _target = null;
            _pauseRemaining = _pauseSeconds;
            BehaviourNodeUtilities.Stop(entity, deltaTime);
            return NodeResult.Success;
        }

        Vector3 desiredDirection = target - position;
        desiredDirection.Y = 0f;
        if (desiredDirection.LengthSquared() <= 0.0001f)
        {
            _target = null;
            return NodeResult.Success;
        }

        desiredDirection = Vector3.Normalize(desiredDirection);
        var movement = BehaviourNodeUtilities.Move(entity, desiredDirection, _speed, deltaTime);
        if (movement.Blocked)
        {
            _target = null;
            _pauseRemaining = _pauseSeconds;
            BehaviourNodeUtilities.Stop(entity, deltaTime);
            return NodeResult.Success;
        }

        return NodeResult.Running;
    }
}

