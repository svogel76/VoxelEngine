namespace VoxelEngine.World;

public static class SkyLightPropagator
{
    public const byte MaxSkyLight = 15;

    private readonly record struct LightNode(int X, int Y, int Z, byte Light);

    public static void RecalculateChunk(World world, Chunk chunk)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(chunk);

        chunk.ClearSkyLight();

        var queue = new Queue<LightNode>(Chunk.Width * Chunk.Depth * 4);

        SeedTopDown(chunk, queue);
        SeedFromNeighborBorders(world, chunk, queue);
        FloodFill(chunk, queue);
    }

    private static void SeedTopDown(Chunk chunk, Queue<LightNode> queue)
    {
        for (int x = 0; x < Chunk.Width; x++)
        for (int z = 0; z < Chunk.Depth; z++)
        {
            byte incoming = MaxSkyLight;
            for (int y = Chunk.Height - 1; y >= 0; y--)
            {
                byte nextLight = Attenuate(incoming, chunk.GetBlock(x, y, z));
                TrySetLight(chunk, x, y, z, nextLight, queue);
                incoming = nextLight;
            }
        }
    }

    private static void SeedFromNeighborBorders(World world, Chunk chunk, Queue<LightNode> queue)
    {
        int chunkX = chunk.ChunkPosition.X;
        int chunkZ = chunk.ChunkPosition.Z;

        Chunk? west = world.GetChunk(chunkX - 1, chunkZ);
        if (west is not null)
        {
            for (int y = 0; y < Chunk.Height; y++)
            for (int z = 0; z < Chunk.Depth; z++)
                TrySeedFromNeighbor(chunk, 0, y, z, west.GetSkyLight(Chunk.Width - 1, y, z), queue);
        }

        Chunk? east = world.GetChunk(chunkX + 1, chunkZ);
        if (east is not null)
        {
            for (int y = 0; y < Chunk.Height; y++)
            for (int z = 0; z < Chunk.Depth; z++)
                TrySeedFromNeighbor(chunk, Chunk.Width - 1, y, z, east.GetSkyLight(0, y, z), queue);
        }

        Chunk? north = world.GetChunk(chunkX, chunkZ - 1);
        if (north is not null)
        {
            for (int y = 0; y < Chunk.Height; y++)
            for (int x = 0; x < Chunk.Width; x++)
                TrySeedFromNeighbor(chunk, x, y, 0, north.GetSkyLight(x, y, Chunk.Depth - 1), queue);
        }

        Chunk? south = world.GetChunk(chunkX, chunkZ + 1);
        if (south is not null)
        {
            for (int y = 0; y < Chunk.Height; y++)
            for (int x = 0; x < Chunk.Width; x++)
                TrySeedFromNeighbor(chunk, x, y, Chunk.Depth - 1, south.GetSkyLight(x, y, 0), queue);
        }
    }

    private static void TrySeedFromNeighbor(Chunk chunk, int targetX, int targetY, int targetZ, byte neighborLight, Queue<LightNode> queue)
    {
        if (neighborLight == 0)
            return;

        byte nextLight = Attenuate(neighborLight, chunk.GetBlock(targetX, targetY, targetZ));
        TrySetLight(chunk, targetX, targetY, targetZ, nextLight, queue);
    }

    private static void FloodFill(Chunk chunk, Queue<LightNode> queue)
    {
        while (queue.Count > 0)
        {
            LightNode node = queue.Dequeue();
            byte nodeLight = chunk.GetSkyLight(node.X, node.Y, node.Z);
            if (nodeLight != node.Light)
                continue;

            if (nodeLight == 0)
                continue;

            SpreadToNeighbor(chunk, node.X - 1, node.Y, node.Z, nodeLight, horizontalStep: true, queue);
            SpreadToNeighbor(chunk, node.X + 1, node.Y, node.Z, nodeLight, horizontalStep: true, queue);
            SpreadToNeighbor(chunk, node.X, node.Y - 1, node.Z, nodeLight, horizontalStep: false, queue);
            SpreadToNeighbor(chunk, node.X, node.Y + 1, node.Z, nodeLight, horizontalStep: false, queue);
            SpreadToNeighbor(chunk, node.X, node.Y, node.Z - 1, nodeLight, horizontalStep: true, queue);
            SpreadToNeighbor(chunk, node.X, node.Y, node.Z + 1, nodeLight, horizontalStep: true, queue);
        }
    }

    private static void SpreadToNeighbor(Chunk chunk, int x, int y, int z, byte sourceLight, bool horizontalStep, Queue<LightNode> queue)
    {
        if (x < 0 || x >= Chunk.Width || y < 0 || y >= Chunk.Height || z < 0 || z >= Chunk.Depth)
            return;

        byte lightAfterDistance = horizontalStep ? SubtractStep(sourceLight, 1) : sourceLight;
        byte nextLight = Attenuate(lightAfterDistance, chunk.GetBlock(x, y, z));
        TrySetLight(chunk, x, y, z, nextLight, queue);
    }

    private static byte SubtractStep(byte sourceLight, int amount)
    {
        if (sourceLight == 0 || amount <= 0)
            return sourceLight;

        int result = sourceLight - amount;
        return (byte)Math.Max(0, result);
    }

    private static byte Attenuate(byte sourceLight, byte blockType)
    {
        if (sourceLight == 0)
            return 0;

        int attenuation = BlockRegistry.Get(blockType).SkyLightAttenuation;
        int result = sourceLight - attenuation;
        return (byte)Math.Max(0, result);
    }

    private static void TrySetLight(Chunk chunk, int x, int y, int z, byte light, Queue<LightNode> queue)
    {
        if (light <= chunk.GetSkyLight(x, y, z))
            return;

        chunk.SetSkyLight(x, y, z, light);
        queue.Enqueue(new LightNode(x, y, z, light));
    }
}
