using System.Collections.Concurrent;

namespace VoxelEngine.World;

public class WorldGenerator
{
    public const int SeaLevel = 64;

    private readonly ClimateSystem _climateSystem;
    private readonly int _worldSeed;
    private readonly int _treeInfluenceRadius;
    private readonly ConcurrentDictionary<(int X, int Z), IReadOnlyList<TreePlacement>> _treeCache = new();

    public WorldGenerator(VoxelEngine.Core.EngineSettings settings)
    {
        _worldSeed = settings.Terrain.Seed;
        _treeInfluenceRadius = settings.TreeInfluenceRadius;
        _climateSystem = new ClimateSystem(settings.Terrain);
    }

    /// <summary>
    /// Generiert flache Testwelt.
    /// Wird nicht mehr aufgerufen, bleibt aber erhalten fuer debugging.
    /// </summary>
    public void GenerateFlat(World world, int fromChunkX, int toChunkX, int fromChunkZ, int toChunkZ)
    {
        for (int cx = fromChunkX; cx <= toChunkX; cx++)
        for (int cz = fromChunkZ; cz <= toChunkZ; cz++)
        {
            int baseX = cx * Chunk.Width;
            int baseZ = cz * Chunk.Width;

            for (int x = baseX; x < baseX + Chunk.Width; x++)
            for (int z = baseZ; z < baseZ + Chunk.Width; z++)
            {
                world.SetBlock(x, 0, z, BlockType.Stone);
                world.SetBlock(x, 1, z, BlockType.Dirt);
                world.SetBlock(x, 2, z, BlockType.Dirt);
                world.SetBlock(x, 3, z, BlockType.Dirt);
                world.SetBlock(x, 4, z, BlockType.Grass);
            }
        }
    }

    /// <summary>
    /// Generiert genau einen Chunk an der angegebenen Position.
    /// </summary>
    public Chunk GenerateChunk(int chunkX, int chunkZ)
    {
        var chunk = new Chunk(chunkX, chunkZ);

        for (int x = 0; x < Chunk.Width; x++)
        for (int z = 0; z < Chunk.Depth; z++)
        {
            int worldX = chunkX * Chunk.Width + x;
            int worldZ = chunkZ * Chunk.Depth + z;
            ClimateSample sample = _climateSystem.Sample(worldX, worldZ);
            int height = sample.SurfaceHeight;

            for (int y = 0; y <= height; y++)
                chunk.SetBlock(x, y, z, SampleTerrainBlock(sample, y));
        }

        ApplyTrees(chunkX, chunkZ, chunk);

        for (int x = 0; x < Chunk.Width; x++)
        for (int z = 0; z < Chunk.Depth; z++)
        {
            int worldX = chunkX * Chunk.Width + x;
            int worldZ = chunkZ * Chunk.Depth + z;
            ClimateSample sample = _climateSystem.Sample(worldX, worldZ);

            for (int y = 1; y <= SeaLevel; y++)
            {
                if (chunk.GetBlock(x, y, z) == BlockType.Air)
                    chunk.SetBlock(x, y, z, sample.SeaBlock);
            }
        }

        return chunk;
    }

    public byte SampleBlock(int worldX, int worldY, int worldZ)
    {
        if (worldY < 0 || worldY >= Chunk.Height)
            return BlockType.Air;

        ClimateSample sample = _climateSystem.Sample(worldX, worldZ);
        int height = sample.SurfaceHeight;

        if (worldY <= height)
            return SampleTerrainBlock(sample, worldY);

        byte treeBlock = SampleTreeBlock(worldX, worldY, worldZ);
        if (treeBlock != BlockType.Air)
            return treeBlock;

        if (worldY <= SeaLevel)
            return sample.SeaBlock;

        return BlockType.Air;
    }

    public int GetSurfaceHeight(int worldX, int worldZ)
        => _climateSystem.Sample(worldX, worldZ).SurfaceHeight;

    public ClimateSample SampleClimate(int worldX, int worldZ)
        => _climateSystem.Sample(worldX, worldZ);

    /// <summary>
    /// Generiert Terrain basierend auf Perlin Noise Hoehenkarte.
    /// </summary>
    public void GenerateTerrain(World world, int fromChunkX, int toChunkX, int fromChunkZ, int toChunkZ)
    {
        for (int cx = fromChunkX; cx <= toChunkX; cx++)
        for (int cz = fromChunkZ; cz <= toChunkZ; cz++)
            world.AddChunk(GenerateChunk(cx, cz));
    }

    private void ApplyTrees(int chunkX, int chunkZ, Chunk chunk)
    {
        for (int originChunkX = chunkX - 1; originChunkX <= chunkX + 1; originChunkX++)
        for (int originChunkZ = chunkZ - 1; originChunkZ <= chunkZ + 1; originChunkZ++)
        {
            foreach (var placement in GetTreePlacementsForChunk(originChunkX, originChunkZ))
            {
                PlaceTree(placement.Template, placement.WorldX, placement.SurfaceY, placement.WorldZ, chunk);
            }
        }
    }

    private IReadOnlyList<TreePlacement> GetTreePlacementsForChunk(int chunkX, int chunkZ) =>
        _treeCache.GetOrAdd((chunkX, chunkZ), static (key, generator) => generator.BuildTreePlacementsForChunk(key.X, key.Z), this);

