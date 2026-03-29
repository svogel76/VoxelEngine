using FluentAssertions;
using VoxelEngine.World;
using Xunit;

namespace VoxelEngine.Tests.World;

public class ChunkDirtyFlagTests
{
    [Fact]
    public void NewChunk_IsDirty_IsFalse()
    {
        // Arrange + Act
        var chunk = new Chunk(0, 0);

        // Assert
        chunk.IsDirty.Should().BeFalse();
    }

    [Fact]
    public void RecordEdit_SetsDirtyFlag()
    {
        // Arrange
        var chunk = new Chunk(0, 0);

        // Act
        chunk.RecordEdit(0, 0, 0, BlockType.Grass);

        // Assert
        chunk.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void RecordEdit_StoresCorrectBlockType()
    {
        // Arrange
        var chunk = new Chunk(0, 0);
        const int x = 5, y = 64, z = 7;

        // Act
        chunk.RecordEdit(x, y, z, BlockType.Stone);

        // Assert
        chunk.PlayerEdits.Should().ContainKey(((byte)x, (byte)y, (byte)z));
        chunk.PlayerEdits[((byte)x, (byte)y, (byte)z)].Should().Be(BlockType.Stone);
    }

    [Fact]
    public void ApplyPlayerEdits_OverridesBlocks()
    {
        // Arrange
        var chunk = new Chunk(0, 0);
        const int x = 3, y = 70, z = 3;
        chunk.RecordEdit(x, y, z, BlockType.Dirt);

        // Act
        chunk.ApplyPlayerEdits();

        // Assert
        chunk.GetBlock(x, y, z).Should().Be(BlockType.Dirt);
    }

    [Fact]
    public void LoadEdits_TransfersEditsToNewChunk()
    {
        // Arrange
        var original = new Chunk(1, 1);
        original.RecordEdit(2, 80, 4, BlockType.Sand);
        original.RecordEdit(6, 90, 8, BlockType.Water);
        var edits = new Dictionary<(byte x, byte y, byte z), byte>(original.PlayerEdits);

        var fresh = new Chunk(1, 1);

        // Act
        fresh.LoadEdits(edits);

        // Assert
        fresh.PlayerEdits.Should().HaveCount(2);
        fresh.PlayerEdits[((byte)2, (byte)80, (byte)4)].Should().Be(BlockType.Sand);
        fresh.PlayerEdits[((byte)6, (byte)90, (byte)8)].Should().Be(BlockType.Water);
    }

    [Fact]
    public void LoadEdits_ThenApply_RestoresAllBlocks()
    {
        // Arrange
        var original = new Chunk(0, 0);
        original.RecordEdit(1, 60, 1, BlockType.Glass);
        original.RecordEdit(2, 61, 2, BlockType.Snow);
        var edits = new Dictionary<(byte x, byte y, byte z), byte>(original.PlayerEdits);

        var reloaded = new Chunk(0, 0);

        // Act
        reloaded.LoadEdits(edits);
        reloaded.ApplyPlayerEdits();

        // Assert
        reloaded.GetBlock(1, 60, 1).Should().Be(BlockType.Glass);
        reloaded.GetBlock(2, 61, 2).Should().Be(BlockType.Snow);
    }

    [Fact]
    public void RecordEdit_MultipleEdits_AllPersist()
    {
        // Arrange
        var chunk = new Chunk(0, 0);
        var expectedEdits = new Dictionary<(byte x, byte y, byte z), byte>
        {
            { ((byte)0, (byte)64, (byte)0), BlockType.Grass },
            { ((byte)1, (byte)64, (byte)0), BlockType.Dirt },
            { ((byte)0, (byte)65, (byte)1), BlockType.Stone },
            { ((byte)Chunk.Width - 1, (byte)0, (byte)Chunk.Depth - 1), BlockType.Sand }
        };

        // Act
        foreach (var (pos, type) in expectedEdits)
            chunk.RecordEdit(pos.x, pos.y, pos.z, type);

        // Assert
        chunk.PlayerEdits.Should().HaveCount(expectedEdits.Count);
        foreach (var (pos, type) in expectedEdits)
            chunk.PlayerEdits[pos].Should().Be(type);
    }
}
