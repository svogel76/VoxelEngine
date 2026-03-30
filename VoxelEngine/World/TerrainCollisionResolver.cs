using System.Numerics;

namespace VoxelEngine.World;

public sealed class TerrainCollisionResolver
{
    private readonly BoundingBox _localBounds;
    private readonly float _collisionEpsilon;
    private readonly float _groundProbeDistance;

    public TerrainCollisionResolver(BoundingBox localBounds, float groundProbeDistance = 0.05f, float collisionEpsilon = 0.001f)
    {
        _localBounds = localBounds;
        _groundProbeDistance = groundProbeDistance;
        _collisionEpsilon = collisionEpsilon;
    }

    public BoundingBox CreateWorldBounds(Vector3 position)
        => new(position + _localBounds.Min, position + _localBounds.Max);

    public void ResolvePenetration(World world, ref Vector3 position)
    {
        const int maxIterations = 8;

        for (int i = 0; i < maxIterations; i++)
        {
            BoundingBox entityBox = CreateWorldBounds(position);
            PenetrationResolution? smallest = null;

            foreach (var block in GetIntersectingSolidBlocks(world, entityBox))
            {
                BoundingBox blockBox = CreateBlockBoundingBox(block.X, block.Y, block.Z);
                foreach (var resolution in GetPenetrationResolutions(entityBox, blockBox))
                {
                    if (smallest is null || resolution.Distance < smallest.Value.Distance)
                        smallest = resolution;
                }
            }

            if (smallest is null)
                return;

            position += smallest.Value.Offset;
        }
    }

    public float EstimateAxisPenetration(Vector3 position, float delta, CollisionAxis axis, World world)
    {
        Vector3 candidate = position;
        SetAxis(ref candidate, axis, GetAxis(candidate, axis) + delta);
        BoundingBox candidateBox = CreateWorldBounds(candidate);

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

    public void ResolveAxis(
        ref Vector3 position,
        float delta,
        CollisionAxis axis,
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

        if (axis == CollisionAxis.Y && delta < 0f)
            isGrounded = true;

        ZeroVelocityAxis(ref velocity, axis);
    }

    public bool HasGroundSupport(Vector3 position, World world)
    {
        var probe = CreateWorldBounds(position).Translate(0f, -_groundProbeDistance, 0f);
        return IntersectsSolid(world, probe);
    }

    public bool Collides(World world, Vector3 position)
        => IntersectsSolid(world, CreateWorldBounds(position));

    public float ResolveAxisPosition(Vector3 current, Vector3 candidate, CollisionAxis axis, World world)
    {
        BoundingBox candidateBox = CreateWorldBounds(candidate);
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

    private bool IntersectsSolid(World world, BoundingBox box)
    {
        foreach (var _ in GetIntersectingSolidBlocks(world, box))
            return true;

        return false;
    }

    private IEnumerable<(int X, int Y, int Z)> GetIntersectingSolidBlocks(World world, BoundingBox box)
    {
        int minX = (int)MathF.Floor(box.Min.X);
        int maxX = (int)MathF.Floor(box.Max.X - _collisionEpsilon);
        int minY = (int)MathF.Floor(box.Min.Y);
        int maxY = (int)MathF.Floor(box.Max.Y - _collisionEpsilon);
        int minZ = (int)MathF.Floor(box.Min.Z);
        int maxZ = (int)MathF.Floor(box.Max.Z - _collisionEpsilon);

        for (int x = minX; x <= maxX; x++)
        for (int y = minY; y <= maxY; y++)
        for (int z = minZ; z <= maxZ; z++)
        {
            if (!BlockRegistry.CollidesWithPlayer(world.GetBlock(x, y, z)))
                continue;

            yield return (x, y, z);
        }
    }

    private static IEnumerable<PenetrationResolution> GetPenetrationResolutions(BoundingBox entityBox, BoundingBox blockBox)
    {
        float overlapLeft = entityBox.Max.X - blockBox.Min.X;
        float overlapRight = blockBox.Max.X - entityBox.Min.X;
        float overlapDown = entityBox.Max.Y - blockBox.Min.Y;
        float overlapUp = blockBox.Max.Y - entityBox.Min.Y;
        float overlapBack = entityBox.Max.Z - blockBox.Min.Z;
        float overlapFront = blockBox.Max.Z - entityBox.Min.Z;

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

    private static float GetAxisPenetration(BoundingBox entityBox, BoundingBox blockBox, CollisionAxis axis, bool movingPositive) => axis switch
    {
        CollisionAxis.X => movingPositive
            ? entityBox.Max.X - blockBox.Min.X
            : blockBox.Max.X - entityBox.Min.X,
        CollisionAxis.Y => movingPositive
            ? entityBox.Max.Y - blockBox.Min.Y
            : blockBox.Max.Y - entityBox.Min.Y,
        _ => movingPositive
            ? entityBox.Max.Z - blockBox.Min.Z
            : blockBox.Max.Z - entityBox.Min.Z
    };

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

    private readonly record struct PenetrationResolution(Vector3 Offset, float Distance);
}

public enum CollisionAxis
{
    X,
    Y,
    Z
}