    private IReadOnlyList<TreePlacement> BuildTreePlacementsForChunk(int chunkX, int chunkZ)
    {
        var placements = new List<TreePlacement>();
        int baseX = chunkX * Chunk.Width;
        int baseZ = chunkZ * Chunk.Depth;

        for (int localX = 0; localX < Chunk.Width; localX++)
        for (int localZ = 0; localZ < Chunk.Depth; localZ++)
        {
            int worldX = baseX + localX;
            int worldZ = baseZ + localZ;

            if (TryGetTreePlacement(worldX, worldZ, out TreePlacement placement))
                placements.Add(placement);
        }

        return placements;
    }

    private bool TryGetTreePlacement(int worldX, int worldZ, out TreePlacement placement)
    {
        ClimateSample sample = _climateSystem.Sample(worldX, worldZ);
        ClimateZone zone = sample.PrimaryZone;
        TreeTemplate template = ResolveTreeTemplate(worldX, worldZ, zone);
        int surfaceY = sample.SurfaceHeight;
        placement = default;

        if (!ShouldPlaceTree(worldX, worldZ, zone))
            return false;
        if (surfaceY <= SeaLevel)
            return false;
        if (!IsValidTreeSurface(sample.SurfaceBlock, template))
            return false;

        placement = new TreePlacement(worldX, worldZ, surfaceY, template);
        return true;
    }

    private bool ShouldPlaceTree(int worldX, int worldZ, ClimateZone zone)
    {
        int mixed = unchecked(worldX * 374761 + worldZ * 668265);
        int hash = HashCode.Combine(_worldSeed, mixed);
        var rng = new Random(hash);
        return rng.NextSingle() < zone.TreeDensity;
    }

    private TreeTemplate ResolveTreeTemplate(int worldX, int worldZ, ClimateZone zone)
    {
        if (zone.TreeTemplate.Name != "cactus")
            return zone.TreeTemplate;

        int mixed = unchecked(worldX * 92821 + worldZ * 68917);
        int hash = HashCode.Combine(_worldSeed, mixed, 5_431);
        var rng = new Random(hash);
        return TreeTemplate.Cactus(rng.Next(3, 6));
    }

    private static bool IsValidTreeSurface(byte surfaceBlock, TreeTemplate template)
    {
        if (template.Name == "cactus")
            return surfaceBlock == BlockType.Sand;

        return surfaceBlock == BlockType.Grass || surfaceBlock == BlockType.DryGrass;
    }

    private static byte SampleTerrainBlock(ClimateSample sample, int worldY)
    {
        if (worldY == 0)
            return sample.StoneBlock;
        if (worldY < sample.SurfaceHeight - 3)
            return sample.StoneBlock;
        if (worldY < sample.SurfaceHeight)
            return sample.SubsurfaceBlock;
        return sample.SurfaceBlock;
    }

    private static void PlaceTree(TreeTemplate template, int worldX, int surfaceY, int worldZ, Chunk chunk)
    {
        int chunkBaseX = chunk.ChunkPosition.X * Chunk.Width;
        int chunkBaseZ = chunk.ChunkPosition.Z * Chunk.Depth;
        int trunkBaseY = surfaceY + 1;

        foreach (var cell in template.FilledBlocks)
        {
            int treeWorldX = worldX + cell.X - template.Pivot.X;
            int treeWorldY = trunkBaseY + cell.Y - template.Pivot.Y;
            int treeWorldZ = worldZ + cell.Z - template.Pivot.Z;

            int localX = treeWorldX - chunkBaseX;
            int localZ = treeWorldZ - chunkBaseZ;

            if (localX < 0 || localX >= Chunk.Width || localZ < 0 || localZ >= Chunk.Depth)
                continue;
            if (treeWorldY < 0 || treeWorldY >= Chunk.Height)
                continue;

            byte existing = chunk.GetBlock(localX, treeWorldY, localZ);

            if (cell.Block == BlockType.Leaves)
            {
                if (existing != BlockType.Air)
                    continue;
            }
            else
            {
                if (BlockRegistry.IsSolid(existing))
                    continue;
            }

            chunk.SetBlock(localX, treeWorldY, localZ, cell.Block);
        }
    }

    private byte SampleTreeBlock(int worldX, int worldY, int worldZ)
    {
        byte result = BlockType.Air;
        int minChunkX = World.WorldToChunk(worldX - _treeInfluenceRadius);
        int maxChunkX = World.WorldToChunk(worldX + _treeInfluenceRadius);
        int minChunkZ = World.WorldToChunk(worldZ - _treeInfluenceRadius);
        int maxChunkZ = World.WorldToChunk(worldZ + _treeInfluenceRadius);

        for (int chunkX = minChunkX; chunkX <= maxChunkX; chunkX++)
        for (int chunkZ = minChunkZ; chunkZ <= maxChunkZ; chunkZ++)
        {
            foreach (var placement in GetTreePlacementsForChunk(chunkX, chunkZ))
            {
                byte candidate = SampleTreeBlockFromOrigin(placement.Template, placement.WorldX, placement.SurfaceY, placement.WorldZ, worldX, worldY, worldZ);
                if (candidate == BlockType.Air)
                    continue;

                if (candidate == BlockType.Wood)
                    return candidate;

                if (result == BlockType.Air)
                    result = candidate;
            }
        }

        return result;
    }

    private static byte SampleTreeBlockFromOrigin(
        TreeTemplate template,
        int originX,
        int surfaceY,
        int originZ,
        int worldX,
        int worldY,
        int worldZ)
    {
        int localX = worldX - originX + template.Pivot.X;
        int localY = worldY - (surfaceY + 1) + template.Pivot.Y;
        int localZ = worldZ - originZ + template.Pivot.Z;
        return template.GetBlock(localX, localY, localZ);
    }

    private readonly record struct TreePlacement(int WorldX, int WorldZ, int SurfaceY, TreeTemplate Template);
}
