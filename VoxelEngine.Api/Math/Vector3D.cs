namespace VoxelEngine.Api.Math;

public struct Vector3D<T>
    where T : struct
{
    public Vector3D(T x, T y, T z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public T X { get; set; }
    public T Y { get; set; }
    public T Z { get; set; }
}
