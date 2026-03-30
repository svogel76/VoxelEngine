using System.Numerics;
using VoxelEngine.Entity.Models;

namespace VoxelEngine.Entity.AI;

public sealed class AnimalMovementStateMachine
{
    private const float ArrivalDistance = 0.35f;

    private readonly EntityBehaviourMetadata _behaviour;
    private readonly Random _random;
    private float _idleTimeRemaining;
    private EntityTimeOfDayActivity _timeOfDayActivity;

    public AnimalMovementStateMachine(EntityBehaviourMetadata behaviour, Random? random = null)
    {
        _behaviour = behaviour ?? throw new ArgumentNullException(nameof(behaviour));
        _random = random ?? Random.Shared;

        ValidateBehaviour(behaviour);
        _timeOfDayActivity = behaviour.DayActivity;
        EnterIdle();
    }

    public AnimalMovementState State { get; private set; }
    public Vector3? CurrentTarget { get; private set; }

    public AnimalMovementDirective Tick(Vector3 position, Vector3? threatPosition, float deltaTime)
    {
        if (_timeOfDayActivity == EntityTimeOfDayActivity.Burrow)
            return new AnimalMovementDirective(AnimalMovementState.Burrow, Vector3.Zero, 0f, null);
        if (State == AnimalMovementState.Sleep)
            return TickSleep(position, threatPosition);

        if (ShouldFlee(position, threatPosition))
            EnterFlee();
        else if (State == AnimalMovementState.Flee)
            EnterScheduledRestState();

        return State switch
        {
            AnimalMovementState.Idle => TickIdle(position, threatPosition, deltaTime),
            AnimalMovementState.Wander => TickWander(position, threatPosition),
            AnimalMovementState.Flee => TickFlee(position, threatPosition),
            AnimalMovementState.Sleep => TickSleep(position, threatPosition),
            AnimalMovementState.Burrow => new AnimalMovementDirective(AnimalMovementState.Burrow, Vector3.Zero, 0f, null),
            _ => new AnimalMovementDirective(AnimalMovementState.Idle, Vector3.Zero, 0f, null)
        };
    }

    public void ApplyTimeOfDayActivity(EntityTimeOfDayActivity activity)
    {
        _timeOfDayActivity = activity;

        switch (activity)
        {
            case EntityTimeOfDayActivity.Active:
                if (State is AnimalMovementState.Sleep or AnimalMovementState.Burrow)
                    EnterIdle();
                break;

            case EntityTimeOfDayActivity.Sleep:
                EnterSleep();
                break;

            case EntityTimeOfDayActivity.Burrow:
                EnterBurrow();
                break;
        }
    }

    public void ApplyMovementResult(Vector3 position, AnimalMovementDirective directive, bool blocked)
    {
        if (directive.State != AnimalMovementState.Wander || CurrentTarget is not { } target)
            return;

        if (blocked || GetHorizontalDistance(position, target) <= ArrivalDistance)
            EnterIdle();
    }

    private AnimalMovementDirective TickIdle(Vector3 position, Vector3? threatPosition, float deltaTime)
    {
        if (_timeOfDayActivity == EntityTimeOfDayActivity.Sleep)
        {
            EnterSleep();
            return TickSleep(position, threatPosition);
        }

        if (ShouldFlee(position, threatPosition))
            return TickFlee(position, threatPosition);

        _idleTimeRemaining -= deltaTime;
        if (_idleTimeRemaining > 0f)
            return new AnimalMovementDirective(AnimalMovementState.Idle, Vector3.Zero, 0f, null);

        EnterWander(position);
        return TickWander(position, threatPosition);
    }

    private AnimalMovementDirective TickWander(Vector3 position, Vector3? threatPosition)
    {
        if (_timeOfDayActivity == EntityTimeOfDayActivity.Sleep)
        {
            EnterSleep();
            return TickSleep(position, threatPosition);
        }

        if (ShouldFlee(position, threatPosition))
            return TickFlee(position, threatPosition);

        if (CurrentTarget is not { } target)
        {
            EnterIdle();
            return new AnimalMovementDirective(AnimalMovementState.Idle, Vector3.Zero, 0f, null);
        }

        Vector3 desired = target - position;
        desired.Y = 0f;

        if (desired.LengthSquared() <= ArrivalDistance * ArrivalDistance)
        {
            EnterIdle();
            return new AnimalMovementDirective(AnimalMovementState.Idle, Vector3.Zero, 0f, null);
        }

        return new AnimalMovementDirective(
            AnimalMovementState.Wander,
            Vector3.Normalize(desired),
            _behaviour.MoveSpeed,
            target);
    }

