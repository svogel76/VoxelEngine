using System.Numerics;

namespace VoxelEngine.World;

public sealed class Player
{
    public const float Width = 0.6f;
    public const float Height = 1.8f;
    public const float EyeHeight = 1.62f;
    public const float SpawnClearance = 2f;
    private const float CollisionEpsilon = 0.001f;
    private const float GroundProbeDistance = 0.05f;
    private static readonly byte[] SelectableBlocks =
    {
        BlockType.Grass,
        BlockType.Dirt,
        BlockType.Stone,
        BlockType.Sand
    };

    public Vector3 Position { get; private set; }
    public Vector3 Velocity { get; private set; }
    public bool IsOnGround { get; private set; }
    public bool FlyMode { get; private set; } = false;
    public byte SelectedBlock { get; private set; } = BlockType.Grass;
    public float InteractionReach { get; private set; } = 5f;
    private float _stepVisualOffsetY;

    public Vector3 Size => new(Width, Height, Width);
    public Vector3 EyePosition => Position + new Vector3(0f, EyeHeight + _stepVisualOffsetY, 0f);
    public BoundingBox BoundingBox => new(Position, Position + Size);

    public Player(Vector3 startPosition, bool flyMode = false)
    {
        Position = startPosition;
        FlyMode = flyMode;
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

        ResolvePenetrationUp(world);

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
        bool wasOnGround = IsOnGround || HasGroundSupport(Position, world);

        if (input.Jump && wasOnGround)
        {
            verticalVelocity = physics.JumpVelocity;
            wasOnGround = false;
        }
        else if (!wasOnGround)
        {
            verticalVelocity = MathF.Max(verticalVelocity - physics.Gravity * dt, -physics.MaxFallSpeed);
        }
        else
        {
            verticalVelocity = 0f;
        }

        Vector3 desiredVelocity = new(horizontalMovement.X * moveSpeed, verticalVelocity, horizontalMovement.Z * moveSpeed);
        Vector3 nextPosition = Position;
        bool isGrounded = false;

        ResolveHorizontalAxis(ref nextPosition, desiredVelocity.X * dt, Axis.X, physics, world, wasOnGround);
        ResolveAxis(ref nextPosition, desiredVelocity.Y * dt, Axis.Y, world, ref desiredVelocity, ref isGrounded);
        ResolveHorizontalAxis(ref nextPosition, desiredVelocity.Z * dt, Axis.Z, physics, world, wasOnGround);

        Position = nextPosition;
        Velocity = desiredVelocity;
        IsOnGround = isGrounded || HasGroundSupport(Position, world);
        UpdateStepVisualOffset(dt, physics.StepUpSmoothingSpeed);

        if (IsOnGround && Velocity.Y < 0f)
            Velocity = new Vector3(Velocity.X, 0f, Velocity.Z);
    }

    public void Teleport(Vector3 feetPosition)
    {
        Position = feetPosition;
        Velocity = Vector3.Zero;
        IsOnGround = false;
        _stepVisualOffsetY = 0f;
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

        int currentIndex = Array.IndexOf(SelectableBlocks, SelectedBlock);
        if (currentIndex < 0)
            currentIndex = 0;

        int nextIndex = (currentIndex + steps) % SelectableBlocks.Length;
        if (nextIndex < 0)
            nextIndex += SelectableBlocks.Length;

        SelectedBlock = SelectableBlocks[nextIndex];
    }

    public void SetInteractionReach(float reach)
    {
        InteractionReach = Math.Max(0.1f, reach);
    }

    public bool WouldIntersectBlock(BlockPosition blockPosition)
    {
        var blockBounds = new BoundingBox(
            new Vector3(blockPosition.X, blockPosition.Y, blockPosition.Z),
            new Vector3(blockPosition.X + 1f, blockPosition.Y + 1f, blockPosition.Z + 1f));
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
        ResolvePenetrationUp(world);
        IsOnGround = HasGroundSupport(Position, world);
        _stepVisualOffsetY = 0f;
        if (IsOnGround)
            Velocity = new Vector3(Velocity.X, 0f, Velocity.Z);
    }

    private void ResolveHorizontalAxis(
        ref Vector3 position,
        float delta,
        Axis axis,
        PlayerPhysicsSettings physics,
        World world,
        bool canStepUp)
    {
        if (MathF.Abs(delta) <= float.Epsilon)
            return;

        Vector3 candidate = position;
        SetAxis(ref candidate, axis, GetAxis(candidate, axis) + delta);

        if (!Collides(world, candidate))
        {
            position = candidate;
            return;
        }

        if (physics.EnableStepUp && canStepUp && TryStepUp(ref position, delta, axis, physics.StepHeight, world))
            return;

        float resolved = ResolveAxisPosition(position, candidate, axis, world);
        SetAxis(ref position, axis, resolved);
    }

    private void ResolveAxis(
        ref Vector3 position,
        float delta,
        Axis axis,
        World world,
        ref Vector3 velocity,
        ref bool isGrounded)
    {
        if (MathF.Abs(delta) <= float.Epsilon)
            return;

        Vector3 candidate = position;
        SetAxis(ref candidate, axis, GetAxis(candidate, axis) + delta);

        if (!Collides(world, candidate))
        {
            position = candidate;
            return;
        }

        float resolved = ResolveAxisPosition(position, candidate, axis, world);
        SetAxis(ref position, axis, resolved);

        if (axis == Axis.Y)
        {
            if (delta < 0f)
                isGrounded = true;

            velocity = new Vector3(velocity.X, 0f, velocity.Z);
        }
        else if (axis == Axis.X)
        {
            velocity = new Vector3(0f, velocity.Y, velocity.Z);
        }
        else
        {
            velocity = new Vector3(velocity.X, velocity.Y, 0f);
        }
    }

