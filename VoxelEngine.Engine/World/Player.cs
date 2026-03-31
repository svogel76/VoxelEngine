using System.Numerics;
using VoxelEngine.Entity;

namespace VoxelEngine.World;

public class Player : Entity.Entity
{
    public const float Width = 0.6f;
    public const float Height = 1.8f;
    public const float EyeHeight = 1.62f;
    public const float SpawnClearance = 2f;
    private const float MaxCollisionSubStep = 0.5f;
    private static readonly BoundingBox PlayerLocalBounds = new(
        new Vector3(-Width * 0.5f, 0f, -Width * 0.5f),
        new Vector3(Width * 0.5f, Height, Width * 0.5f));
    private static readonly TerrainCollisionResolver TerrainCollision = new(PlayerLocalBounds);

    // Position und Velocity kommen von Entity.Entity
    public bool IsOnGround { get; private set; }
    public bool FlyMode { get; private set; } = false;
    public Inventory Inventory { get; }
    public byte SelectedBlock => Inventory.Hotbar[Inventory.SelectedSlot]?.BlockType ?? BlockType.Air;
    public float InteractionReach { get; private set; } = 5f;
    private float _stepVisualOffsetY;

    // Fallschaden-Tracking
    private float _fallStartY;
    private bool  _wasFalling;

    public Vector3 Size => new(Width, Height, Width);
    public Vector3 EyePosition => Position + new Vector3(0f, EyeHeight + _stepVisualOffsetY, 0f);
    public BoundingBox BoundingBox => TerrainCollision.CreateWorldBounds(Position);

    public Player(Vector3 startPosition, bool flyMode = false, VitalsConfig? vitalsConfig = null)
        : base(startPosition, vitalsConfig)
    {
        FlyMode = flyMode;
        Inventory = new Inventory();
        // Startzustand: ein paar Blöcke vorbefüllen
        Inventory.SetSlot(0, new ItemStack(BlockType.Grass, 10));
        Inventory.SetSlot(1, new ItemStack(BlockType.Dirt,  10));
        Inventory.SetSlot(2, new ItemStack(BlockType.Stone, 10));
        Inventory.SetSlot(3, new ItemStack(BlockType.Sand,  10));
    }

    public void ProcessInput(
        PlayerInput input,
        Vector3 lookForward,
        Vector3 lookRight,
        Vector3 lookUp,
        float moveSpeed,
        PlayerPhysicsSettings physics,
        World world,
        double deltaTime)
    {
        float dt = (float)deltaTime;

        if (FlyMode)
        {
            Vector3 movement = Vector3.Zero;

            if (input.MoveForward != 0f)
                movement += lookForward * input.MoveForward;

            if (input.MoveRight != 0f)
                movement += lookRight * input.MoveRight;

            if (input.MoveUp != 0f)
                movement += lookUp * input.MoveUp;

            Velocity = movement.LengthSquared() > 1f
                ? Vector3.Normalize(movement) * moveSpeed
                : movement * moveSpeed;

            Position += Velocity * dt;
            IsOnGround = false;
            _stepVisualOffsetY = 0f;
            return;
        }

        ResolvePenetration(world);

        Vector3 horizontalForward = new(lookForward.X, 0f, lookForward.Z);
        if (horizontalForward.LengthSquared() > 0f)
            horizontalForward = Vector3.Normalize(horizontalForward);

        Vector3 horizontalRight = new(lookRight.X, 0f, lookRight.Z);
        if (horizontalRight.LengthSquared() > 0f)
            horizontalRight = Vector3.Normalize(horizontalRight);

        Vector3 horizontalMovement = horizontalForward * input.MoveForward + horizontalRight * input.MoveRight;
        if (horizontalMovement.LengthSquared() > 1f)
            horizontalMovement = Vector3.Normalize(horizontalMovement);

        float verticalVelocity = Velocity.Y;
        bool hadGroundSupport = IsOnGround || TerrainCollision.HasGroundSupport(Position, world);

        if (input.Jump && hadGroundSupport)
        {
            verticalVelocity = physics.JumpVelocity;
            hadGroundSupport = false;
        }
        else if (!hadGroundSupport)
        {
            verticalVelocity = MathF.Max(verticalVelocity - physics.Gravity * dt, -physics.MaxFallSpeed);
        }
        else
        {
            verticalVelocity = 0f;
        }

        Vector3 desiredVelocity = new(horizontalMovement.X * moveSpeed, verticalVelocity, horizontalMovement.Z * moveSpeed);
        Vector3 nextPosition = Position;
        bool groundedDuringMove = false;

        int steps = Math.Max(1, (int)MathF.Ceiling(desiredVelocity.Length() * dt / MaxCollisionSubStep));
        float subDelta = dt / steps;

        for (int i = 0; i < steps; i++)
        {
            bool canStepUp = groundedDuringMove || hadGroundSupport || TerrainCollision.HasGroundSupport(nextPosition, world);
            ResolveMovementStep(
                ref nextPosition,
                new Vector3(desiredVelocity.X * subDelta, desiredVelocity.Y * subDelta, desiredVelocity.Z * subDelta),
                physics,
                world,
                canStepUp,
                ref desiredVelocity,
                ref groundedDuringMove);
        }

        bool wasOnGroundBefore = IsOnGround;
        Position = nextPosition;
        Velocity = desiredVelocity;
        IsOnGround = groundedDuringMove || TerrainCollision.HasGroundSupport(Position, world);
        UpdateStepVisualOffset(dt, physics.StepUpSmoothingSpeed);

        if (IsOnGround && Velocity.Y < 0f)
            Velocity = new Vector3(Velocity.X, 0f, Velocity.Z);

        // Fallschaden-Tracking: Fallstart und Landeimpact erkennen
        TrackFallDamage(wasOnGroundBefore);
    }

