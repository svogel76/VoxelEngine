using System.Numerics;
using FluentAssertions;
using VoxelEngine.Core;
using VoxelEngine.Entity;
using VoxelEngine.World;

namespace VoxelEngine.Tests.Entity;

public class TerrainPhysicsEntityTests
{
    private static readonly BoundingBox TestBounds = new(
        new Vector3(-0.3f, 0f, -0.3f),
        new Vector3(0.3f, 1.0f, 0.3f));

    [Fact]
    public void Update_AppliesGravityToEntityInLoadedChunk()
    {
        // Arrange
        var world = new global::VoxelEngine.World.World();
        world.AddChunk(new Chunk(0, 0));

        var manager = new EntityManager(world, new EngineSettings());
        var entity = manager.Create(() => new PhysicsTestEntity(new Vector3(2f, 10f, 2f), TestBounds));

        // Act
        manager.Update(0.25);

        // Assert
        entity.Position.Y.Should().BeLessThan(10f);
        entity.Velocity.Y.Should().BeLessThan(0f);
        entity.IsOnGround.Should().BeFalse();
    }

    [Fact]
    public void Update_LandsEntityOnGroundWithoutFallingThroughBlocks()
    {
        // Arrange
        var world = new global::VoxelEngine.World.World();
        world.AddChunk(new Chunk(0, 0));
        BuildFloor(world, minX: 1, maxX: 2, minZ: 1, maxZ: 2, y: 0);

        var manager = new EntityManager(world, new EngineSettings());
        var entity = manager.Create(() => new PhysicsTestEntity(new Vector3(1.5f, 4f, 1.5f), TestBounds));

        // Act
        for (int i = 0; i < 120; i++)
            manager.Update(1.0 / 60.0);

        // Assert
        entity.Position.Y.Should().BeApproximately(1f, 0.001f);
        entity.Velocity.Y.Should().Be(0f);
        entity.IsOnGround.Should().BeTrue();
        entity.Bounds.Min.Y.Should().BeGreaterThanOrEqualTo(1f);
    }

    private static void BuildFloor(global::VoxelEngine.World.World world, int minX, int maxX, int minZ, int maxZ, int y)
    {
        for (int x = minX; x <= maxX; x++)
        for (int z = minZ; z <= maxZ; z++)
            world.SetBlock(x, y, z, BlockType.Stone);
    }

    private sealed class PhysicsTestEntity : TerrainPhysicsEntity
    {
        public PhysicsTestEntity(Vector3 position, BoundingBox localBounds)
            : base(position, localBounds)
        {
        }
    }
}