    private bool TryStepUp(ref Vector3 position, float delta, Axis axis, float stepHeight, World world)
    {
        if (stepHeight <= 0f)
            return false;

        Vector3 raised = position with { Y = position.Y + stepHeight };
        if (Collides(world, raised))
            return false;

        Vector3 advanced = raised;
        SetAxis(ref advanced, axis, GetAxis(advanced, axis) + delta);
        if (Collides(world, advanced))
            return false;

        Vector3 descentVelocity = Vector3.Zero;
        bool grounded = false;
        ResolveAxis(ref advanced, -stepHeight, Axis.Y, world, ref descentVelocity, ref grounded);
        if (!grounded)
            return false;

        float stepAmount = advanced.Y - position.Y;
        position = advanced;
        ApplyStepVisualOffset(stepAmount);
        return true;
    }

    private void ApplyStepVisualOffset(float stepAmount)
    {
        if (stepAmount <= 0f)
            return;

        _stepVisualOffsetY -= stepAmount;
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

    private void ResolvePenetrationUp(World world)
    {
        if (!Collides(world, Position))
            return;

        float resolvedY = Position.Y;
        foreach (var block in GetIntersectingSolidBlocks(world, BoundingBox))
            resolvedY = MathF.Max(resolvedY, block.Y + 1f);

        Position = new Vector3(Position.X, resolvedY, Position.Z);
    }

    private bool HasGroundSupport(Vector3 position, World world)
    {
        var probe = CreateBoundingBox(position).Translate(0f, -GroundProbeDistance, 0f);
        return IntersectsSolid(world, probe);
    }

    private bool Collides(World world, Vector3 feetPosition)
        => IntersectsSolid(world, CreateBoundingBox(feetPosition));

    private bool IntersectsSolid(World world, BoundingBox box)
    {
        foreach (var _ in GetIntersectingSolidBlocks(world, box))
            return true;
        return false;
    }

    private IEnumerable<(int X, int Y, int Z)> GetIntersectingSolidBlocks(World world, BoundingBox box)
    {
        int minX = (int)MathF.Floor(box.Min.X);
        int maxX = (int)MathF.Floor(box.Max.X - CollisionEpsilon);
        int minY = (int)MathF.Floor(box.Min.Y);
        int maxY = (int)MathF.Floor(box.Max.Y - CollisionEpsilon);
        int minZ = (int)MathF.Floor(box.Min.Z);
        int maxZ = (int)MathF.Floor(box.Max.Z - CollisionEpsilon);

        for (int x = minX; x <= maxX; x++)
        for (int y = minY; y <= maxY; y++)
        for (int z = minZ; z <= maxZ; z++)
        {
            if (!BlockType.IsSolid(world.GetBlock(x, y, z)))
                continue;

            yield return (x, y, z);
        }
    }

    private float ResolveAxisPosition(Vector3 current, Vector3 candidate, Axis axis, World world)
    {
        BoundingBox candidateBox = CreateBoundingBox(candidate);
        float resolved = GetAxis(candidate, axis);

        foreach (var block in GetIntersectingSolidBlocks(world, candidateBox))
        {
            resolved = axis switch
            {
                Axis.X when GetAxis(candidate, axis) > GetAxis(current, axis) => MathF.Min(resolved, block.X - Width),
                Axis.X => MathF.Max(resolved, block.X + 1f),
                Axis.Y when GetAxis(candidate, axis) > GetAxis(current, axis) => MathF.Min(resolved, block.Y - Height),
                Axis.Y => MathF.Max(resolved, block.Y + 1f),
                Axis.Z when GetAxis(candidate, axis) > GetAxis(current, axis) => MathF.Min(resolved, block.Z - Width),
                Axis.Z => MathF.Max(resolved, block.Z + 1f),
                _ => resolved
            };
        }

        return resolved;
    }

    private BoundingBox CreateBoundingBox(Vector3 feetPosition)
        => new(feetPosition, feetPosition + Size);

    private static float GetAxis(Vector3 vector, Axis axis) => axis switch
    {
        Axis.X => vector.X,
        Axis.Y => vector.Y,
        _ => vector.Z
    };

    private static void SetAxis(ref Vector3 vector, Axis axis, float value)
    {
        vector = axis switch
        {
            Axis.X => vector with { X = value },
            Axis.Y => vector with { Y = value },
            _ => vector with { Z = value }
        };
    }

    private enum Axis
    {
        X,
        Y,
        Z
    }
}

public readonly record struct PlayerInput(float MoveForward, float MoveRight, float MoveUp, bool Jump);

public readonly record struct PlayerPhysicsSettings(
    float Gravity,
    float MaxFallSpeed,
    float JumpVelocity,
    float StepHeight,
    float StepUpSmoothingSpeed,
    bool EnableStepUp);

public readonly record struct BoundingBox(Vector3 Min, Vector3 Max)
{
    public bool Intersects(BoundingBox other) =>
        Max.X > other.Min.X && Min.X < other.Max.X &&
        Max.Y > other.Min.Y && Min.Y < other.Max.Y &&
        Max.Z > other.Min.Z && Min.Z < other.Max.Z;

    public BoundingBox Translate(float x, float y, float z)
        => new(Min + new Vector3(x, y, z), Max + new Vector3(x, y, z));
}