    /// <summary>
    /// Verarbeitet einen Vitalwerte-Tick für Hunger, Regeneration und Verhungern.
    /// Muss einmal pro Update-Schritt mit dem gleichen deltaTime wie ProcessInput aufgerufen werden.
    /// </summary>
    /// <param name="deltaTime">Zeitschritt in Sekunden.</param>
    public void UpdateVitals(double deltaTime)
    {
        bool isMoving = Velocity.X != 0f || Velocity.Z != 0f;
        Vitals.Tick((float)deltaTime, isMoving);
    }

    private void TrackFallDamage(bool wasOnGroundBefore)
    {
        if (FlyMode)
        {
            _wasFalling = false;
            _fallStartY = Position.Y;
            return;
        }

        if (!IsOnGround && Velocity.Y < 0f)
        {
            // Falle gerade
            if (!_wasFalling)
            {
                _fallStartY = Position.Y;
                _wasFalling = true;
            }
        }
        else if (IsOnGround && _wasFalling)
        {
            // Gerade gelandet
            float fallen = _fallStartY - Position.Y;
            Vitals.FallDistance = MathF.Max(0f, fallen);
            Vitals.ApplyFallDamage();
            _wasFalling = false;
        }
        else if (IsOnGround)
        {
            _wasFalling = false;
        }
    }

    public void Teleport(Vector3 feetPosition)
    {
        Position = feetPosition;
        Velocity = Vector3.Zero;
        IsOnGround = false;
        _stepVisualOffsetY = 0f;
        _wasFalling = false;
        Vitals.FallDistance = 0f;
    }

    public void SetFlyMode(bool enabled)
    {
        FlyMode = enabled;
        Velocity = Vector3.Zero;
        IsOnGround = false;
        _stepVisualOffsetY = 0f;
    }

    public void CycleSelectedBlock(int steps)
    {
        if (steps == 0)
            return;

        if (steps > 0)
            for (int i = 0; i < steps; i++)
                Inventory.SelectNext();
        else
            for (int i = 0; i < -steps; i++)
                Inventory.SelectPrevious();
    }

    public void SetInteractionReach(float reach)
    {
        InteractionReach = Math.Max(0.1f, reach);
    }

    public bool WouldIntersectBlock(BlockPosition blockPosition)
    {
        var blockBounds = CreateBlockBoundingBox(blockPosition.X, blockPosition.Y, blockPosition.Z);
        return BoundingBox.Intersects(blockBounds);
    }

    public void SyncPhysics(World world)
    {
        if (FlyMode)
        {
            IsOnGround = false;
            Velocity = Vector3.Zero;
            return;
        }

        Velocity = new Vector3(Velocity.X, 0f, Velocity.Z);
        ResolvePenetration(world);
        IsOnGround = TerrainCollision.HasGroundSupport(Position, world);
        _stepVisualOffsetY = 0f;
        if (IsOnGround)
            Velocity = new Vector3(Velocity.X, 0f, Velocity.Z);
    }

