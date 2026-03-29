using FluentAssertions;
using VoxelEngine.Persistence;
using VoxelEngine.World;

namespace VoxelEngine.Tests.Persistence;

public class WorldStatePersistenceTests
{
    [Fact]
    public async Task SaveLoadedChunkEditsAsync_PersistsDirtyLoadedChunks()
    {
        // Arrange
        var world = new global::VoxelEngine.World.World();
        var persistence = new InMemoryPersistence();

        var chunk = new Chunk(2, -1);
        chunk.SetBlock(3, 70, 4, BlockType.Glass);
        chunk.RecordEdit(3, 70, 4, BlockType.Glass);
        world.AddChunk(chunk);

        // Act
        await WorldStatePersistence.SaveLoadedChunkEditsAsync(world, persistence);
        var loadedEdits = await persistence.LoadChunkEditsAsync(2, -1);

        // Assert
        loadedEdits.Should().NotBeNull();
        loadedEdits.Should().BeEquivalentTo(new Dictionary<(byte x, byte y, byte z), byte>
        {
            { ((byte)3, (byte)70, (byte)4), BlockType.Glass }
        });
    }

    [Fact]
    public async Task SaveLoadedChunkEditsAsync_IgnoresCleanChunks()
    {
        // Arrange
        var world = new global::VoxelEngine.World.World();
        var persistence = new InMemoryPersistence();

        world.AddChunk(new Chunk(0, 0));

        // Act
        await WorldStatePersistence.SaveLoadedChunkEditsAsync(world, persistence);

        // Assert
        (await persistence.LoadChunkEditsAsync(0, 0)).Should().BeNull();
    }
}
