namespace VoxelEngine.World;

public sealed class ClimateZone
{
    public string Name           { get; }
    public NoiseSettings Terrain { get; }
    public byte SurfaceBlock     { get; }
    public byte SubsurfaceBlock  { get; }
    public byte StoneBlock       { get; }
    public byte SeaBlock         { get; }
    public int SnowLine          { get; }

    public ClimateZone(
        string name,
        NoiseSettings terrain,
        byte surfaceBlock,
        byte subsurfaceBlock,
        byte stoneBlock,
        byte seaBlock,
        int snowLine)
    {
        Name = name;
        Terrain = terrain;
        SurfaceBlock = surfaceBlock;
        SubsurfaceBlock = subsurfaceBlock;
        StoneBlock = stoneBlock;
        SeaBlock = seaBlock;
        SnowLine = snowLine;
    }
}
