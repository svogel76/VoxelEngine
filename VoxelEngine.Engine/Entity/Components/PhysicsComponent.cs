using System.Numerics;
using System.Text.Json;
using VoxelEngine.Api;
using VoxelEngine.Api.Entity;
using VoxelEngine.World;

namespace VoxelEngine.Entity.Components;

/// <summary>
/// Verwaltet Gravitation, Geschwindigkeitsintegration und AABB-Terrain-Kollision fuer eine Entity.
/// Update() uebernimmt die vertikale Physik (Gravitation + Kollision).
/// MoveHorizontally() wird von AIComponent / InputComponent fuer horizontale Bewegung aufgerufen.
/// </summary>
public sealed class PhysicsComponent : IComponent, IEntityBoundsProvider
{
    private const float MaxCollisionSubStep = 0.5f;

    private readonly global::VoxelEngine.World.World _world;
    private readonly float _gravity;
    private readonly float _maxFallSpeed;
    private readonly float _fallDamageThreshold;
    private readonly float _fallDamageMultiplier;
    private readonly float _stepHeight;
    private readonly bool _enableStepUp;
    private readonly float _stepUpMaxVisualDrop;
    private readonly float _stepUpSmoothingSpeed;
    private readonly TerrainCollisionResolver _terrainCollision;

    private float _stepVisualOffsetY;
    private float _lastFrameVerticalVelocity;

    public string ComponentId => "physics";

    public float Width { get; }
    public float Height { get; }
    public bool IsOnGround { get; private set; }
    public bool FlyMode { get; private set; }

    public BoundingBox Bounds => _terrainCollision.CreateWorldBounds(_ownerPosition);

    // Owner position cache (updated at start of each Update)
    private Vector3 _ownerPosition;

    public PhysicsComponent(
        global::VoxelEngine.World.World world,
        float width,
        float height,
        float gravity,
        float maxFallSpeed,
        float fallDamageThreshold = 8.0f,
        float fallDamageMultiplier = 1.0f,
        float stepHeight = 0f,
        bool enableStepUp = false,
        float stepUpMaxVisualDrop = 0f,
        float stepUpSmoothingSpeed = 0f)
    {
        _world = world ?? throw new ArgumentNullException(nameof(world));
        Width = width;
        Height = height;
        _gravity = gravity;
        _maxFallSpeed = maxFallSpeed;
        _fallDamageThreshold = MathF.Max(0f, fallDamageThreshold);
        _fallDamageMultiplier = MathF.Max(0f, fallDamageMultiplier);
        _stepHeight = stepHeight;
        _enableStepUp = enableStepUp;
        _stepUpMaxVisualDrop = stepUpMaxVisualDrop;
        _stepUpSmoothingSpeed = stepUpSmoothingSpeed;

        var localBounds = new BoundingBox(
            new Vector3(-width * 0.5f, 0f, -width * 0.5f),
            new Vector3(width * 0.5f, height, width * 0.5f));
        _terrainCollision = new TerrainCollisionResolver(localBounds);
    }

    public float EyeOffset { get; set; }

    public Vector3 GetEyePosition(Vector3 entityPosition)
        => entityPosition + new Vector3(0f, EyeOffset + _stepVisualOffsetY, 0f);

    public void SetFlyMode(bool enabled)
    {
        FlyMode = enabled;
        _stepVisualOffsetY = 0f;
    }

    public void SyncPhysics(Entity entity)
    {
        ref var pos = ref entity.InternalPosition;
        ref var vel = ref entity.InternalVelocity;

        if (FlyMode)
        {
            IsOnGround = false;
            vel = Vector3.Zero;
            _lastFrameVerticalVelocity = 0f;
            return;
        }

        vel = new Vector3(vel.X, 0f, vel.Z);
        _terrainCollision.ResolvePenetration(_world, ref pos);
        IsOnGround = _terrainCollision.HasGroundSupport(pos, _world);
        _stepVisualOffsetY = 0f;
        if (IsOnGround)
            vel = new Vector3(vel.X, 0f, vel.Z);
        _ownerPosition = pos;
        _lastFrameVerticalVelocity = vel.Y;
    }

