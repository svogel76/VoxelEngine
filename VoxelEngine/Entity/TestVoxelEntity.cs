using System.Numerics;
using VoxelEngine.Entity.Models;
using VoxelEngine.World;

namespace VoxelEngine.Entity;

public sealed class TestVoxelEntity : Entity, IEntityBoundsProvider, IEntityRenderDataProvider
{
    private readonly IVoxelModelDefinition _model;

    public float YawRadians { get; }

    public BoundingBox Bounds => new(Position + _model.PlacementBounds.Min, Position + _model.PlacementBounds.Max);

    public TestVoxelEntity(Vector3 position, IVoxelModelDefinition model, float yawRadians = 0f)
        : base(position)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
        YawRadians = yawRadians;
    }

    public EntityRenderInstance GetRenderInstance()
        => new(_model.Id, Position, YawRadians);
}
