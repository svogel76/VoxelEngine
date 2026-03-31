using System.Numerics;
using VoxelEngine.Entity.AI;
using VoxelEngine.Entity.Models;
using VoxelEngine.World;

namespace VoxelEngine.Entity;

public sealed class AnimalEntity : TerrainPhysicsEntity, IEntityRenderDataProvider, IEntityUpdatable
{
    private readonly IVoxelModelDefinition _model;
    private readonly global::VoxelEngine.World.World _world;
    private readonly Func<Vector3>? _threatPositionProvider;
    private readonly AnimalMovementStateMachine _stateMachine;

    public AnimalEntity(
        Vector3 position,
        IVoxelModelDefinition model,
        global::VoxelEngine.World.World world,
        Func<Vector3>? threatPositionProvider = null,
        float yawRadians = 0f,
        Random? random = null)
        : base(position, model?.PlacementBounds ?? throw new ArgumentNullException(nameof(model)))
    {
        _model = model;
        _world = world ?? throw new ArgumentNullException(nameof(world));
        _threatPositionProvider = threatPositionProvider;

        if (_model.Metadata.Behaviour is null)
            throw new ArgumentException($"Entity model '{_model.Id}' does not define behaviour metadata.", nameof(model));

        _stateMachine = new AnimalMovementStateMachine(_model.Metadata.Behaviour, random);
        YawRadians = yawRadians;
    }

    public float YawRadians { get; private set; }
    public AnimalMovementState MovementState => _stateMachine.State;

    public EntityTimeOfDayActivity GetTimeOfDayActivity(bool isDay)
        => isDay ? _model.Metadata.Behaviour!.DayActivity : _model.Metadata.Behaviour!.NightActivity;

    public void ApplyTimeOfDay(bool isDay)
        => _stateMachine.ApplyTimeOfDayActivity(GetTimeOfDayActivity(isDay));

    public void Update(double deltaTime)
    {
        float dt = (float)deltaTime;
        Vector3? threatPosition = _threatPositionProvider?.Invoke();
        var directive = _stateMachine.Tick(Position, threatPosition, dt);

        Vector2 desiredVelocity = directive.DesiredDirection.LengthSquared() > 0f
            ? new Vector2(directive.DesiredDirection.X, directive.DesiredDirection.Z) * directive.Speed
            : Vector2.Zero;

        var movement = MoveHorizontally(_world, desiredVelocity, deltaTime);
        if (movement.Displacement.LengthSquared() > 0.000001f)
            YawRadians = MathF.Atan2(movement.Displacement.X, movement.Displacement.Y);

        _stateMachine.ApplyMovementResult(Position, directive, movement.Blocked);
    }

    public EntityRenderInstance GetRenderInstance()
        => new(_model.Id, Position, YawRadians);
}