    public HorizontalMovementResult MoveHorizontally(Entity entity, Vector2 desiredHorizontalVelocity, double deltaTime)
    {
        float dt = (float)deltaTime;
        ref var pos = ref entity.InternalPosition;
        ref var vel = ref entity.InternalVelocity;
        Vector3 startPosition = pos;

        Vector3 nextPosition = pos;
        Vector3 nextVelocity = new(desiredHorizontalVelocity.X, vel.Y, desiredHorizontalVelocity.Y);
        bool grounded = IsOnGround;

        _terrainCollision.ResolveAxis(ref nextPosition, desiredHorizontalVelocity.X * dt, CollisionAxis.X, _world, ref nextVelocity, ref grounded);
        _terrainCollision.ResolveAxis(ref nextPosition, desiredHorizontalVelocity.Y * dt, CollisionAxis.Z, _world, ref nextVelocity, ref grounded);

        pos = nextPosition;
        vel = nextVelocity;
        _ownerPosition = pos;

        Vector2 actualDisplacement = new(nextPosition.X - startPosition.X, nextPosition.Z - startPosition.Z);
        Vector2 expectedDisplacement = desiredHorizontalVelocity * dt;
        bool blocked = desiredHorizontalVelocity.LengthSquared() > 0f &&
                       actualDisplacement.LengthSquared() + 0.000001f < expectedDisplacement.LengthSquared();

        return new HorizontalMovementResult(actualDisplacement, blocked);
    }

    public void ProcessPlayerInput(
        Entity entity,
        PlayerInput input,
        Vector3 lookForward,
        Vector3 lookRight,
        Vector3 lookUp,
        float moveSpeed,
        double deltaTime)
    {
        float dt = (float)deltaTime;
        ref var pos = ref entity.InternalPosition;
        ref var vel = ref entity.InternalVelocity;
        float previousVerticalVelocity = _lastFrameVerticalVelocity;

        if (FlyMode)
        {
            Vector3 movement = Vector3.Zero;
            if (input.MoveForward != 0f) movement += lookForward * input.MoveForward;
            if (input.MoveRight != 0f) movement += lookRight * input.MoveRight;
            if (input.MoveUp != 0f) movement += lookUp * input.MoveUp;

            vel = movement.LengthSquared() > 1f
                ? Vector3.Normalize(movement) * moveSpeed
                : movement * moveSpeed;

            pos += vel * dt;
            IsOnGround = false;
            _stepVisualOffsetY = 0f;
            UpdateStepVisualOffset(dt, _stepUpSmoothingSpeed);
            _ownerPosition = pos;
            _lastFrameVerticalVelocity = 0f;
            return;
        }

        _terrainCollision.ResolvePenetration(_world, ref pos);

        Vector3 horizontalForward = new(lookForward.X, 0f, lookForward.Z);
        if (horizontalForward.LengthSquared() > 0f)
            horizontalForward = Vector3.Normalize(horizontalForward);

        Vector3 horizontalRight = new(lookRight.X, 0f, lookRight.Z);
        if (horizontalRight.LengthSquared() > 0f)
            horizontalRight = Vector3.Normalize(horizontalRight);

        Vector3 horizontalMovement = horizontalForward * input.MoveForward + horizontalRight * input.MoveRight;
        if (horizontalMovement.LengthSquared() > 1f)
            horizontalMovement = Vector3.Normalize(horizontalMovement);

        float verticalVelocity = vel.Y;
        bool hadGroundSupport = IsOnGround || _terrainCollision.HasGroundSupport(pos, _world);

        if (input.Jump && hadGroundSupport)
        {
            verticalVelocity = 0f;
            hadGroundSupport = false;
        }
        else if (!hadGroundSupport)
        {
            verticalVelocity = MathF.Max(verticalVelocity - _gravity * dt, -_maxFallSpeed);
        }
        else
        {
            verticalVelocity = 0f;
        }

        Vector3 desiredVelocity = new(horizontalMovement.X * moveSpeed, verticalVelocity, horizontalMovement.Z * moveSpeed);
        Vector3 nextPosition = pos;
        bool groundedDuringMove = false;
        int steps = Math.Max(1, (int)MathF.Ceiling(desiredVelocity.Length() * dt / MaxCollisionSubStep));
        float subDelta = dt / steps;

        for (int i = 0; i < steps; i++)
        {
            bool canStepUp = groundedDuringMove || hadGroundSupport || _terrainCollision.HasGroundSupport(nextPosition, _world);
            ResolveMovementStep(
                ref nextPosition,
                new Vector3(desiredVelocity.X * subDelta, desiredVelocity.Y * subDelta, desiredVelocity.Z * subDelta),
                canStepUp,
                ref desiredVelocity,
                ref groundedDuringMove);
        }

        bool wasOnGroundBefore = IsOnGround;
        pos = nextPosition;
        vel = desiredVelocity;
        IsOnGround = groundedDuringMove || _terrainCollision.HasGroundSupport(pos, _world);
        UpdateStepVisualOffset(dt, _stepUpSmoothingSpeed);
        _ownerPosition = pos;

        if (IsOnGround && vel.Y < 0f)
            vel = new Vector3(vel.X, 0f, vel.Z);

        TrackFallDamage(entity, previousVerticalVelocity);
        _lastFrameVerticalVelocity = IsOnGround && !wasOnGroundBefore ? 0f : vel.Y;
    }

    public void ApplyJumpVelocity(Entity entity, float jumpVelocity)
    {
        ref var vel = ref entity.InternalVelocity;
        vel = new Vector3(vel.X, jumpVelocity, vel.Z);
    }

