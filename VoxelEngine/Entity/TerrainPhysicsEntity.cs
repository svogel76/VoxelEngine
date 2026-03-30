using System.Numerics;
using VoxelEngine.World;

namespace VoxelEngine.Entity;

public abstract class TerrainPhysicsEntity : Entity, IEntityBoundsProvider
{
    private readonly TerrainCollisionResolver _terrainCollision;

    protected TerrainPhysicsEntity(Vector3 startPosition, BoundingBox localBounds, VitalsConfig? vitalsConfig = null)
        : base(startPosition, vitalsConfig)
    {
        LocalBounds = localBounds;
        _terrainCollision = new TerrainCollisionResolver(localBounds);
    }

    public BoundingBox LocalBounds { get; }
    public BoundingBox Bounds => _terrainCollision.CreateWorldBounds(Position);
    public bool IsOnGround { get; private set; }

    public void ApplyTerrainPhysics(global::VoxelEngine.World.World world, EntityPhysicsSettings settings, double deltaTime)
    {
        float dt = (float)deltaTime;

        ResolveTerrainPenetration(world);

        float verticalVelocity = Velocity.Y;
        bool hadGroundSupport = IsOnGround || _terrainCollision.HasGroundSupport(Position, world);

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
        _terrainCollision.ResolveAxis(ref nextPosition, nextVelocity.Y * dt, CollisionAxis.Y, world, ref nextVelocity, ref groundedDuringMove);

        Position = nextPosition;
        Velocity = nextVelocity;
        IsOnGround = groundedDuringMove || _terrainCollision.HasGroundSupport(Position, world);

        if (IsOnGround && Velocity.Y < 0f)
            Velocity = Velocity with { Y = 0f };
    }

    public void SyncTerrainPhysics(global::VoxelEngine.World.World world)
    {
        Velocity = Velocity with { Y = 0f };
        ResolveTerrainPenetration(world);
        IsOnGround = _terrainCollision.HasGroundSupport(Position, world);

        if (IsOnGround)
            Velocity = Velocity with { Y = 0f };
    }

    private void ResolveTerrainPenetration(global::VoxelEngine.World.World world)
    {
        var position = Position;
        _terrainCollision.ResolvePenetration(world, ref position);
        Position = position;
    }
}
