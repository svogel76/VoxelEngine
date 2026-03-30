using System.Numerics;

namespace VoxelEngine.Entity.AI;

public readonly record struct AnimalMovementDirective(
    AnimalMovementState State,
    Vector3 DesiredDirection,
    float Speed,
    Vector3? TargetPosition);
