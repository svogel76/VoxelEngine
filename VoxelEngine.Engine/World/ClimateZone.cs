namespace VoxelEngine.World;

public sealed class ClimateZone
{
    public string Id { get; }
    public string Name { get; }
    public NoiseSettings Terrain { get; }
    public byte SurfaceBlock { get; }
    public byte SubsurfaceBlock { get; }
    public byte StoneBlock { get; }
    public byte SeaBlock { get; }
    public int SnowLine { get; }
    public float TreeDensity { get; }
    public TreeTemplate TreeTemplate { get; }
    public IReadOnlyList<ClimateSpawnDefinition> Spawns { get; }

    public ClimateZone(
        string id,
        string name,
        NoiseSettings terrain,
        byte surfaceBlock,
        byte subsurfaceBlock,
        byte stoneBlock,
        byte seaBlock,
        int snowLine,
        float treeDensity,
        TreeTemplate treeTemplate,
        IReadOnlyList<ClimateSpawnDefinition>? spawns = null)
    {
        Id = id;
        Name = name;
        Terrain = terrain;
        SurfaceBlock = surfaceBlock;
        SubsurfaceBlock = subsurfaceBlock;
        StoneBlock = stoneBlock;
        SeaBlock = seaBlock;
        SnowLine = snowLine;
        TreeDensity = treeDensity;
        TreeTemplate = treeTemplate;
        Spawns = spawns ?? [];
    }
}