    public void Teleport(Entity entity, Vector3 feetPosition)
    {
        entity.InternalPosition = feetPosition;
        entity.InternalVelocity = Vector3.Zero;
        IsOnGround = false;
        _stepVisualOffsetY = 0f;
        FallDistance = 0f;
        _wasFalling = false;
        _lastFrameVerticalVelocity = 0f;
        _ownerPosition = feetPosition;
    }

    public bool WouldIntersectBlock(Entity entity, int bx, int by, int bz)
    {
        var blockBounds = new BoundingBox(new Vector3(bx, by, bz), new Vector3(bx + 1f, by + 1f, bz + 1f));
        return Bounds.Intersects(blockBounds);
    }

    public float FallDistance { get; set; }
    private float _fallStartY;
    private bool _wasFalling;

    private void TrackFallDamage(Entity entity, float previousVerticalVelocity)
    {
        if (FlyMode)
        {
            _wasFalling = false;
            _fallStartY = entity.InternalPosition.Y;
            return;
        }

        if (!IsOnGround && entity.InternalVelocity.Y < 0f)
        {
            if (!_wasFalling)
            {
                _fallStartY = entity.InternalPosition.Y;
                _wasFalling = true;
            }
        }
        else if (IsOnGround && _wasFalling)
        {
            FallDistance = MathF.Max(0f, _fallStartY - entity.InternalPosition.Y);
            ApplyFallDamage(entity, previousVerticalVelocity);
            _wasFalling = false;
        }
        else if (IsOnGround)
        {
            _wasFalling = false;
        }
    }

    public void Update(IEntity iEntity, IModContext context, double deltaTime)
    {
        if (iEntity is not Entity entity) return;
        if (FlyMode) return;

        float dt = (float)deltaTime;
        ref var pos = ref entity.InternalPosition;
        ref var vel = ref entity.InternalVelocity;
        float previousVerticalVelocity = _lastFrameVerticalVelocity;

        _terrainCollision.ResolvePenetration(_world, ref pos);

        float verticalVelocity = vel.Y;
        bool hadGroundSupport = IsOnGround || _terrainCollision.HasGroundSupport(pos, _world);

        if (!hadGroundSupport)
            verticalVelocity = MathF.Max(verticalVelocity - _gravity * dt, -_maxFallSpeed);
        else if (verticalVelocity < 0f)
            verticalVelocity = 0f;

        Vector3 nextPosition = pos;
        Vector3 nextVelocity = new(vel.X, verticalVelocity, vel.Z);
        bool groundedDuringMove = false;
        _terrainCollision.ResolveAxis(ref nextPosition, nextVelocity.Y * dt, CollisionAxis.Y, _world, ref nextVelocity, ref groundedDuringMove);

        pos = nextPosition;
        vel = nextVelocity;
        IsOnGround = groundedDuringMove || _terrainCollision.HasGroundSupport(pos, _world);
        _ownerPosition = pos;

        if (IsOnGround && vel.Y < 0f)
            vel = vel with { Y = 0f };

        TrackFallDamage(entity, previousVerticalVelocity);
        _lastFrameVerticalVelocity = vel.Y;
    }

    private void ApplyFallDamage(Entity entity, float previousVerticalVelocity)
    {
        float impactVelocity = MathF.Max(0f, -previousVerticalVelocity);
        if (impactVelocity <= _fallDamageThreshold || _fallDamageMultiplier <= 0f)
            return;

        float fallDamage = (impactVelocity - _fallDamageThreshold) * _fallDamageMultiplier;
        entity.GetComponent<HealthComponent>()?.TakeDamage(fallDamage);
    }

    private void ResolveMovementStep(
        ref Vector3 position,
        Vector3 stepDelta,
        bool canStepUp,
        ref Vector3 velocity,
        ref bool isGrounded)
    {
        Span<AxisMove> axisMoves = stackalloc AxisMove[3];
        int count = 0;

        if (MathF.Abs(stepDelta.X) > float.Epsilon)
            axisMoves[count++] = new AxisMove(CollisionAxis.X, stepDelta.X, _terrainCollision.EstimateAxisPenetration(position, stepDelta.X, CollisionAxis.X, _world));
        if (MathF.Abs(stepDelta.Y) > float.Epsilon)
            axisMoves[count++] = new AxisMove(CollisionAxis.Y, stepDelta.Y, _terrainCollision.EstimateAxisPenetration(position, stepDelta.Y, CollisionAxis.Y, _world));
        if (MathF.Abs(stepDelta.Z) > float.Epsilon)
            axisMoves[count++] = new AxisMove(CollisionAxis.Z, stepDelta.Z, _terrainCollision.EstimateAxisPenetration(position, stepDelta.Z, CollisionAxis.Z, _world));

        for (int i = 0; i < count - 1; i++)
        for (int j = i + 1; j < count; j++)
        {
            if (axisMoves[j].Penetration < axisMoves[i].Penetration)
                (axisMoves[i], axisMoves[j]) = (axisMoves[j], axisMoves[i]);
        }

        for (int i = 0; i < count; i++)
        {
            var move = axisMoves[i];
            if (move.Axis == CollisionAxis.Y)
            {
                _terrainCollision.ResolveAxis(ref position, move.Delta, CollisionAxis.Y, _world, ref velocity, ref isGrounded);
            }
            else
            {
                bool stepAllowed = _enableStepUp && canStepUp && (isGrounded || _terrainCollision.HasGroundSupport(position, _world));
                ResolveHorizontalAxis(ref position, move.Delta, move.Axis, stepAllowed, ref velocity);
            }
        }
    }

