using System.Numerics;
using FluentAssertions;
using VoxelEngine.Api;
using VoxelEngine.Api.Entity;
using VoxelEngine.Core;
using VoxelEngine.Entity;
using VoxelEngine.World;

namespace VoxelEngine.Tests.Entity;

public class EntityManagerTests
{
    [Fact]
    public void AddAndRemove_ManageActiveEntities()
    {
        // Arrange
        var manager = CreateManager();

        var first  = manager.Create(() => new global::VoxelEngine.Entity.Entity("a", new Vector3(1f, 2f, 3f)));
        var second = manager.Create(() => new global::VoxelEngine.Entity.Entity("b", new Vector3(4f, 2f, 3f)));

        // Act
        bool removed = manager.Remove(first);

        // Assert
        removed.Should().BeTrue();
        manager.Count.Should().Be(1);
        manager.GetAll<global::VoxelEngine.Entity.Entity>().Should().ContainSingle().Which.Should().BeSameAs(second);
    }

    [Fact]
    public void GetNearby_ReturnsOnlyEntitiesInsideRadius()
    {
        // Arrange
        var manager  = CreateManager(cellSize: 4f);
        var near     = manager.Create(() => new global::VoxelEngine.Entity.Entity("near", new Vector3(2f, 1f, 2f)));
        var edge     = manager.Create(() => new global::VoxelEngine.Entity.Entity("edge", new Vector3(4.5f, 1f, 2f)));
        manager.Create(() => new global::VoxelEngine.Entity.Entity("far", new Vector3(20f, 1f, 2f)));

        // Act
        var nearby = manager.GetNearby(new Vector3(0f, 1f, 2f), radius: 5f);

        // Assert
        nearby.Should().Contain(near);
        nearby.Should().Contain(edge);
        nearby.Should().HaveCount(2);
    }

    [Fact]
    public void GetVisible_ReturnsOnlyEntitiesInsideFrustum()
    {
        // Arrange
        var manager = CreateManager();
        var visible = manager.Create(() => new global::VoxelEngine.Entity.Entity("visible", new Vector3(-0.5f, -0.5f, -6f)));
        manager.Create(() => new global::VoxelEngine.Entity.Entity("hidden", new Vector3(25f, -0.5f, -6f)));

        var view       = Matrix4x4.CreateLookAt(Vector3.Zero, -Vector3.UnitZ, Vector3.UnitY);
        var projection = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 2f, 1f, 0.1f, 100f);
        var frustum    = ViewFrustum.FromViewProjection(view * projection);

        // Act
        var visibleEntities = manager.GetVisible(frustum);

        // Assert
        visibleEntities.Should().ContainSingle().Which.Should().BeSameAs(visible);
    }

    [Fact]
    public void Update_TicksOnlyEntitiesInsideLoadedChunks()
    {
        // Arrange
        var world = new global::VoxelEngine.World.World();
        world.AddChunk(new Chunk(0, 0));

        var manager         = new EntityManager(world, new EngineSettings());
        var loadedCounter   = new UpdateCounterComponent();
        var unloadedCounter = new UpdateCounterComponent();

        var loaded   = new global::VoxelEngine.Entity.Entity("loaded",   new Vector3(2f, 10f, 2f));
        var unloaded = new global::VoxelEngine.Entity.Entity("unloaded", new Vector3(Chunk.Width * 2f + 2f, 10f, 2f));

        loaded.AddComponent(loadedCounter);
        unloaded.AddComponent(unloadedCounter);
        manager.Add(loaded);
        manager.Add(unloaded);

        // Act
        manager.Update(0.25);

        // Assert
        loadedCounter.Count.Should().Be(1);
        unloadedCounter.Count.Should().Be(0);
    }

    private static EntityManager CreateManager(float? cellSize = null)
    {
        var settings = cellSize is null
            ? new EngineSettings()
            : new EngineSettings { EntitySpatialHashCellSize = cellSize.Value };

        return new EntityManager(new global::VoxelEngine.World.World(), settings);
    }

    private sealed class UpdateCounterComponent : IComponent
    {
        public string ComponentId => "test-counter";
        public int    Count       { get; private set; }

        public void Update(IEntity entity, IModContext context, double deltaTime)
            => Count++;
    }
}
