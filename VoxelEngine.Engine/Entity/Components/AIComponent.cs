using System.Numerics;
using System.Text.Json;
using VoxelEngine.Api;
using VoxelEngine.Api.Entity;
using VoxelEngine.Entity.AI;
using VoxelEngine.Entity.Models;

namespace VoxelEngine.Entity.Components;

/// <summary>
/// KI-Komponente: steuert Tier-Verhalten (Idle / Wander / Flee / Sleep / Burrow).
/// Liest den Zustand aus AnimalMovementStateMachine und leitet horizontale Bewegung an
/// PhysicsComponent weiter.
/// </summary>
public sealed class AIComponent : IComponent
{
    private readonly global::VoxelEngine.World.World _world;
    private readonly AnimalMovementStateMachine _stateMachine;
    private readonly EntityTimeOfDayActivity _dayActivity;
    private readonly EntityTimeOfDayActivity _nightActivity;
    private readonly Func<Vector3>? _threatPositionProvider;

    public string ComponentId => "ai";

    public float YawRadians { get; private set; }
    public AnimalMovementState State => _stateMachine.State;

    public AIComponent(
        global::VoxelEngine.World.World world,
        EntityBehaviourMetadata behaviour,
        Func<Vector3>? threatPositionProvider = null,
        float yawRadians = 0f,
        Random? random = null)
    {
        _world                  = world     ?? throw new ArgumentNullException(nameof(world));
        _stateMachine           = new AnimalMovementStateMachine(behaviour, random);
        _dayActivity            = behaviour.DayActivity;
        _nightActivity          = behaviour.NightActivity;
        _threatPositionProvider = threatPositionProvider;
        YawRadians              = yawRadians;
    }

    public EntityTimeOfDayActivity GetTimeOfDayActivity(bool isDay)
        => isDay ? _dayActivity : _nightActivity;

    public void ApplyTimeOfDay(bool isDay)
        => _stateMachine.ApplyTimeOfDayActivity(isDay ? _dayActivity : _nightActivity);

    public void Update(IEntity iEntity, IModContext context, double deltaTime)
    {
        if (iEntity is not Entity entity) return;

        float   dt        = (float)deltaTime;
        Vector3 position  = entity.InternalPosition;
        Vector3? threatPos = _threatPositionProvider?.Invoke();
        var     directive  = _stateMachine.Tick(position, threatPos, dt);

        var phys = entity.GetComponent<PhysicsComponent>();
        if (phys is null) return;

        Vector2 desiredVelocity = directive.DesiredDirection.LengthSquared() > 0f
            ? new Vector2(directive.DesiredDirection.X, directive.DesiredDirection.Z) * directive.Speed
            : Vector2.Zero;

        var movement = phys.MoveHorizontally(entity, desiredVelocity, deltaTime);
        if (movement.Displacement.LengthSquared() > 0.000001f)
            YawRadians = MathF.Atan2(movement.Displacement.X, movement.Displacement.Y);

        _stateMachine.ApplyMovementResult(entity.InternalPosition, directive, movement.Blocked);
    }

    public static AIComponent FromJson(
        JsonElement config,
        global::VoxelEngine.World.World world,
        Func<Vector3>? threatProvider = null,
        Random? random = null)
    {
        float moveSpeed  = config.TryGetProperty("move_speed",  out var mp) ? mp.GetSingle() : 2f;
        float fleeRadius = config.TryGetProperty("flee_radius", out var fp) ? fp.GetSingle() : 6f;

        var meta = new EntityBehaviourMetadata
        {
            MoveSpeed     = moveSpeed,
            FleeSpeed     = moveSpeed * 2f,
            FleeRadius    = fleeRadius,
            IdleTimeMin   = 2f,
            IdleTimeMax   = 6f,
            WanderRadius  = 12f,
            DayActivity   = EntityTimeOfDayActivity.Active,
            NightActivity = EntityTimeOfDayActivity.Sleep
        };

        return new AIComponent(world, meta, threatProvider, random: random);
    }
}
