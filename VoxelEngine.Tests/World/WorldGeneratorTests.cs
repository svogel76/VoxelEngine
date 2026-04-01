using FluentAssertions;
using VoxelEngine.Core;
using VoxelEngine.World;

namespace VoxelEngine.Tests.World;

public class WorldGeneratorTests
{
    private const int WorldSeed = 12_345;
    private const int ChunkX = 8;
    private const int ChunkZ = 6;
    private const ulong ExpectedChunkFingerprint = 5187773064207754119ul;

    [Fact]
    public void GenerateChunk_SameSeedAcrossIndependentGenerators_ProducesIdenticalBlocks()
    {
        // Arrange
        var firstGenerator = CreateGenerator(WorldSeed);
        var secondGenerator = CreateGenerator(WorldSeed);

        // Act
        Chunk firstChunk = firstGenerator.GenerateChunk(ChunkX, ChunkZ);
        Chunk secondChunk = secondGenerator.GenerateChunk(ChunkX, ChunkZ);

        // Assert
        CalculateChunkFingerprint(firstChunk).Should().Be(CalculateChunkFingerprint(secondChunk));
    }

    [Fact]
    public void GenerateChunk_WithKnownSeed_ProducesStableFingerprint()
    {
        // Arrange
        var generator = CreateGenerator(WorldSeed);

        // Act
        Chunk chunk = generator.GenerateChunk(ChunkX, ChunkZ);

        // Assert
        CalculateChunkFingerprint(chunk).Should().Be(ExpectedChunkFingerprint);
    }

    private static WorldGenerator CreateGenerator(int worldSeed)
    {
        var settings = new EngineSettings
        {
            Terrain = new NoiseSettings
            {
                Seed = worldSeed
            }
        };

        return new WorldGenerator(settings);
    }

    private static ulong CalculateChunkFingerprint(Chunk chunk)
    {
        const ulong offset = 14695981039346656037ul;
        const ulong prime = 1099511628211ul;

        ulong hash = offset;
        for (int x = 0; x < Chunk.Width; x++)
        for (int y = 0; y < Chunk.Height; y++)
        for (int z = 0; z < Chunk.Depth; z++)
        {
            hash ^= chunk.GetBlock(x, y, z);
            hash *= prime;
        }

        return hash;
    }
}
