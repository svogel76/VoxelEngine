namespace VoxelEngine.Entity.Models;

public sealed class EntityModelMetadata
{
    public float? Scale { get; init; }
    public EntityModelRotation? Rotation { get; init; }
    public EntityModelBounds? Bounds { get; init; }
}

public sealed class EntityModelRotation
{
    public float X { get; init; }
    public float Y { get; init; }
    public float Z { get; init; }

    public bool IsIdentity => X == 0f && Y == 0f && Z == 0f;
}

public sealed class EntityModelBounds
{
    public EntityModelBoundsAxis Min { get; init; } = new();
    public EntityModelBoundsAxis Max { get; init; } = new();
}

public sealed class EntityModelBoundsAxis
{
    public float X { get; init; }
    public float Y { get; init; }
    public float Z { get; init; }
}
