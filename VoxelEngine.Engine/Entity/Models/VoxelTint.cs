namespace VoxelEngine.Entity.Models;

public readonly record struct VoxelTint(byte R, byte G, byte B, byte A)
{
    public static VoxelTint White => new(255, 255, 255, 255);
}
