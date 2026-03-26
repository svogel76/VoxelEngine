namespace VoxelEngine.World;

public class WorldGenerator
{
    public const int SeaLevel = 64;

    private readonly ClimateSystem _climateSystem;

    public WorldGenerator(VoxelEngine.Core.EngineSettings settings)
    {
        _climateSystem = new ClimateSystem(settings.Terrain);
    }

    /// <summary>
    /// Generiert flache Testwelt.
    /// Wird nicht mehr aufgerufen, bleibt aber erhalten für debugging.
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
            {
                byte blockType;

                if (y == 0)
                    blockType = sample.StoneBlock;
                else if (y < height - 3)
                    blockType = sample.StoneBlock;
                else if (y < height)
                    blockType = sample.SubsurfaceBlock;
                else
                    blockType = sample.SurfaceBlock;

                chunk.SetBlock(x, y, z, blockType);
            }
        }

        // Täler unter Meeresspiegel mit Wasser füllen
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

        if (worldY == 0)
            return sample.StoneBlock;

        if (worldY <= height)
        {
            if (worldY < height - 3) return sample.StoneBlock;
            if (worldY < height)     return sample.SubsurfaceBlock;
            return sample.SurfaceBlock;
        }

        if (worldY <= SeaLevel)
            return sample.SeaBlock;

        return BlockType.Air;
    }

    public int GetSurfaceHeight(int worldX, int worldZ)
    {
        return _climateSystem.Sample(worldX, worldZ).SurfaceHeight;
    }

    public ClimateSample SampleClimate(int worldX, int worldZ)
        => _climateSystem.Sample(worldX, worldZ);

    /// <summary>
    /// Generiert Terrain basierend auf Perlin Noise Höhenkarte.
    /// </summary>
    public void GenerateTerrain(World world, int fromChunkX, int toChunkX, int fromChunkZ, int toChunkZ)
    {
        for (int cx = fromChunkX; cx <= toChunkX; cx++)
        for (int cz = fromChunkZ; cz <= toChunkZ; cz++)
            world.AddChunk(GenerateChunk(cx, cz));
    }
}
