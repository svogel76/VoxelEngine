using System.Numerics;
using VoxelEngine.World;

namespace VoxelEngine.Entity;

public sealed class ViewFrustum
{
    private readonly Plane[] _planes;

    private ViewFrustum(Plane[] planes)
    {
        _planes = planes;
    }

    public static ViewFrustum FromViewProjection(Matrix4x4 viewProjection)
    {
        var planes = new[]
        {
            Plane.Normalize(new Plane(
                viewProjection.M14 + viewProjection.M11,
                viewProjection.M24 + viewProjection.M21,
                viewProjection.M34 + viewProjection.M31,
                viewProjection.M44 + viewProjection.M41)),
            Plane.Normalize(new Plane(
                viewProjection.M14 - viewProjection.M11,
                viewProjection.M24 - viewProjection.M21,
                viewProjection.M34 - viewProjection.M31,
                viewProjection.M44 - viewProjection.M41)),
            Plane.Normalize(new Plane(
                viewProjection.M14 + viewProjection.M12,
                viewProjection.M24 + viewProjection.M22,
                viewProjection.M34 + viewProjection.M32,
                viewProjection.M44 + viewProjection.M42)),
            Plane.Normalize(new Plane(
                viewProjection.M14 - viewProjection.M12,
                viewProjection.M24 - viewProjection.M22,
                viewProjection.M34 - viewProjection.M32,
                viewProjection.M44 - viewProjection.M42)),
            Plane.Normalize(new Plane(
                viewProjection.M14 + viewProjection.M13,
                viewProjection.M24 + viewProjection.M23,
                viewProjection.M34 + viewProjection.M33,
                viewProjection.M44 + viewProjection.M43)),
            Plane.Normalize(new Plane(
                viewProjection.M14 - viewProjection.M13,
                viewProjection.M24 - viewProjection.M23,
                viewProjection.M34 - viewProjection.M33,
                viewProjection.M44 - viewProjection.M43))
        };

        return new ViewFrustum(planes);
    }

    public bool IsVisible(BoundingBox bounds)
    {
        foreach (var plane in _planes)
        {
            if (IsOutsidePlane(plane, bounds))
                return false;
        }

        return true;
    }

    private static bool IsOutsidePlane(Plane plane, BoundingBox bounds)
    {
        var positive = new Vector3(
            plane.Normal.X >= 0f ? bounds.Max.X : bounds.Min.X,
            plane.Normal.Y >= 0f ? bounds.Max.Y : bounds.Min.Y,
            plane.Normal.Z >= 0f ? bounds.Max.Z : bounds.Min.Z);

        return Vector3.Dot(plane.Normal, positive) + plane.D < 0f;
    }
}
