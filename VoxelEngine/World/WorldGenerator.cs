namespace VoxelEngine.World;

public static class WorldGenerator
{
    public static void GenerateFlat(World world, int fromChunkX, int toChunkX, int fromChunkZ, int toChunkZ)
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
}
