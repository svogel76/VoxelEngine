namespace VoxelEngine.World;

public class WorldGenerator
{
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
    /// Generiert Terrain basierend auf Perlin Noise Höhenkarte.
    /// </summary>
    public void GenerateTerrain(World world, int fromChunkX, int toChunkX, int fromChunkZ, int toChunkZ)
    {
        for (int cx = fromChunkX; cx <= toChunkX; cx++)
        for (int cz = fromChunkZ; cz <= toChunkZ; cz++)
        {
            // Lade oder erstelle Chunk
            var chunk = world.GetOrCreateChunk(cx, cz);

            for (int x = 0; x < Chunk.Width; x++)
            for (int z = 0; z < Chunk.Depth; z++)
            {
                int worldX = cx * Chunk.Width + x;
                int worldZ = cz * Chunk.Depth + z;

                // Berechne Terrain-Höhe basierend auf Noise
                float noiseValue = _noise.GetNoise(worldX, worldZ);
                int height = (int)(_settings.BaseHeight + noiseValue * _settings.Amplitude);
                height = Math.Clamp(height, 1, Chunk.Height - 1);

                // Befülle Blöcke von Y=0 bis Y=height
                for (int y = 0; y <= height; y++)
                {
                    byte blockType;

                    if (y == 0)
                        blockType = BlockType.Stone;
                    else if (y < height - 2)
                        blockType = BlockType.Stone;
                    else if (y < height)
                        blockType = BlockType.Dirt;
                    else // y == height
                        blockType = BlockType.Grass;

                    world.SetBlock(worldX, y, worldZ, blockType);
                }
            }
        }
    }
}
