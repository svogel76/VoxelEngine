using System.Numerics;
using VoxelEngine.Entity.Models;
using VoxelEngine.World;

namespace VoxelEngine.Entity;

public sealed class TestVoxelEntity : TerrainPhysicsEntity, IEntityRenderDataProvider
{
    private readonly IVoxelModelDefinition _model;

    public float YawRadians { get; }

    public TestVoxelEntity(Vector3 position, IVoxelModelDefinition model, float yawRadians = 0f)
        : base(position, model?.PlacementBounds ?? throw new ArgumentNullException(nameof(model)))
    {
        _model = model;
        YawRadians = yawRadians;
    }

    public EntityRenderInstance GetRenderInstance()
        => new(_model.Id, Position, YawRadians);
}
