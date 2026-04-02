using WorldModel = VoxelEngine.World.World;
using FluentAssertions;
using VoxelEngine.World;

namespace VoxelEngine.Tests.World;

public sealed class SkyLightPropagatorTests
{
    private const int TestY = 90;
    private const int TestZ = 8;

    [Fact]
    public void RecalculateChunk_OpaqueBlock_BlocksSkyLightBelow()
    {
        // Arrange
        var world = new WorldModel();
        var chunk = new Chunk(0, 0);
        world.AddChunk(chunk);

        FillChunk(chunk, BlockType.Stone);

        int topY = Chunk.Height - 1;
        int solidY = topY - 1;
        for (int y = 0; y < Chunk.Height; y++)
            chunk.SetBlock(4, y, 4, BlockType.Air);
        chunk.SetBlock(4, solidY, 4, BlockType.Stone);

        // Act
        SkyLightPropagator.RecalculateChunk(world, chunk);

        // Assert
        byte topLight = chunk.GetSkyLight(4, topY, 4);
        byte blockedLight = chunk.GetSkyLight(4, solidY, 4);
        byte belowBlockedLight = chunk.GetSkyLight(4, solidY - 1, 4);

        topLight.Should().Be(Attenuate(SkyLightPropagator.MaxSkyLight, chunk.GetBlock(4, topY, 4)));
        blockedLight.Should().Be(0);
        belowBlockedLight.Should().Be(0);
    }

    [Fact]
    public void RecalculateChunk_TransparentBlock_AttenuatesInsteadOfBlocking()
    {
        // Arrange
        var world = new WorldModel();
        var chunk = new Chunk(0, 0);
        world.AddChunk(chunk);

        FillChunk(chunk, BlockType.Stone);

        int topY = Chunk.Height - 1;
        int waterY = topY - 1;
        int belowY = waterY - 1;
        for (int y = 0; y < Chunk.Height; y++)
            chunk.SetBlock(5, y, 5, BlockType.Air);
        chunk.SetBlock(5, waterY, 5, BlockType.Water);

        // Act
        SkyLightPropagator.RecalculateChunk(world, chunk);

        // Assert
        byte topLight = chunk.GetSkyLight(5, topY, 5);
        byte waterLight = chunk.GetSkyLight(5, waterY, 5);
        byte belowLight = chunk.GetSkyLight(5, belowY, 5);

        waterLight.Should().Be(Attenuate(topLight, BlockType.Water));
        belowLight.Should().Be(Attenuate(waterLight, BlockType.Air));
        waterLight.Should().BeGreaterThan(0);
    }

    [Fact]
    public void RecalculateChunk_HorizontalFloodFill_LightsCaveEntryFromSideOpening()
    {
        // Arrange
        var world = new WorldModel();
        var chunk = new Chunk(0, 0);
        world.AddChunk(chunk);

        for (int x = 1; x < Chunk.Width; x++)
            chunk.SetBlock(x, TestY + 1, TestZ, BlockType.Stone);

        // Act
        SkyLightPropagator.RecalculateChunk(world, chunk);

        // Assert
        byte openingLight = chunk.GetSkyLight(0, TestY, TestZ);
        byte caveInteriorLight = chunk.GetSkyLight(1, TestY, TestZ);

        openingLight.Should().BeGreaterThan(0);
        caveInteriorLight.Should().BeGreaterThan(0);
    }

    [Fact]
    public void RecalculateChunk_HorizontalFloodFill_LosesOneLightPerStepInAir()
    {
        // Arrange
        var world = new WorldModel();
        var chunk = new Chunk(0, 0);
        world.AddChunk(chunk);

        FillChunk(chunk, BlockType.Stone);

        const int y = 80;
        const int z = 8;
        for (int x = 0; x < 4; x++)
            chunk.SetBlock(x, y, z, BlockType.Air);

        // open entrance column to sky
        for (int yy = y; yy < Chunk.Height; yy++)
            chunk.SetBlock(0, yy, z, BlockType.Air);

        // keep corridor enclosed from side and above, so only horizontal spread lights x>0
        for (int x = 1; x < 4; x++)
        {
            chunk.SetBlock(x, y + 1, z, BlockType.Stone);
            chunk.SetBlock(x, y, z - 1, BlockType.Stone);
            chunk.SetBlock(x, y, z + 1, BlockType.Stone);
        }

        // Act
        SkyLightPropagator.RecalculateChunk(world, chunk);

        // Assert
        byte atEntrance = chunk.GetSkyLight(0, y, z);
        byte oneStep = chunk.GetSkyLight(1, y, z);
        byte twoSteps = chunk.GetSkyLight(2, y, z);

        oneStep.Should().Be((byte)Math.Max(0, atEntrance - 1));
        twoSteps.Should().Be((byte)Math.Max(0, oneStep - 1));
    }

    [Fact]
    public void RecalculateChunk_NeighborBorderSeeds_UsesNeighborEdgeLight()
    {
        // Arrange
        var world = new WorldModel();
        var targetChunk = new Chunk(0, 0);
        var eastNeighbor = new Chunk(1, 0);
        world.AddChunk(targetChunk);
        world.AddChunk(eastNeighbor);

        targetChunk.SetBlock(Chunk.Width - 1, TestY + 1, TestZ, BlockType.Stone);

        SkyLightPropagator.RecalculateChunk(world, eastNeighbor);

        // Act
        SkyLightPropagator.RecalculateChunk(world, targetChunk);

        // Assert
        byte neighborEdge = eastNeighbor.GetSkyLight(0, TestY, TestZ);
        byte targetEdge = targetChunk.GetSkyLight(Chunk.Width - 1, TestY, TestZ);

        neighborEdge.Should().BeGreaterThan(0);
        targetEdge.Should().BeGreaterThan(0);
    }

    private static void FillChunk(Chunk chunk, byte blockType)
    {
        for (int x = 0; x < Chunk.Width; x++)
        for (int y = 0; y < Chunk.Height; y++)
        for (int z = 0; z < Chunk.Depth; z++)
            chunk.SetBlock(x, y, z, blockType);
    }

    private static byte Attenuate(byte sourceLight, byte blockType)
    {
        int attenuation = BlockRegistry.Get(blockType).SkyLightAttenuation;
        return (byte)Math.Max(0, sourceLight - attenuation);
    }
}
