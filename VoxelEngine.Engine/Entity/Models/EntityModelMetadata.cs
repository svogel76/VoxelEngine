using System.Text.Json;
using System.Text.Json.Serialization;

namespace VoxelEngine.Entity.Models;

public sealed class EntityModelMetadata
{
    public static EntityModelMetadata Empty { get; } = new();

    public EntityDisplayMetadata Display { get; init; } = new();
    public EntityAiMetadata? Ai { get; init; }
    public EntitySoundMetadata Sounds { get; init; } = new();
    public IReadOnlyList<EntityDropMetadata> Drops { get; init; } = [];

    // Legacy flat metadata stays supported so existing files keep working.
    public float? Scale { get; init; }
    public EntityModelRotation? Rotation { get; init; }
    public EntityModelBounds? Bounds { get; init; }

    public float? GetScaleOverride()
        => Display.Scale ?? Scale;

    public EntityModelRotation? GetRotationOverride()
        => Display.Rotation ?? Rotation;

    public EntityBoundingBoxMetadata? GetBoundingBoxOverride()
        => Display.BoundingBox;

    public EntityModelBounds? GetLegacyBoundsOverride()
        => Bounds;
}

public sealed class EntityAiMetadata
{
    [JsonPropertyName("behaviour_tree")]
    public JsonElement BehaviourTree { get; init; }

    public bool HasBehaviourTree => BehaviourTree.ValueKind is not JsonValueKind.Undefined and not JsonValueKind.Null;
}

public sealed class EntityDisplayMetadata
{
    public string? Model { get; init; }
    public float? Scale { get; init; }
    public EntityBoundingBoxMetadata? BoundingBox { get; init; }
    public EntityAnimationMetadata Animations { get; init; } = new();
    public EntityModelRotation? Rotation { get; init; }
}

public sealed class EntityBoundingBoxMetadata
{
    public float Width { get; init; }
    public float Height { get; init; }
}

public sealed class EntityAnimationMetadata
{
    public string? Idle { get; init; }
    public string? Walk { get; init; }
    public string? Flee { get; init; }
    public string? Hit { get; init; }
    public string? Death { get; init; }
}

public sealed class EntitySoundMetadata
{
    public string? Idle { get; init; }
    public string? Flee { get; init; }
    public string? Hurt { get; init; }
    public string? Death { get; init; }
}

public sealed class EntityDropMetadata
{
    public string? Item { get; init; }
    public int? Count { get; init; }
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