    private void ResolveMovementStep(
        ref Vector3 position,
        Vector3 stepDelta,
        PlayerPhysicsSettings physics,
        World world,
        bool canStepUp,
        ref Vector3 velocity,
        ref bool isGrounded)
    {
        Span<AxisMove> axisMoves = stackalloc AxisMove[3];
        int count = 0;

        if (MathF.Abs(stepDelta.X) > float.Epsilon)
            axisMoves[count++] = new AxisMove(CollisionAxis.X, stepDelta.X, TerrainCollision.EstimateAxisPenetration(position, stepDelta.X, CollisionAxis.X, world));
        if (MathF.Abs(stepDelta.Y) > float.Epsilon)
            axisMoves[count++] = new AxisMove(CollisionAxis.Y, stepDelta.Y, TerrainCollision.EstimateAxisPenetration(position, stepDelta.Y, CollisionAxis.Y, world));
        if (MathF.Abs(stepDelta.Z) > float.Epsilon)
            axisMoves[count++] = new AxisMove(CollisionAxis.Z, stepDelta.Z, TerrainCollision.EstimateAxisPenetration(position, stepDelta.Z, CollisionAxis.Z, world));

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
                TerrainCollision.ResolveAxis(ref position, move.Delta, CollisionAxis.Y, world, ref velocity, ref isGrounded);
            }
            else
            {
                bool stepAllowed = physics.EnableStepUp && canStepUp && (isGrounded || TerrainCollision.HasGroundSupport(position, world));
                ResolveHorizontalAxis(ref position, move.Delta, move.Axis, physics, world, stepAllowed, ref velocity);
            }
        }
    }

    private void ResolveHorizontalAxis(
        ref Vector3 position,
        float delta,
        CollisionAxis axis,
        PlayerPhysicsSettings physics,
        World world,
        bool canStepUp,
        ref Vector3 velocity)
    {
        if (MathF.Abs(delta) <= float.Epsilon)
            return;

        Vector3 candidate = position;
        SetAxis(ref candidate, axis, GetAxis(candidate, axis) + delta);

        if (!TerrainCollision.Collides(world, candidate))
        {
            position = candidate;
            return;
        }

        if (physics.EnableStepUp && canStepUp && TryStepUp(ref position, delta, axis, physics.StepHeight, physics.StepUpMaxVisualDrop, world))
            return;

        float resolved = TerrainCollision.ResolveAxisPosition(position, candidate, axis, world);
        SetAxis(ref position, axis, resolved);
        ZeroVelocityAxis(ref velocity, axis);
    }

    private bool TryStepUp(ref Vector3 position, float delta, CollisionAxis axis, float stepHeight, float maxVisualDrop, World world)
    {
        if (stepHeight <= 0f)
            return false;

        Vector3 raised = position with { Y = position.Y + stepHeight };
        if (TerrainCollision.Collides(world, raised))
            return false;

        Vector3 advanced = raised;
        SetAxis(ref advanced, axis, GetAxis(advanced, axis) + delta);
        if (TerrainCollision.Collides(world, advanced))
            return false;

        Vector3 descentVelocity = Vector3.Zero;
        bool grounded = false;
        TerrainCollision.ResolveAxis(ref advanced, -stepHeight, CollisionAxis.Y, world, ref descentVelocity, ref grounded);
        if (!grounded)
            return false;

        float stepAmount = advanced.Y - position.Y;
        position = advanced;
        ApplyStepVisualOffset(stepAmount, maxVisualDrop);
        return true;
    }

    private void ApplyStepVisualOffset(float stepAmount, float maxVisualDrop)
    {
        if (stepAmount <= 0f || maxVisualDrop <= 0f)
            return;

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

    private void ResolvePenetration(World world)
    {
        var position = Position;
        TerrainCollision.ResolvePenetration(world, ref position);
        Position = position;
    }

    private static BoundingBox CreateBlockBoundingBox(int x, int y, int z)
        => new(new Vector3(x, y, z), new Vector3(x + 1f, y + 1f, z + 1f));

    private static float GetAxis(Vector3 vector, CollisionAxis axis) => axis switch
    {
        CollisionAxis.X => vector.X,
        CollisionAxis.Y => vector.Y,
        _ => vector.Z
    };

    private static void SetAxis(ref Vector3 vector, CollisionAxis axis, float value)
    {
        vector = axis switch
        {
            CollisionAxis.X => vector with { X = value },
            CollisionAxis.Y => vector with { Y = value },
            _ => vector with { Z = value }
        };
    }

    private static void ZeroVelocityAxis(ref Vector3 velocity, CollisionAxis axis)
    {
        velocity = axis switch
        {
            CollisionAxis.X => velocity with { X = 0f },
            CollisionAxis.Y => velocity with { Y = 0f },
            _ => velocity with { Z = 0f }
        };
    }

    private readonly record struct AxisMove(CollisionAxis Axis, float Delta, float Penetration);
}

public readonly record struct PlayerInput(float MoveForward, float MoveRight, float MoveUp, bool Jump);

public readonly record struct PlayerPhysicsSettings(
    float Gravity,
    float MaxFallSpeed,
    float JumpVelocity,
    float StepHeight,
    float StepUpMaxVisualDrop,
    float StepUpSmoothingSpeed,
    bool EnableStepUp);
