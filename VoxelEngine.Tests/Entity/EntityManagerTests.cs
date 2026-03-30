using System.Numerics;
using FluentAssertions;
using VoxelEngine.Core;
using VoxelEngine.Entity;
using VoxelEngine.Entity.Models;
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

    [Fact]
    public void Update_SpawnsDiurnalEntitiesOnlyDuringDay()
    {
        string directory = CreateClimateDirectory(SpawnActivity.Diurnal);

        try
        {
            // Arrange
            var setup = CreateSpawnManager(directory, timeOfDay: 12.0);

            // Act
            UpdateUntilSpawned(setup.Manager);

            // Assert
            setup.Manager.Count.Should().Be(1);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Update_DoesNotSpawnDiurnalEntitiesAtNight()
    {
        string directory = CreateClimateDirectory(SpawnActivity.Diurnal);

        try
        {
            // Arrange
            var setup = CreateSpawnManager(directory, timeOfDay: 22.0);

            // Act
            UpdateUntilSpawned(setup.Manager);

            // Assert
            setup.Manager.Count.Should().Be(0);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Update_OnDayNightTransition_DespawnsInactiveManagedEntitiesOutsideProtectionRadius()
    {
        string directory = CreateClimateDirectory(SpawnActivity.Diurnal);

        try
        {
            // Arrange
            var setup = CreateSpawnManager(directory, timeOfDay: 12.0);
            UpdateUntilSpawned(setup.Manager);
            setup.Manager.Count.Should().Be(1);
            setup.PlayerPosition = new Vector3(512f, 90f, 512f);
            setup.Time.SetTime(22.0);

            // Act
            setup.Manager.Update(0.1);

            // Assert
            setup.Manager.Count.Should().Be(0);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Update_OnDayNightTransition_KeepsInactiveManagedEntitiesInsideProtectionRadius()
    {
        string directory = CreateClimateDirectory(SpawnActivity.Diurnal);

        try
        {
            // Arrange
            var setup = CreateSpawnManager(directory, timeOfDay: 12.0);
            UpdateUntilSpawned(setup.Manager);
            setup.Manager.Count.Should().Be(1);
            setup.Time.SetTime(22.0);

            // Act
            setup.Manager.Update(0.1);

            // Assert
            setup.Manager.Count.Should().Be(1);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static EntityManager CreateManager(float? cellSize = null)
    {
        var settings = cellSize is null
            ? new EngineSettings()
            : new EngineSettings { EntitySpatialHashCellSize = cellSize.Value };

        return new EntityManager(new global::VoxelEngine.World.World(), settings);
    }

    private static SpawnTestSetup CreateSpawnManager(string climateDirectory, double timeOfDay)
    {
        var settings = new EngineSettings
        {
            Terrain = new NoiseSettings { Seed = 12345 },
            EntitySpawnRadius = 48f,
            EntitySpawnPlacementAttempts = 128,
            EntityDespawnProtectionRadius = 128f
        };

        var generator = new WorldGenerator(settings, climateDirectory);
        (int worldX, int worldZ) = FindCoordinateForClimate(generator, "temperate");
        int chunkX = global::VoxelEngine.World.World.WorldToChunk(worldX);
        int chunkZ = global::VoxelEngine.World.World.WorldToChunk(worldZ);
        var world = new global::VoxelEngine.World.World();

        for (int x = chunkX - 1; x <= chunkX + 1; x++)
        for (int z = chunkZ - 1; z <= chunkZ + 1; z++)
            world.AddChunk(generator.GenerateChunk(x, z));

        int surfaceHeight = generator.GetSurfaceHeight(worldX, worldZ);
        var playerPosition = new Vector3(worldX + 0.5f, surfaceHeight + 1f, worldZ + 0.5f);
        var time = new WorldTime();
        time.SetTime(timeOfDay);
        var manager = new EntityManager(
            world,
            settings,
            generator,
            time,
            new TestModelLibrary(CreateAnimalModel("deer")),
            () => playerPosition,
            () => null!,
            new Random(42));

        return new SpawnTestSetup(manager, time, () => playerPosition, value => playerPosition = value);
    }

    private static void UpdateUntilSpawned(EntityManager manager)
    {
        for (int i = 0; i < 6; i++)
            manager.Update(0.1);
    }

    private static (int WorldX, int WorldZ) FindCoordinateForClimate(WorldGenerator generator, string climateId)
    {
        for (int z = -512; z <= 512; z += 8)
        for (int x = -512; x <= 512; x += 8)
        {
            if (string.Equals(generator.SampleClimate(x, z).PrimaryZone.Id, climateId, StringComparison.OrdinalIgnoreCase))
                return (x, z);
        }

        throw new InvalidOperationException($"Could not find a sample coordinate for climate '{climateId}'.");
    }

    private static string CreateClimateDirectory(SpawnActivity activity)
    {
        string directory = Path.Combine(Path.GetTempPath(), $"{nameof(EntityManagerTests)}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);

        WriteClimateFile(directory, "temperate", true, activity);
        WriteClimateFile(directory, "taiga", false, SpawnActivity.Any);
        WriteClimateFile(directory, "steppe", false, SpawnActivity.Any);
        WriteClimateFile(directory, "savanna", false, SpawnActivity.Any);
        WriteClimateFile(directory, "desert", false, SpawnActivity.Any);
        WriteClimateFile(directory, "tropics", false, SpawnActivity.Any);
        return directory;
    }

    private static void WriteClimateFile(string directory, string id, bool includeSpawn, SpawnActivity activity)
    {
        string spawnJson = includeSpawn
            ? $$"""
              "spawns": [
                { "entity": "deer", "maxCount": 1, "minSpawnDistance": 8, "spawnInterval": 0, "activity": "{{activity.ToString().ToLowerInvariant()}}" }
              ]
              """
            : "\"spawns\": []";

        File.WriteAllText(
            Path.Combine(directory, $"{id}.json"),
            $$"""
            {
              "id": "{{id}}",
              "terrain": { "baseHeight": 72, "amplitude": 8, "frequency": 0.009, "octaves": 3 },
              "blocks": { "surface": "grass", "subsurface": "dirt", "stone": "stone", "sea": "water" },
              "snowLine": 999,
              "trees": { "density": 0.0, "template": "oak" },
              {{spawnJson}}
            }
            """);
    }

    private static IVoxelModelDefinition CreateAnimalModel(string id)
        => new VoxelModelDefinition(
            id,
            1f,
            [new VoxelModelVoxel(0, 0, 0, 0, 0, new VoxelTint(255, 255, 255, 255))],
            new EntityModelMetadata
            {
                Behaviour = new EntityBehaviourMetadata
                {
                    MoveSpeed = 0f,
                    FleeSpeed = 0f,
                    FleeRadius = 0f,
                    IdleTimeMin = 1f,
                    IdleTimeMax = 1f,
                    WanderRadius = 0f
                }
            });

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

    private sealed class TestModelLibrary : IEntityModelLibrary
    {
        private readonly Dictionary<string, IVoxelModelDefinition> _models;

        public TestModelLibrary(params IVoxelModelDefinition[] models)
        {
            _models = models.ToDictionary(model => model.Id, StringComparer.OrdinalIgnoreCase);
        }

        public EntityAtlasDefinition Atlas { get; } = new("Assets/Entities/entity_atlas.png", 4, 2);

        public IReadOnlyCollection<IVoxelModelDefinition> GetAllModels()
            => _models.Values;

        public IVoxelModelDefinition GetModel(string modelId)
            => _models[modelId];
    }

    private sealed class SpawnTestSetup
    {
        private readonly Func<Vector3> _getPlayerPosition;
        private readonly Action<Vector3> _setPlayerPosition;

        public SpawnTestSetup(EntityManager manager, WorldTime time, Func<Vector3> getPlayerPosition, Action<Vector3> setPlayerPosition)
        {
            Manager = manager;
            Time = time;
            _getPlayerPosition = getPlayerPosition;
            _setPlayerPosition = setPlayerPosition;
        }

        public EntityManager Manager { get; }
        public WorldTime Time { get; }

        public Vector3 PlayerPosition
        {
            get => _getPlayerPosition();
            set => _setPlayerPosition(value);
        }
    }
}
