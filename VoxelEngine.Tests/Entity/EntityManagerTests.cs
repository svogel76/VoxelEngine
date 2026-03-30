using System.Numerics;
using FluentAssertions;
using VoxelEngine.Core;
using VoxelEngine.Entity;
using VoxelEngine.World;

namespace VoxelEngine.Tests.Entity;

public class EntityManagerTests
{
    [Fact]
    public void AddAndRemove_ManageActiveEntities_AndQueryByType()
    {
        // Arrange
        var manager = CreateManager();

        var passive = manager.Create(() => new TestEntity(new Vector3(1f, 2f, 3f)));
        var active = manager.Create(() => new DerivedTestEntity(new Vector3(4f, 2f, 3f)));

        // Act
        bool removed = manager.Remove(passive);

        // Assert
        removed.Should().BeTrue();
        manager.Count.Should().Be(1);
        manager.GetAll<DerivedTestEntity>().Should().ContainSingle().Which.Should().BeSameAs(active);
        manager.GetAll<TestEntity>().Should().ContainSingle().Which.Should().BeSameAs(active);
    }

    [Fact]
    public void GetNearby_ReturnsOnlyEntitiesInsideRadius()
    {
        // Arrange
        var manager = CreateManager(cellSize: 4f);
        var near = manager.Create(() => new TestEntity(new Vector3(2f, 1f, 2f)));
        var edge = manager.Create(() => new TestEntity(new Vector3(4.5f, 1f, 2f), new Vector3(1.5f, 1f, 1f)));
        manager.Create(() => new TestEntity(new Vector3(20f, 1f, 2f)));

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
        var visible = manager.Create(() => new TestEntity(
            new Vector3(-0.5f, -0.5f, -6f),
            new Vector3(1f, 1f, 1f)));
        manager.Create(() => new TestEntity(
            new Vector3(25f, -0.5f, -6f),
            new Vector3(1f, 1f, 1f)));

        var view = Matrix4x4.CreateLookAt(Vector3.Zero, -Vector3.UnitZ, Vector3.UnitY);
        var projection = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 2f, 1f, 0.1f, 100f);
        var frustum = ViewFrustum.FromViewProjection(view * projection);

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

        var manager = new EntityManager(world, new EngineSettings());
        var loadedEntity = manager.Create(() => new TestEntity(new Vector3(2f, 10f, 2f)));
        var unloadedEntity = manager.Create(() => new TestEntity(new Vector3(Chunk.Width * 2f + 2f, 10f, 2f)));

        // Act
        manager.Update(0.25);

        // Assert
        loadedEntity.UpdateCallCount.Should().Be(1);
        unloadedEntity.UpdateCallCount.Should().Be(0);
    }

    private static EntityManager CreateManager(float? cellSize = null)
    {
        var settings = cellSize is null
            ? new EngineSettings()
            : new EngineSettings { EntitySpatialHashCellSize = cellSize.Value };

        return new EntityManager(new global::VoxelEngine.World.World(), settings);
    }

    private class TestEntity : global::VoxelEngine.Entity.Entity, IEntityUpdatable, IEntityBoundsProvider
    {
        private readonly Vector3 _size;

        public int UpdateCallCount { get; private set; }

        public BoundingBox Bounds => new(Position, Position + _size);

        public TestEntity(Vector3 position, Vector3? size = null)
            : base(position)
        {
            _size = size ?? Vector3.Zero;
        }

        public void Update(double deltaTime)
        {
            UpdateCallCount++;
        }
    }

    private sealed class DerivedTestEntity : TestEntity
    {
        public DerivedTestEntity(Vector3 position)
            : base(position)
        {
        }
    }
}
