using System.Numerics;
using FluentAssertions;
using VoxelEngine.Core;
using VoxelEngine.Entity;
using VoxelEngine.Entity.Components;
using VoxelEngine.World;

namespace VoxelEngine.Tests.Entity;

public class TerrainPhysicsEntityTests
{
    private const float Width  = 0.6f;
    private const float Height = 1.0f;

    [Fact]
    public void Update_AppliesGravityToEntityInLoadedChunk()
    {
        // Arrange
        var settings = new EngineSettings();
        var world    = new global::VoxelEngine.World.World();
        world.AddChunk(new Chunk(0, 0));

        var entity = new global::VoxelEngine.Entity.Entity("test", new Vector3(2f, 10f, 2f));
        var phys   = new PhysicsComponent(world, Width, Height, settings.Gravity, settings.MaxFallSpeed);
        entity.AddComponent(phys);

        var manager = new EntityManager(world, settings);
        manager.Add(entity);

        // Act
        manager.Update(0.25);

        // Assert
        entity.InternalPosition.Y.Should().BeLessThan(10f);
        entity.InternalVelocity.Y.Should().BeLessThan(0f);
        phys.IsOnGround.Should().BeFalse();
    }

    [Fact]
    public void Update_LandsEntityOnGroundWithoutFallingThroughBlocks()
    {
        // Arrange
        var settings = new EngineSettings();
        var world    = new global::VoxelEngine.World.World();
        world.AddChunk(new Chunk(0, 0));
        BuildFloor(world, minX: 1, maxX: 2, minZ: 1, maxZ: 2, y: 0);

        var entity = new global::VoxelEngine.Entity.Entity("test", new Vector3(1.5f, 4f, 1.5f));
        var phys   = new PhysicsComponent(world, Width, Height, settings.Gravity, settings.MaxFallSpeed);
        entity.AddComponent(phys);

        var manager = new EntityManager(world, settings);
        manager.Add(entity);

        // Act
        for (int i = 0; i < 120; i++)
            manager.Update(1.0 / 60.0);

        // Assert
        entity.InternalPosition.Y.Should().BeApproximately(1f, 0.001f);
        entity.InternalVelocity.Y.Should().Be(0f);
        phys.IsOnGround.Should().BeTrue();
        phys.Bounds.Min.Y.Should().BeGreaterThanOrEqualTo(1f);
    }

    private static void BuildFloor(global::VoxelEngine.World.World world, int minX, int maxX, int minZ, int maxZ, int y)
    {
        for (int x = minX; x <= maxX; x++)
        for (int z = minZ; z <= maxZ; z++)
            world.SetBlock(x, y, z, BlockType.Stone);
    }
}
