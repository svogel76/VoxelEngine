using System.Numerics;
using VoxelEngine.World;

namespace VoxelEngine.Entity;

public abstract class TerrainPhysicsEntity : Entity, IEntityBoundsProvider
{
    protected readonly TerrainCollisionResolver TerrainCollision;

    protected TerrainPhysicsEntity(Vector3 startPosition, BoundingBox localBounds, VitalsConfig? vitalsConfig = null)
        : base(startPosition, vitalsConfig)
    {
        LocalBounds = localBounds;
        TerrainCollision = new TerrainCollisionResolver(localBounds);
    }

    public BoundingBox LocalBounds { get; }
    public BoundingBox Bounds => TerrainCollision.CreateWorldBounds(Position);
    public bool IsOnGround { get; private set; }

    public void ApplyTerrainPhysics(global::VoxelEngine.World.World world, EntityPhysicsSettings settings, double deltaTime)
    {
        float dt = (float)deltaTime;

        ResolveTerrainPenetration(world);

        float verticalVelocity = Velocity.Y;
        bool hadGroundSupport = IsOnGround || TerrainCollision.HasGroundSupport(Position, world);

        if (!hadGroundSupport)
        {
            verticalVelocity = MathF.Max(verticalVelocity - settings.Gravity * dt, -settings.MaxFallSpeed);
        }
        else if (verticalVelocity < 0f)
        {
            verticalVelocity = 0f;
        }

        Vector3 nextPosition = Position;
        Vector3 nextVelocity = new(Velocity.X, verticalVelocity, Velocity.Z);
        bool groundedDuringMove = false;
        TerrainCollision.ResolveAxis(ref nextPosition, nextVelocity.Y * dt, CollisionAxis.Y, world, ref nextVelocity, ref groundedDuringMove);

        Position = nextPosition;
        Velocity = nextVelocity;
        IsOnGround = groundedDuringMove || TerrainCollision.HasGroundSupport(Position, world);

        if (IsOnGround && Velocity.Y < 0f)
            Velocity = Velocity with { Y = 0f };
    }

    public void SyncTerrainPhysics(global::VoxelEngine.World.World world)
    {
        Velocity = Velocity with { Y = 0f };
        ResolveTerrainPenetration(world);
        IsOnGround = TerrainCollision.HasGroundSupport(Position, world);

        if (IsOnGround)
            Velocity = Velocity with { Y = 0f };
    }

    protected HorizontalMovementResult MoveHorizontally(global::VoxelEngine.World.World world, Vector2 desiredHorizontalVelocity, double deltaTime)
    {
        float dt = (float)deltaTime;
        Vector3 startPosition = Position;
        Vector3 nextPosition = Position;
        Vector3 nextVelocity = new(desiredHorizontalVelocity.X, Velocity.Y, desiredHorizontalVelocity.Y);
        bool grounded = IsOnGround;

        TerrainCollision.ResolveAxis(ref nextPosition, desiredHorizontalVelocity.X * dt, CollisionAxis.X, world, ref nextVelocity, ref grounded);
        TerrainCollision.ResolveAxis(ref nextPosition, desiredHorizontalVelocity.Y * dt, CollisionAxis.Z, world, ref nextVelocity, ref grounded);

        Position = nextPosition;
        Velocity = nextVelocity;

        Vector2 actualDisplacement = new(nextPosition.X - startPosition.X, nextPosition.Z - startPosition.Z);
        Vector2 expectedDisplacement = desiredHorizontalVelocity * dt;
        bool blocked = desiredHorizontalVelocity.LengthSquared() > 0f &&
                       actualDisplacement.LengthSquared() + 0.000001f < expectedDisplacement.LengthSquared();

        return new HorizontalMovementResult(actualDisplacement, blocked);
    }

    private void ResolveTerrainPenetration(global::VoxelEngine.World.World world)
    {
        var position = Position;
        TerrainCollision.ResolvePenetration(world, ref position);
        Position = position;
    }

    protected readonly record struct HorizontalMovementResult(Vector2 Displacement, bool Blocked);
}