    private AnimalMovementDirective TickFlee(Vector3 position, Vector3? threatPosition)
    {
        if (!ShouldFlee(position, threatPosition) || threatPosition is not { } threat)
        {
            EnterScheduledRestState();
            return State switch
            {
                AnimalMovementState.Sleep => new AnimalMovementDirective(AnimalMovementState.Sleep, Vector3.Zero, 0f, null),
                AnimalMovementState.Burrow => new AnimalMovementDirective(AnimalMovementState.Burrow, Vector3.Zero, 0f, null),
                _ => new AnimalMovementDirective(AnimalMovementState.Idle, Vector3.Zero, 0f, null)
            };
        }

        Vector3 desired = position - threat;
        desired.Y = 0f;

        if (desired.LengthSquared() <= 0.0001f)
            desired = Vector3.UnitZ;

        return new AnimalMovementDirective(
            AnimalMovementState.Flee,
            Vector3.Normalize(desired),
            _behaviour.FleeSpeed,
            null);
    }

    private AnimalMovementDirective TickSleep(Vector3 position, Vector3? threatPosition)
    {
        if (ShouldFlee(position, threatPosition))
        {
            EnterIdle();
            return new AnimalMovementDirective(AnimalMovementState.Idle, Vector3.Zero, 0f, null);
        }

        return new AnimalMovementDirective(AnimalMovementState.Sleep, Vector3.Zero, 0f, null);
    }

    private void EnterIdle()
    {
        State = AnimalMovementState.Idle;
        CurrentTarget = null;
        _idleTimeRemaining = NextIdleDuration();
    }

    private void EnterWander(Vector3 origin)
    {
        State = AnimalMovementState.Wander;
        CurrentTarget = origin + GetRandomHorizontalOffset(_behaviour.WanderRadius);
    }

    private void EnterFlee()
    {
        State = AnimalMovementState.Flee;
        CurrentTarget = null;
    }

    private void EnterSleep()
    {
        State = AnimalMovementState.Sleep;
        CurrentTarget = null;
    }

    private void EnterBurrow()
    {
        State = AnimalMovementState.Burrow;
        CurrentTarget = null;
    }

    private void EnterScheduledRestState()
    {
        if (_timeOfDayActivity == EntityTimeOfDayActivity.Sleep)
            EnterSleep();
        else if (_timeOfDayActivity == EntityTimeOfDayActivity.Burrow)
            EnterBurrow();
        else
            EnterIdle();
    }

    private bool ShouldFlee(Vector3 position, Vector3? threatPosition)
        => threatPosition is { } threat && GetHorizontalDistance(position, threat) <= _behaviour.FleeRadius;

    private float NextIdleDuration()
    {
        if (MathF.Abs(_behaviour.IdleTimeMax - _behaviour.IdleTimeMin) <= float.Epsilon)
            return _behaviour.IdleTimeMin;

        return (float)(_behaviour.IdleTimeMin + _random.NextDouble() * (_behaviour.IdleTimeMax - _behaviour.IdleTimeMin));
    }

    private Vector3 GetRandomHorizontalOffset(float radius)
    {
        double angle = _random.NextDouble() * Math.PI * 2.0;
        double distance = Math.Sqrt(_random.NextDouble()) * radius;
        return new Vector3(
            (float)(Math.Cos(angle) * distance),
            0f,
            (float)(Math.Sin(angle) * distance));
    }

    private static float GetHorizontalDistance(Vector3 a, Vector3 b)
    {
        float dx = a.X - b.X;
        float dz = a.Z - b.Z;
        return MathF.Sqrt(dx * dx + dz * dz);
    }

    private static void ValidateBehaviour(EntityBehaviourMetadata behaviour)
    {
        if (behaviour.MoveSpeed < 0f)
            throw new ArgumentOutOfRangeException(nameof(behaviour.MoveSpeed));
        if (behaviour.FleeSpeed < 0f)
            throw new ArgumentOutOfRangeException(nameof(behaviour.FleeSpeed));
        if (behaviour.FleeRadius < 0f)
            throw new ArgumentOutOfRangeException(nameof(behaviour.FleeRadius));
        if (behaviour.IdleTimeMin < 0f || behaviour.IdleTimeMax < behaviour.IdleTimeMin)
            throw new ArgumentOutOfRangeException(nameof(behaviour.IdleTimeMin));
        if (behaviour.WanderRadius < 0f)
            throw new ArgumentOutOfRangeException(nameof(behaviour.WanderRadius));
    }
}
