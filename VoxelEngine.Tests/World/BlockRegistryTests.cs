using FluentAssertions;
using VoxelEngine.World;
using Xunit;

namespace VoxelEngine.Tests.World;

public class BlockRegistryTests
{
    [Fact]
    public void GetById_ReturnsCorrectDefinition_Air()
    {
        // Arrange
        // Act
        var definition = BlockRegistry.Get(BlockType.Air);

        // Assert
        definition.Id.Should().Be(BlockType.Air);
        definition.Name.Should().Be("air");
    }

    [Fact]
    public void GetById_ReturnsCorrectDefinition_Grass()
    {
        // Arrange
        // Act
        var definition = BlockRegistry.Get(BlockType.Grass);

        // Assert
        definition.Id.Should().Be(BlockType.Grass);
        definition.Name.Should().Be("grass");
    }

    [Fact]
    public void GetById_ReturnsCorrectDefinition_AllRegisteredTypes()
    {
        // Arrange
        byte[] knownIds =
        [
            BlockType.Air, BlockType.Grass, BlockType.Dirt, BlockType.Stone,
            BlockType.Sand, BlockType.Water, BlockType.Glass, BlockType.Ice,
            BlockType.DryGrass, BlockType.Snow, BlockType.Wood, BlockType.Leaves
        ];

        // Act + Assert
        foreach (var id in knownIds)
        {
            var definition = BlockRegistry.Get(id);
            definition.Id.Should().Be(id);
        }
    }

    [Fact]
    public void Solid_Air_IsNotSolid()
    {
        // Arrange
        // Act
        var isSolid = BlockRegistry.IsSolid(BlockType.Air);

        // Assert
        isSolid.Should().BeFalse();
    }

    [Fact]
    public void Solid_Grass_IsSolid()
    {
        // Arrange
        // Act
        var isSolid = BlockRegistry.IsSolid(BlockType.Grass);

        // Assert
        isSolid.Should().BeTrue();
    }

    [Fact]
    public void Transparent_Water_IsTransparent()
    {
        // Arrange
        // Act
        var isTransparent = BlockRegistry.IsTransparent(BlockType.Water);

        // Assert
        isTransparent.Should().BeTrue();
    }

    [Fact]
    public void Transparent_Grass_IsNotTransparent()
    {
        // Arrange
        // Act
        var isTransparent = BlockRegistry.IsTransparent(BlockType.Grass);

        // Assert
        isTransparent.Should().BeFalse();
    }

    [Fact]
    public void Replaceable_Air_IsReplaceable()
    {
        // Arrange
        // Act
        var isReplaceable = BlockRegistry.IsReplaceable(BlockType.Air);

        // Assert
        isReplaceable.Should().BeTrue();
    }

    [Fact]
    public void Replaceable_Grass_IsNotReplaceable()
    {
        // Arrange
        // Act
        var isReplaceable = BlockRegistry.IsReplaceable(BlockType.Grass);

        // Assert
        isReplaceable.Should().BeFalse();
    }

    [Fact]
    public void CollidesWithPlayer_Leaves_Collides()
    {
        // Arrange
        // Act
        var collides = BlockRegistry.CollidesWithPlayer(BlockType.Leaves);

        // Assert
        collides.Should().BeTrue();
    }

    [Fact]
    public void GetById_InvalidId_ThrowsKeyNotFoundException()
    {
        // Arrange
        const byte unregisteredId = 200;

        // Act
        Action act = () => BlockRegistry.Get(unregisteredId);

        // Assert
        act.Should().Throw<KeyNotFoundException>();
    }
}
