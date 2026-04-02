using System.Numerics;

namespace VoxelEngine.Rendering;

public readonly record struct FogProfile(float StartDistance, float EndDistance, Vector3 Color);
