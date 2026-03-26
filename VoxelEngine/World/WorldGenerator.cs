namespace VoxelEngine.World;

public class WorldGenerator
{
    public const int SeaLevel = 64;

    private readonly NoiseSettings _settings;
    private readonly FastNoiseLite _noise;

    public WorldGenerator(NoiseSettings settings)
    {
        _settings = settings;
        _noise = new FastNoiseLite(_settings.Seed);
        _noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        _noise.SetFrequency(_settings.Frequency);
        _noise.SetFractalType(FastNoiseLite.FractalType.FBm);
        _noise.SetFractalOctaves(_settings.Octaves);
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

            float noiseValue = _noise.GetNoise(worldX, worldZ);
            int height = (int)(_settings.BaseHeight + noiseValue * _settings.Amplitude);
            height = Math.Clamp(height, 1, Chunk.Height - 1);

            for (int y = 0; y <= height; y++)
            {
                byte blockType;

                if (y == 0)
                    blockType = BlockType.Stone;
                else if (y < height - 2)
                    blockType = BlockType.Stone;
                else if (y < height)
                    blockType = BlockType.Dirt;
                else
                    blockType = BlockType.Grass;

                chunk.SetBlock(x, y, z, blockType);
            }
        }

        // Täler unter Meeresspiegel mit Wasser füllen
        for (int x = 0; x < Chunk.Width; x++)
        for (int z = 0; z < Chunk.Depth; z++)
        {
            int worldX = chunkX * Chunk.Width + x;
            int worldZ = chunkZ * Chunk.Depth + z;
            for (int y = 1; y <= SeaLevel; y++)
            {
                if (chunk.GetBlock(x, y, z) == BlockType.Air)
                    chunk.SetBlock(x, y, z, BlockType.Water);
            }
        }

        return chunk;
    }

    public byte SampleBlock(int worldX, int worldY, int worldZ)
    {
        if (worldY < 0 || worldY >= Chunk.Height)
            return BlockType.Air;

        float noiseValue = _noise.GetNoise(worldX, worldZ);
        int height = (int)(_settings.BaseHeight + noiseValue * _settings.Amplitude);
        height = Math.Clamp(height, 1, Chunk.Height - 1);

        if (worldY == 0)
            return BlockType.Stone;

        if (worldY <= height)
        {
            if (worldY < height - 2) return BlockType.Stone;
            if (worldY < height)     return BlockType.Dirt;
            return BlockType.Grass;
        }

        if (worldY <= SeaLevel)
            return BlockType.Water;

        return BlockType.Air;
    }

    public int GetSurfaceHeight(int worldX, int worldZ)
    {
        float noiseValue = _noise.GetNoise(worldX, worldZ);
        int height = (int)(_settings.BaseHeight + noiseValue * _settings.Amplitude);
        return Math.Clamp(height, 1, Chunk.Height - 1);
    }

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
