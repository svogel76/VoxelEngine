using System.Numerics;
using VoxelEngine.World;

namespace VoxelEngine.Entity.Models;

public sealed class VoxelModelDefinition : IVoxelModelDefinition
{
    public string Id { get; }
    public float VoxelSize { get; }
    public BoundingBox PlacementBounds { get; }
    public EntityModelMetadata Metadata { get; }
    public IReadOnlyList<VoxelModelVoxel> Voxels { get; }

    public VoxelModelDefinition(string id, float voxelSize, IReadOnlyList<VoxelModelVoxel> voxels, EntityModelMetadata? metadata = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(voxels);

        if (voxelSize <= 0f)
            throw new ArgumentOutOfRangeException(nameof(voxelSize));
        if (voxels.Count == 0)
            throw new ArgumentException("A voxel model must contain at least one voxel.", nameof(voxels));

        Id = id;
        VoxelSize = voxelSize;
        Voxels = voxels;
        Metadata = metadata ?? EntityModelMetadata.Empty;
        PlacementBounds = CreatePlacementBounds(voxelSize, voxels);
    }

    private static BoundingBox CreatePlacementBounds(float voxelSize, IReadOnlyList<VoxelModelVoxel> voxels)
    {
        float minX = float.MaxValue;
        float minY = float.MaxValue;
        float minZ = float.MaxValue;
        float maxX = float.MinValue;
        float maxY = float.MinValue;
        float maxZ = float.MinValue;

        foreach (var voxel in voxels)
        {
            minX = MathF.Min(minX, voxel.X * voxelSize);
            minY = MathF.Min(minY, voxel.Y * voxelSize);
            minZ = MathF.Min(minZ, voxel.Z * voxelSize);
            maxX = MathF.Max(maxX, (voxel.X + 1) * voxelSize);
            maxY = MathF.Max(maxY, (voxel.Y + 1) * voxelSize);
            maxZ = MathF.Max(maxZ, (voxel.Z + 1) * voxelSize);
        }

        float pivotX = (minX + maxX) * 0.5f;
        float pivotZ = (minZ + maxZ) * 0.5f;

        return new BoundingBox(
            new Vector3(minX - pivotX, minY, minZ - pivotZ),
            new Vector3(maxX - pivotX, maxY, maxZ - pivotZ));
    }
}