    private void ResolveHorizontalAxis(ref Vector3 position, float delta, CollisionAxis axis, bool canStepUp, ref Vector3 velocity)
    {
        if (MathF.Abs(delta) <= float.Epsilon) return;

        Vector3 candidate = position;
        SetAxis(ref candidate, axis, GetAxis(candidate, axis) + delta);

        if (!_terrainCollision.Collides(_world, candidate))
        {
            position = candidate;
            return;
        }

        if (_enableStepUp && canStepUp && TryStepUp(ref position, delta, axis))
            return;

        float resolved = _terrainCollision.ResolveAxisPosition(position, candidate, axis, _world);
        SetAxis(ref position, axis, resolved);
        ZeroVelocityAxis(ref velocity, axis);
    }

    private bool TryStepUp(ref Vector3 position, float delta, CollisionAxis axis)
    {
        if (_stepHeight <= 0f) return false;

        Vector3 raised = position with { Y = position.Y + _stepHeight };
        if (_terrainCollision.Collides(_world, raised)) return false;

        Vector3 advanced = raised;
        SetAxis(ref advanced, axis, GetAxis(advanced, axis) + delta);
        if (_terrainCollision.Collides(_world, advanced)) return false;

        Vector3 descentVelocity = Vector3.Zero;
        bool grounded = false;
        _terrainCollision.ResolveAxis(ref advanced, -_stepHeight, CollisionAxis.Y, _world, ref descentVelocity, ref grounded);
        if (!grounded) return false;

        float stepAmount = advanced.Y - position.Y;
        position = advanced;
        ApplyStepVisualOffset(stepAmount, _stepUpMaxVisualDrop);
        return true;
    }

    private void ApplyStepVisualOffset(float stepAmount, float maxVisualDrop)
    {
        if (stepAmount <= 0f || maxVisualDrop <= 0f) return;
        float appliedDrop = MathF.Min(stepAmount, maxVisualDrop);
        _stepVisualOffsetY = MathF.Min(_stepVisualOffsetY, -appliedDrop);
    }

    private void UpdateStepVisualOffset(float deltaTime, float smoothingSpeed)
    {
        if (_stepVisualOffsetY >= 0f)
        {
            _stepVisualOffsetY = 0f;
            return;
        }

        float recover = MathF.Max(0.01f, smoothingSpeed) * deltaTime;
        _stepVisualOffsetY = MathF.Min(0f, _stepVisualOffsetY + recover);
    }

    private static float GetAxis(Vector3 v, CollisionAxis axis) => axis switch
    {
        CollisionAxis.X => v.X,
        CollisionAxis.Y => v.Y,
        _ => v.Z
    };

    private static void SetAxis(ref Vector3 v, CollisionAxis axis, float value)
    {
        v = axis switch
        {
            CollisionAxis.X => v with { X = value },
            CollisionAxis.Y => v with { Y = value },
            _ => v with { Z = value }
        };
    }

    private static void ZeroVelocityAxis(ref Vector3 v, CollisionAxis axis)
    {
        v = axis switch
        {
            CollisionAxis.X => v with { X = 0f },
            CollisionAxis.Y => v with { Y = 0f },
            _ => v with { Z = 0f }
        };
    }

    private readonly record struct AxisMove(CollisionAxis Axis, float Delta, float Penetration);
    public readonly record struct HorizontalMovementResult(Vector2 Displacement, bool Blocked);

    public static PhysicsComponent FromJson(JsonElement config, global::VoxelEngine.World.World world, Core.EngineSettings settings)
    {
        float width = config.TryGetProperty("width", out var wp) ? wp.GetSingle() : 0.6f;
        float height = config.TryGetProperty("height", out var hp) ? hp.GetSingle() : 0.9f;
        return new PhysicsComponent(
            world,
            width,
            height,
            settings.Gravity,
            settings.MaxFallSpeed,
            settings.FallDamageThreshold,
            settings.FallDamageMultiplier);
    }
}

