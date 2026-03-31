using System.Numerics;

namespace VoxelEngine.Entity;

public readonly record struct EntityRenderInstance(string ModelId, Vector3 Position, float YawRadians);
