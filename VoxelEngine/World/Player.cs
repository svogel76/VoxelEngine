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
    private const float MaxCollisionSubStep = 0.5f;

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
    public BoundingBox BoundingBox => CreateBoundingBox(Position);

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
        bool hadGroundSupport = IsOnGround || HasGroundSupport(Position, world);

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
            bool canStepUp = groundedDuringMove || hadGroundSupport || HasGroundSupport(nextPosition, world);
            ResolveMovementStep(
                ref nextPosition,
                new Vector3(desiredVelocity.X * subDelta, desiredVelocity.Y * subDelta, desiredVelocity.Z * subDelta),
                physics,
                world,
                canStepUp,
                ref desiredVelocity,
                ref groundedDuringMove);
        }

        Position = nextPosition;
        Velocity = desiredVelocity;
        IsOnGround = groundedDuringMove || HasGroundSupport(Position, world);
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
        IsOnGround = HasGroundSupport(Position, world);
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
            axisMoves[count++] = new AxisMove(Axis.X, stepDelta.X, EstimateAxisPenetration(position, stepDelta.X, Axis.X, world));
        if (MathF.Abs(stepDelta.Y) > float.Epsilon)
            axisMoves[count++] = new AxisMove(Axis.Y, stepDelta.Y, EstimateAxisPenetration(position, stepDelta.Y, Axis.Y, world));
        if (MathF.Abs(stepDelta.Z) > float.Epsilon)
            axisMoves[count++] = new AxisMove(Axis.Z, stepDelta.Z, EstimateAxisPenetration(position, stepDelta.Z, Axis.Z, world));

        for (int i = 0; i < count - 1; i++)
        for (int j = i + 1; j < count; j++)
        {
            if (axisMoves[j].Penetration < axisMoves[i].Penetration)
                (axisMoves[i], axisMoves[j]) = (axisMoves[j], axisMoves[i]);
        }

        for (int i = 0; i < count; i++)
        {
            var move = axisMoves[i];
            if (move.Axis == Axis.Y)
            {
                ResolveAxis(ref position, move.Delta, Axis.Y, world, ref velocity, ref isGrounded);
            }
            else
            {
                bool stepAllowed = physics.EnableStepUp && canStepUp && (isGrounded || HasGroundSupport(position, world));
                ResolveHorizontalAxis(ref position, move.Delta, move.Axis, physics, world, stepAllowed, ref velocity);
            }
        }
    }

    private void ResolveHorizontalAxis(
        ref Vector3 position,
        float delta,
        Axis axis,
        PlayerPhysicsSettings physics,
        World world,
        bool canStepUp,
        ref Vector3 velocity)
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
        ZeroVelocityAxis(ref velocity, axis);
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

        if (axis == Axis.Y && delta < 0f)
            isGrounded = true;

        ZeroVelocityAxis(ref velocity, axis);
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

    private void ResolvePenetration(World world)
    {
        const int maxIterations = 8;

        for (int i = 0; i < maxIterations; i++)
        {
            BoundingBox playerBox = CreateBoundingBox(Position);
            PenetrationResolution? smallest = null;

            foreach (var block in GetIntersectingSolidBlocks(world, playerBox))
            {
                BoundingBox blockBox = CreateBlockBoundingBox(block.X, block.Y, block.Z);
                foreach (var resolution in GetPenetrationResolutions(playerBox, blockBox))
                {
                    if (smallest is null || resolution.Distance < smallest.Value.Distance)
                        smallest = resolution;
                }
            }

            if (smallest is null)
                return;

            Position += smallest.Value.Offset;
        }
    }

    private IEnumerable<PenetrationResolution> GetPenetrationResolutions(BoundingBox playerBox, BoundingBox blockBox)
    {
        float overlapLeft = playerBox.Max.X - blockBox.Min.X;
        float overlapRight = blockBox.Max.X - playerBox.Min.X;
        float overlapDown = playerBox.Max.Y - blockBox.Min.Y;
        float overlapUp = blockBox.Max.Y - playerBox.Min.Y;
        float overlapBack = playerBox.Max.Z - blockBox.Min.Z;
        float overlapFront = blockBox.Max.Z - playerBox.Min.Z;

        yield return overlapLeft < overlapRight
            ? new PenetrationResolution(new Vector3(-overlapLeft, 0f, 0f), overlapLeft)
            : new PenetrationResolution(new Vector3(overlapRight, 0f, 0f), overlapRight);

        yield return overlapDown < overlapUp
            ? new PenetrationResolution(new Vector3(0f, -overlapDown, 0f), overlapDown)
            : new PenetrationResolution(new Vector3(0f, overlapUp, 0f), overlapUp);

        yield return overlapBack < overlapFront
            ? new PenetrationResolution(new Vector3(0f, 0f, -overlapBack), overlapBack)
            : new PenetrationResolution(new Vector3(0f, 0f, overlapFront), overlapFront);
    }

    private float EstimateAxisPenetration(Vector3 position, float delta, Axis axis, World world)
    {
        Vector3 candidate = position;
        SetAxis(ref candidate, axis, GetAxis(candidate, axis) + delta);
        BoundingBox candidateBox = CreateBoundingBox(candidate);

        float penetration = 0f;
        bool collided = false;

        foreach (var block in GetIntersectingSolidBlocks(world, candidateBox))
        {
            collided = true;
            BoundingBox blockBox = CreateBlockBoundingBox(block.X, block.Y, block.Z);
            float overlap = GetAxisPenetration(candidateBox, blockBox, axis, delta > 0f);
            penetration = penetration <= 0f ? overlap : MathF.Min(penetration, overlap);
        }

        return collided ? penetration : 0f;
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
            if (!BlockRegistry.CollidesWithPlayer(world.GetBlock(x, y, z)))
                continue;

            yield return (x, y, z);
        }
    }

    private float ResolveAxisPosition(Vector3 current, Vector3 candidate, Axis axis, World world)
    {
        BoundingBox candidateBox = CreateBoundingBox(candidate);
        float resolved = GetAxis(candidate, axis);
        bool movingPositive = GetAxis(candidate, axis) > GetAxis(current, axis);

        foreach (var block in GetIntersectingSolidBlocks(world, candidateBox))
        {
            BoundingBox blockBox = CreateBlockBoundingBox(block.X, block.Y, block.Z);
            float penetration = GetAxisPenetration(candidateBox, blockBox, axis, movingPositive);
            resolved = movingPositive
                ? MathF.Min(resolved, GetAxis(candidate, axis) - penetration)
                : MathF.Max(resolved, GetAxis(candidate, axis) + penetration);
        }

        return resolved;
    }

    private static float GetAxisPenetration(BoundingBox playerBox, BoundingBox blockBox, Axis axis, bool movingPositive) => axis switch
    {
        Axis.X => movingPositive
            ? playerBox.Max.X - blockBox.Min.X
            : blockBox.Max.X - playerBox.Min.X,
        Axis.Y => movingPositive
            ? playerBox.Max.Y - blockBox.Min.Y
            : blockBox.Max.Y - playerBox.Min.Y,
        _ => movingPositive
            ? playerBox.Max.Z - blockBox.Min.Z
            : blockBox.Max.Z - playerBox.Min.Z
    };

    private BoundingBox CreateBoundingBox(Vector3 feetPosition)
    {
        float halfWidth = Width * 0.5f;
        return new(
            new Vector3(feetPosition.X - halfWidth, feetPosition.Y, feetPosition.Z - halfWidth),
            new Vector3(feetPosition.X + halfWidth, feetPosition.Y + Height, feetPosition.Z + halfWidth));
    }

    private static BoundingBox CreateBlockBoundingBox(int x, int y, int z)
        => new(new Vector3(x, y, z), new Vector3(x + 1f, y + 1f, z + 1f));

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

    private static void ZeroVelocityAxis(ref Vector3 velocity, Axis axis)
    {
        velocity = axis switch
        {
            Axis.X => velocity with { X = 0f },
            Axis.Y => velocity with { Y = 0f },
            _ => velocity with { Z = 0f }
        };
    }

    private readonly record struct AxisMove(Axis Axis, float Delta, float Penetration);

    private readonly record struct PenetrationResolution(Vector3 Offset, float Distance);

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
