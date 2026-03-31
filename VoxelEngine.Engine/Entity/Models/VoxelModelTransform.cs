using System.Numerics;

namespace VoxelEngine.Entity.Models;

public static class VoxelModelTransform
{
    public static IVoxelModelDefinition ApplyRotation(IVoxelModelDefinition model, EntityModelRotation? rotation)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (rotation is null || rotation.IsIdentity)
            return model;

        var bounds = GetVoxelBounds(model.Voxels);
        return new VoxelModelDefinition(
            model.Id,
            model.VoxelSize,
            RotateVoxels(model.Voxels, rotation, bounds.MinX, bounds.MinY, bounds.MinZ, bounds.MaxX, bounds.MaxY, bounds.MaxZ));
    }

    public static IReadOnlyList<VoxelModelVoxel> RotateVoxels(IReadOnlyList<VoxelModelVoxel> voxels, EntityModelRotation rotation)
    {
        ArgumentNullException.ThrowIfNull(voxels);
        ArgumentNullException.ThrowIfNull(rotation);

        var bounds = GetVoxelBounds(voxels);
        return RotateVoxels(voxels, rotation, bounds.MinX, bounds.MinY, bounds.MinZ, bounds.MaxX, bounds.MaxY, bounds.MaxZ);
    }

    public static IReadOnlyList<VoxelModelVoxel> RotateVoxels(
        IReadOnlyList<VoxelModelVoxel> voxels,
        EntityModelRotation rotation,
        int minX,
        int minY,
        int minZ,
        int maxX,
        int maxY,
        int maxZ)
    {
        ArgumentNullException.ThrowIfNull(voxels);
        ArgumentNullException.ThrowIfNull(rotation);

        int xTurns = NormalizeQuarterTurns(rotation.X, nameof(rotation.X));
        int yTurns = NormalizeQuarterTurns(rotation.Y, nameof(rotation.Y));
        int zTurns = NormalizeQuarterTurns(rotation.Z, nameof(rotation.Z));

        if (xTurns == 0 && yTurns == 0 && zTurns == 0)
            return voxels.ToArray();

        var transformedBounds = TransformBounds(minX, minY, minZ, maxX, maxY, maxZ, xTurns, yTurns, zTurns);
        var transformed = new List<TransformedVoxel>(voxels.Count);

        foreach (var voxel in voxels)
        {
            var position = TransformVoxelPosition(voxel, xTurns, yTurns, zTurns);
            transformed.Add(new TransformedVoxel(position.X, position.Y, position.Z, voxel.TileX, voxel.TileY, voxel.Tint));
        }

        var deduplicated = new Dictionary<(int X, int Y, int Z), VoxelModelVoxel>();
        foreach (var voxel in transformed)
        {
            int x = voxel.X - transformedBounds.MinX;
            int y = voxel.Y - transformedBounds.MinY;
            int z = voxel.Z - transformedBounds.MinZ;
            deduplicated[(x, y, z)] = new VoxelModelVoxel(x, y, z, voxel.TileX, voxel.TileY, voxel.Tint);
        }

        return deduplicated.Values
            .OrderBy(voxel => voxel.X)
            .ThenBy(voxel => voxel.Y)
            .ThenBy(voxel => voxel.Z)
            .ToArray();
    }

    private static (int X, int Y, int Z) TransformVoxelPosition(VoxelModelVoxel voxel, int xTurns, int yTurns, int zTurns)
    {
        Span<Vector3> corners =
        [
            new Vector3(voxel.X, voxel.Y, voxel.Z),
            new Vector3(voxel.X + 1, voxel.Y, voxel.Z),
            new Vector3(voxel.X, voxel.Y + 1, voxel.Z),
            new Vector3(voxel.X + 1, voxel.Y + 1, voxel.Z),
            new Vector3(voxel.X, voxel.Y, voxel.Z + 1),
            new Vector3(voxel.X + 1, voxel.Y, voxel.Z + 1),
            new Vector3(voxel.X, voxel.Y + 1, voxel.Z + 1),
            new Vector3(voxel.X + 1, voxel.Y + 1, voxel.Z + 1)
        ];

        int minX = int.MaxValue;
        int minY = int.MaxValue;
        int minZ = int.MaxValue;

        foreach (var corner in corners)
        {
            Vector3 transformedCorner = ApplyRotation(corner, xTurns, yTurns, zTurns);
            minX = Math.Min(minX, RoundToInt(transformedCorner.X));
            minY = Math.Min(minY, RoundToInt(transformedCorner.Y));
            minZ = Math.Min(minZ, RoundToInt(transformedCorner.Z));
        }

        return (minX, minY, minZ);
    }

    private static (int MinX, int MinY, int MinZ, int MaxX, int MaxY, int MaxZ) TransformBounds(
        int minX,
        int minY,
        int minZ,
        int maxX,
        int maxY,
        int maxZ,
        int xTurns,
        int yTurns,
        int zTurns)
    {
        Span<Vector3> corners =
        [
            new Vector3(minX, minY, minZ),
            new Vector3(maxX, minY, minZ),
            new Vector3(minX, maxY, minZ),
            new Vector3(maxX, maxY, minZ),
            new Vector3(minX, minY, maxZ),
            new Vector3(maxX, minY, maxZ),
            new Vector3(minX, maxY, maxZ),
            new Vector3(maxX, maxY, maxZ)
        ];

        int transformedMinX = int.MaxValue;
        int transformedMinY = int.MaxValue;
        int transformedMinZ = int.MaxValue;
        int transformedMaxX = int.MinValue;
        int transformedMaxY = int.MinValue;
        int transformedMaxZ = int.MinValue;

        foreach (var corner in corners)
        {
            Vector3 transformedCorner = ApplyRotation(corner, xTurns, yTurns, zTurns);
            int x = RoundToInt(transformedCorner.X);
            int y = RoundToInt(transformedCorner.Y);
            int z = RoundToInt(transformedCorner.Z);

            transformedMinX = Math.Min(transformedMinX, x);
            transformedMinY = Math.Min(transformedMinY, y);
            transformedMinZ = Math.Min(transformedMinZ, z);
            transformedMaxX = Math.Max(transformedMaxX, x);
            transformedMaxY = Math.Max(transformedMaxY, y);
            transformedMaxZ = Math.Max(transformedMaxZ, z);
        }

        return (transformedMinX, transformedMinY, transformedMinZ, transformedMaxX, transformedMaxY, transformedMaxZ);
    }

    private static (int MinX, int MinY, int MinZ, int MaxX, int MaxY, int MaxZ) GetVoxelBounds(IReadOnlyList<VoxelModelVoxel> voxels)
    {
        int minX = int.MaxValue;
        int minY = int.MaxValue;
        int minZ = int.MaxValue;
        int maxX = int.MinValue;
        int maxY = int.MinValue;
        int maxZ = int.MinValue;

        foreach (var voxel in voxels)
        {
            minX = Math.Min(minX, voxel.X);
            minY = Math.Min(minY, voxel.Y);
            minZ = Math.Min(minZ, voxel.Z);
            maxX = Math.Max(maxX, voxel.X + 1);
            maxY = Math.Max(maxY, voxel.Y + 1);
            maxZ = Math.Max(maxZ, voxel.Z + 1);
        }

        return (minX, minY, minZ, maxX, maxY, maxZ);
    }

    private static Vector3 ApplyRotation(Vector3 value, int xTurns, int yTurns, int zTurns)
    {
        for (int i = 0; i < xTurns; i++)
            value = new Vector3(value.X, -value.Z, value.Y);

        for (int i = 0; i < yTurns; i++)
            value = new Vector3(value.Z, value.Y, -value.X);

        for (int i = 0; i < zTurns; i++)
            value = new Vector3(-value.Y, value.X, value.Z);

        return value;
    }

    private static int NormalizeQuarterTurns(float degrees, string parameterName)
    {
        float turns = degrees / 90f;
        int rounded = (int)MathF.Round(turns);
        if (MathF.Abs(turns - rounded) > 0.0001f)
            throw new ArgumentException("Only 90-degree rotation steps are supported for voxel models.", parameterName);

        rounded %= 4;
        if (rounded < 0)
            rounded += 4;

        return rounded;
    }

    private static int RoundToInt(float value)
    {
        int rounded = (int)MathF.Round(value);
        if (MathF.Abs(value - rounded) > 0.0001f)
            throw new InvalidOperationException("Voxel rotation produced a non-integer coordinate.");

        return rounded;
    }

    private readonly record struct TransformedVoxel(int X, int Y, int Z, int TileX, int TileY, VoxelTint Tint);
}
