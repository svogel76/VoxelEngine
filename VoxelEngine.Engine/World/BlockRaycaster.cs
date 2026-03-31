using System.Numerics;

namespace VoxelEngine.World;

public static class BlockRaycaster
{
    private const float Epsilon = 0.0001f;

    public static BlockRaycastHit? Raycast(
        World world,
        Vector3 origin,
        Vector3 direction,
        float maxDistance,
        bool ignoreWater = false)
    {
        if (direction.LengthSquared() <= 0f)
            return null;

        direction = Vector3.Normalize(direction);

        int x = (int)MathF.Floor(origin.X);
        int y = (int)MathF.Floor(origin.Y);
        int z = (int)MathF.Floor(origin.Z);

        int stepX = Math.Sign(direction.X);
        int stepY = Math.Sign(direction.Y);
        int stepZ = Math.Sign(direction.Z);

        float tDeltaX = stepX != 0 ? MathF.Abs(1f / direction.X) : float.PositiveInfinity;
        float tDeltaY = stepY != 0 ? MathF.Abs(1f / direction.Y) : float.PositiveInfinity;
        float tDeltaZ = stepZ != 0 ? MathF.Abs(1f / direction.Z) : float.PositiveInfinity;

        float tMaxX = InitialTMax(origin.X, direction.X, x, stepX);
        float tMaxY = InitialTMax(origin.Y, direction.Y, y, stepY);
        float tMaxZ = InitialTMax(origin.Z, direction.Z, z, stepZ);

        var previous = new BlockPosition(x, y, z);
        float distance = 0f;

        while (distance <= maxDistance)
        {
            if (y >= 0 && y < Chunk.Height)
            {
                byte blockType = world.GetBlock(x, y, z);
                bool isIgnoredWater = ignoreWater && blockType == BlockType.Water;
                if (blockType != BlockType.Air && !isIgnoredWater)
                {
                    var blockPosition = new BlockPosition(x, y, z);
                    return new BlockRaycastHit(
                        blockPosition,
                        previous,
                        blockPosition - previous,
                        blockType,
                        distance);
                }
            }

            previous = new BlockPosition(x, y, z);

            if (tMaxX < tMaxY)
            {
                if (tMaxX < tMaxZ)
                {
                    x += stepX;
                    distance = tMaxX;
                    tMaxX += tDeltaX;
                }
                else
                {
                    z += stepZ;
                    distance = tMaxZ;
                    tMaxZ += tDeltaZ;
                }
            }
            else
            {
                if (tMaxY < tMaxZ)
                {
                    y += stepY;
                    distance = tMaxY;
                    tMaxY += tDeltaY;
                }
                else
                {
                    z += stepZ;
                    distance = tMaxZ;
                    tMaxZ += tDeltaZ;
                }
            }
        }

        return null;
    }

    private static float InitialTMax(float origin, float direction, int cell, int step)
    {
        if (step == 0)
            return float.PositiveInfinity;

        float nextBoundary = step > 0 ? cell + 1f : cell;
        return (nextBoundary - origin + (step < 0 ? -Epsilon : Epsilon)) / direction;
    }
}

public readonly record struct BlockPosition(int X, int Y, int Z)
{
    public static BlockPosition operator -(BlockPosition left, BlockPosition right)
        => new(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
}

public readonly record struct BlockRaycastHit(
    BlockPosition BlockPosition,
    BlockPosition PlacementPosition,
    BlockPosition HitNormal,
    byte BlockType,
    float Distance);

public readonly record struct BlockPlacementPreview(BlockPosition Position, byte BlockType);
