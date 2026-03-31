using System.Numerics;

namespace VoxelEngine.World;

public readonly record struct BoundingBox(Vector3 Min, Vector3 Max)
{
    public bool Intersects(BoundingBox other) =>
        Max.X > other.Min.X && Min.X < other.Max.X &&
        Max.Y > other.Min.Y && Min.Y < other.Max.Y &&
        Max.Z > other.Min.Z && Min.Z < other.Max.Z;

    public BoundingBox Translate(float x, float y, float z)
        => new(Min + new Vector3(x, y, z), Max + new Vector3(x, y, z));
}
