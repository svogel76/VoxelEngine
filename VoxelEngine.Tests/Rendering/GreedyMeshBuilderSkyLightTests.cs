using FluentAssertions;
using VoxelEngine.Core;
using VoxelEngine.Rendering;
using VoxelEngine.World;
using WorldModel = VoxelEngine.World.World;

namespace VoxelEngine.Tests.Rendering;

public sealed class GreedyMeshBuilderSkyLightTests
{
    [Fact]
    public void Build_TopFace_SamplesSkyLightFromCellAbove()
    {
        // Arrange
        const int x = 8;
        const int y = 80;
        const int z = 8;

        var world = CreateWorldWithAirNeighbors(out var centerChunk);
        var generator = new WorldGenerator(new EngineSettings());

        centerChunk.SetBlock(x, y, z, BlockType.Stone);
        centerChunk.SetSkyLight(x, y + 1, z, SkyLightPropagator.MaxSkyLight);

        // Act
        var (opaqueVerts, _, _, _, _, _) = GreedyMeshBuilder.Build(centerChunk, world, generator, minSkyLightAmbient: 0f);
        float[] faceLights = ExtractFaceLights(opaqueVerts);

        // Assert
        faceLights.Should().NotBeEmpty();
        faceLights.Max().Should().BeApproximately(1.0f, 0.0001f);
        faceLights.Should().Contain(value => MathF.Abs(value) < 0.0001f);
    }

    [Fact]
    public void Build_BottomFace_SamplesSkyLightFromCellBelow()
    {
        // Arrange
        const int x = 8;
        const int y = 80;
        const int z = 8;

        var world = CreateWorldWithAirNeighbors(out var centerChunk);
        var generator = new WorldGenerator(new EngineSettings());

        centerChunk.SetBlock(x, y, z, BlockType.Stone);
        centerChunk.SetSkyLight(x, y - 1, z, SkyLightPropagator.MaxSkyLight);

        // Act
        var (opaqueVerts, _, _, _, _, _) = GreedyMeshBuilder.Build(centerChunk, world, generator, minSkyLightAmbient: 0f);
        float[] faceLights = ExtractFaceLights(opaqueVerts);

        // Assert
        faceLights.Should().NotBeEmpty();
        faceLights.Max().Should().BeApproximately(0.4f, 0.0001f);
        faceLights.Should().Contain(value => MathF.Abs(value) < 0.0001f);
    }

    [Fact]
    public void Build_SideFaceAtChunkBoundary_SamplesNeighborChunkSkyLight()
    {
        // Arrange
        const int x = Chunk.Width - 1;
        const int y = 80;
        const int z = 8;

        var world = new WorldModel();
        var centerChunk = new Chunk(0, 0);
        var eastChunk = new Chunk(1, 0);
        var westChunk = new Chunk(-1, 0);
        var northChunk = new Chunk(0, -1);
        var southChunk = new Chunk(0, 1);

        world.AddChunk(centerChunk);
        world.AddChunk(eastChunk);
        world.AddChunk(westChunk);
        world.AddChunk(northChunk);
        world.AddChunk(southChunk);

        var generator = new WorldGenerator(new EngineSettings());

        centerChunk.SetBlock(x, y, z, BlockType.Stone);
        eastChunk.SetSkyLight(0, y, z, SkyLightPropagator.MaxSkyLight);

        // Act
        var (opaqueVerts, _, _, _, _, _) = GreedyMeshBuilder.Build(centerChunk, world, generator, minSkyLightAmbient: 0f);
        float[] faceLights = ExtractFaceLights(opaqueVerts);

        // Assert
        faceLights.Should().NotBeEmpty();
        faceLights.Max().Should().BeApproximately(0.7f, 0.0001f);
        faceLights.Should().Contain(value => MathF.Abs(value) < 0.0001f);
    }

    [Fact]
    public void Build_SideFaceAtChunkBoundary_MissingNeighborChunk_UsesUnloadedFallbackInsteadOfBlack()
    {
        // Arrange
        const int x = Chunk.Width - 1;
        const int y = 220;
        const int z = 8;

        var world = new WorldModel();
        var centerChunk = new Chunk(0, 0);
        world.AddChunk(centerChunk);
        world.AddChunk(new Chunk(-1, 0));
        world.AddChunk(new Chunk(0, -1));
        world.AddChunk(new Chunk(0, 1));

        var generator = new WorldGenerator(new EngineSettings());

        centerChunk.SetBlock(x, y, z, BlockType.Stone);

        // Act
        var (opaqueVerts, _, _, _, _, _) = GreedyMeshBuilder.Build(centerChunk, world, generator, minSkyLightAmbient: 0f);
        float[] faceLights = ExtractFaceLights(opaqueVerts);

        // Assert
        faceLights.Should().NotBeEmpty();
        faceLights.Max().Should().BeApproximately(0.7f, 0.0001f);
    }

    private static WorldModel CreateWorldWithAirNeighbors(out Chunk centerChunk)
    {
        var world = new WorldModel();
        centerChunk = new Chunk(0, 0);

        world.AddChunk(centerChunk);
        world.AddChunk(new Chunk(-1, 0));
        world.AddChunk(new Chunk(1, 0));
        world.AddChunk(new Chunk(0, -1));
        world.AddChunk(new Chunk(0, 1));

        return world;
    }

    private static float[] ExtractFaceLights(float[] vertices)
    {
        const int stride = 9;
        const int faceLightOffset = 7;

        var values = new List<float>(vertices.Length / stride);
        for (int i = faceLightOffset; i < vertices.Length; i += stride)
            values.Add(vertices[i]);

        return values.ToArray();
    }
}
