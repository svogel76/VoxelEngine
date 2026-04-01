using System.Numerics;
using VoxelEngine.Api;
using VoxelEngine.Api.Entity;

namespace VoxelEngine.Entity.BehaviourTree.Actions;

public sealed class FleeAction : IBehaviourNode
{
    private readonly float _speed;
    private readonly float _radius;

    public FleeAction(float speed, float radius)
    {
        if (speed < 0f)
            throw new ArgumentOutOfRangeException(nameof(speed));
        if (radius < 0f)
            throw new ArgumentOutOfRangeException(nameof(radius));

        _speed = speed;
        _radius = radius;
    }

    public NodeResult Tick(IEntity entity, IModContext context, double deltaTime)
    {
        Vector3 position = BehaviourNodeUtilities.GetEntityPosition(entity);
        Vector3 playerPosition = BehaviourNodeUtilities.GetPlayerPosition(context);
        float distance = BehaviourNodeUtilities.GetHorizontalDistance(position, playerPosition);

        if (distance > _radius)
        {
            BehaviourNodeUtilities.Stop(entity, deltaTime);
            return NodeResult.Success;
        }

        Vector3 desiredDirection = position - playerPosition;
        desiredDirection.Y = 0f;
        if (desiredDirection.LengthSquared() <= 0.0001f)
            desiredDirection = Vector3.UnitZ;
        else
            desiredDirection = Vector3.Normalize(desiredDirection);

        BehaviourNodeUtilities.Move(entity, desiredDirection, _speed, deltaTime);
        return NodeResult.Running;
    }
}

