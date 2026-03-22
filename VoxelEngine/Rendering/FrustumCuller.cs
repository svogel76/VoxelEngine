using Silk.NET.Maths;
using VoxelEngine.World;
using SNPlane  = System.Numerics.Plane;
using SNVector = System.Numerics.Vector3;

namespace VoxelEngine.Rendering;

public class FrustumCuller
{
    private readonly SNPlane[] _planes = new SNPlane[6];

    public int LastVisibleCount { get; private set; }

    public void Update(Matrix4X4<float> viewProjection)
    {
        LastVisibleCount = 0;

        var vp = viewProjection;

        // Gribb-Hartmann Ebenen-Extraktion
        _planes[0] = SNPlane.Normalize(new SNPlane(
            vp.M14 + vp.M11, vp.M24 + vp.M21,
            vp.M34 + vp.M31, vp.M44 + vp.M41)); // Left

        _planes[1] = SNPlane.Normalize(new SNPlane(
            vp.M14 - vp.M11, vp.M24 - vp.M21,
            vp.M34 - vp.M31, vp.M44 - vp.M41)); // Right

        _planes[2] = SNPlane.Normalize(new SNPlane(
            vp.M14 + vp.M12, vp.M24 + vp.M22,
            vp.M34 + vp.M32, vp.M44 + vp.M42)); // Bottom

        _planes[3] = SNPlane.Normalize(new SNPlane(
            vp.M14 - vp.M12, vp.M24 - vp.M22,
            vp.M34 - vp.M32, vp.M44 - vp.M42)); // Top

        _planes[4] = SNPlane.Normalize(new SNPlane(
            vp.M14 + vp.M13, vp.M24 + vp.M23,
            vp.M34 + vp.M33, vp.M44 + vp.M43)); // Near

        _planes[5] = SNPlane.Normalize(new SNPlane(
            vp.M14 - vp.M13, vp.M24 - vp.M23,
            vp.M34 - vp.M33, vp.M44 - vp.M43)); // Far
    }

    public bool IsChunkVisible(int chunkX, int chunkZ)
    {
        var min = new SNVector(chunkX * Chunk.Width,  0,            chunkZ * Chunk.Depth);
        var max = new SNVector(min.X   + Chunk.Width, Chunk.Height, min.Z  + Chunk.Depth);

        foreach (var plane in _planes)
        {
            if (IsAABBOutsidePlane(plane, min, max))
                return false;
        }

        LastVisibleCount++;
        return true;
    }

    private static bool IsAABBOutsidePlane(SNPlane plane, SNVector min, SNVector max)
    {
        var positive = new SNVector(
            plane.Normal.X >= 0 ? max.X : min.X,
            plane.Normal.Y >= 0 ? max.Y : min.Y,
            plane.Normal.Z >= 0 ? max.Z : min.Z
        );
        return SNVector.Dot(plane.Normal, positive) + plane.D < 0;
    }
}
