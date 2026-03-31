using System.Numerics;
using FluentAssertions;
using VoxelEngine.Core;
using VoxelEngine.Entity;
using VoxelEngine.Entity.AI;
using VoxelEngine.Entity.Models;
using VoxelEngine.Entity.Spawning;
using VoxelEngine.World;

namespace VoxelEngine.Tests.Entity;

public class SpawnManagerTests
{
    [Fact]
    public void Tick_SpawnsDiurnalEntitiesOnlyDuringDay()
    {
        string directory = CreateClimateDirectory(SpawnActivity.Diurnal, spawnInterval: 0f);

        try
        {
            // Arrange
            var setup = CreateSpawnSetup(directory, timeOfDay: 12.0);

            // Act
            setup.EntityManager.Update(0.1);

            // Assert
            setup.EntityManager.Count.Should().Be(1);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Tick_DoesNotSpawnDiurnalEntitiesAtNight()
    {
        string directory = CreateClimateDirectory(SpawnActivity.Diurnal, spawnInterval: 0f);

        try
        {
            // Arrange
            var setup = CreateSpawnSetup(directory, timeOfDay: 22.0);

            // Act
            setup.EntityManager.Update(0.1);

            // Assert
            setup.EntityManager.Count.Should().Be(0);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Tick_RespectsSpawnTickInterval()
    {
        string directory = CreateClimateDirectory(SpawnActivity.Any, spawnInterval: 0f);

        try
        {
            // Arrange
            var setup = CreateSpawnSetup(directory, timeOfDay: 12.0, spawnTickInterval: 5f);

            // Act
            setup.EntityManager.Update(4.9);

            // Assert
            setup.EntityManager.Count.Should().Be(0);

            // Act
            setup.EntityManager.Update(0.1);

            // Assert
            setup.EntityManager.Count.Should().Be(1);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Tick_RespectsPopulationCapAcrossPeriodicChecks()
    {
        string directory = CreateClimateDirectory(SpawnActivity.Any, spawnInterval: 0f, maxCount: 1);

        try
        {
            // Arrange
            var setup = CreateSpawnSetup(directory, timeOfDay: 12.0, spawnTickInterval: 0f);

            // Act
            setup.EntityManager.Update(0.1);
            setup.EntityManager.Update(0.1);
            setup.EntityManager.Update(0.1);

            // Assert
            setup.EntityManager.GetAll<global::VoxelEngine.Entity.Entity>().Should().HaveCount(1);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Tick_RespectsPerSpeciesSpawnInterval()
    {
        string directory = CreateClimateDirectory(SpawnActivity.Any, spawnInterval: 10f, maxCount: 2);

        try
        {
            // Arrange
            var setup = CreateSpawnSetup(directory, timeOfDay: 12.0, spawnTickInterval: 5f);

            // Act
            setup.EntityManager.Update(5.0);

            // Assert
            setup.EntityManager.Count.Should().Be(0);

            // Act
            setup.EntityManager.Update(5.0);

            // Assert
            setup.EntityManager.Count.Should().Be(1);

            // Act
            setup.EntityManager.Update(5.0);

            // Assert
            setup.EntityManager.Count.Should().Be(1);

            // Act
            setup.EntityManager.Update(5.0);

            // Assert
            setup.EntityManager.Count.Should().Be(2);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Tick_DoesNotSpawnInsideViewFrustum()
    {
        string directory = CreateClimateDirectory(SpawnActivity.Any, spawnInterval: 0f);

        try
        {
            // Arrange
            var setup = CreateSpawnSetup(
                directory,
                timeOfDay: 12.0,
                spawnTickInterval: 0f,
                frustumFactory: position => CreateOverheadFrustum(position));

            // Act
            setup.EntityManager.Update(0.1);

            // Assert
            setup.EntityManager.Count.Should().Be(0);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Tick_DespawnsManagedEntitiesBeyondSpawnDistancePlusBuffer()
    {
        string directory = CreateClimateDirectory(SpawnActivity.Any, spawnInterval: 0f);

        try
        {
            // Arrange
            var setup = CreateSpawnSetup(directory, timeOfDay: 12.0, spawnTickInterval: 0f);
            setup.EntityManager.Update(0.1);
            setup.EntityManager.Count.Should().Be(1);
            setup.PlayerPosition = new Vector3(512f, 90f, 512f);

            // Act
            setup.EntityManager.Update(0.1);

            // Assert
            setup.EntityManager.Count.Should().Be(0);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Tick_DoesNotDespawnManagedEntitiesInsideProtectionRadius()
    {
        string directory = CreateClimateDirectory(SpawnActivity.Any, spawnInterval: 0f);

        try
        {
            // Arrange
            var setup = CreateSpawnSetup(directory, timeOfDay: 12.0, spawnTickInterval: 0f);
            setup.EntityManager.Update(0.1);
            global::VoxelEngine.Entity.Entity entity = setup.EntityManager.GetAll<global::VoxelEngine.Entity.Entity>().Single();
            setup.PlayerPosition = entity.InternalPosition + new Vector3(1f, 0f, 1f);

            // Act
            setup.EntityManager.Update(0.1);

            // Assert
            setup.EntityManager.Count.Should().Be(1);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Tick_OnDayNightTransition_DespawnsBurrowingEntitiesOutsideProtectionRadius()
    {
        string directory = CreateClimateDirectory(SpawnActivity.Any, spawnInterval: 0f);

        try
        {
            // Arrange
            var setup = CreateSpawnSetup(
                directory,
                timeOfDay: 22.0,
                spawnTickInterval: 0f,
                dayActivity: EntityTimeOfDayActivity.Burrow,
                nightActivity: EntityTimeOfDayActivity.Active);
            setup.EntityManager.Update(0.1);
            setup.EntityManager.Count.Should().Be(1);
            setup.PlayerPosition = new Vector3(512f, 90f, 512f);
            setup.Time.SetTime(12.0);

            // Act
            setup.EntityManager.Update(0.1);

            // Assert
            setup.EntityManager.Count.Should().Be(0);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Tick_OnDayNightTransition_PutsAnimalsToSleepInsteadOfDespawning()
    {
        string directory = CreateClimateDirectory(SpawnActivity.Diurnal, spawnInterval: 0f);

        try
        {
            // Arrange
            var setup = CreateSpawnSetup(
                directory,
                timeOfDay: 12.0,
                spawnTickInterval: 0f,
                dayActivity: EntityTimeOfDayActivity.Active,
                nightActivity: EntityTimeOfDayActivity.Sleep);
            setup.EntityManager.Update(0.1);
            setup.Time.SetTime(22.0);

            // Act
            setup.EntityManager.Update(0.1);

            // Assert
            setup.EntityManager.Count.Should().Be(1);
            setup.EntityManager.GetAll<global::VoxelEngine.Entity.Entity>().Should().ContainSingle()
                .Which.GetComponent<VoxelEngine.Entity.Components.AIComponent>()!.State
                .Should().Be(AnimalMovementState.Sleep);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Tick_DelaysBurrowWhileEntityIsInsideProtectionRadius_AndDespawnsLater()
    {
        string directory = CreateClimateDirectory(SpawnActivity.Any, spawnInterval: 0f);

        try
        {
            // Arrange
            var setup = CreateSpawnSetup(
                directory,
                timeOfDay: 22.0,
                spawnTickInterval: 0f,
                dayActivity: EntityTimeOfDayActivity.Burrow,
                nightActivity: EntityTimeOfDayActivity.Active);
            setup.EntityManager.Update(0.1);
            setup.Time.SetTime(12.0);

            // Act
            setup.EntityManager.Update(0.1);

            // Assert
            setup.EntityManager.Count.Should().Be(1);

            // Act
            setup.PlayerPosition = new Vector3(512f, 90f, 512f);
            setup.EntityManager.Update(0.1);

            // Assert
            setup.EntityManager.Count.Should().Be(0);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static SpawnTestSetup CreateSpawnSetup(
        string climateDirectory,
        double timeOfDay,
        float spawnTickInterval = 0f,
        EntityTimeOfDayActivity dayActivity = EntityTimeOfDayActivity.Active,
        EntityTimeOfDayActivity nightActivity = EntityTimeOfDayActivity.Active,
        Func<Vector3, ViewFrustum>? frustumFactory = null)
    {
        var settings = new EngineSettings
        {
            Terrain = new NoiseSettings { Seed = 12345 },
            MaxSpawnDistance = 48f,
            SpawnTickInterval = spawnTickInterval,
            EntitySpawnPlacementAttempts = 128,
            EntityDespawnProtectionRadius = 128f
        };

        var generator = new WorldGenerator(settings, climateDirectory);
        (int worldX, int worldZ) = FindCoordinateForClimate(generator, "temperate");
        int chunkX = global::VoxelEngine.World.World.WorldToChunk(worldX);
        int chunkZ = global::VoxelEngine.World.World.WorldToChunk(worldZ);
        var world = new global::VoxelEngine.World.World();

        for (int x = chunkX - 4; x <= chunkX + 4; x++)
        for (int z = chunkZ - 4; z <= chunkZ + 4; z++)
            world.AddChunk(generator.GenerateChunk(x, z));

        int surfaceHeight = generator.GetSurfaceHeight(worldX, worldZ);
        var playerPosition = new Vector3(worldX + 0.5f, surfaceHeight + 1f, worldZ + 0.5f);
        var time = new WorldTime();
        time.SetTime(timeOfDay);
        var entityManager = new EntityManager(world, settings);
        var spawnManager = new SpawnManager(
            world,
            entityManager,
            settings,
            generator,
            time,
            new TestModelLibrary(CreateAnimalModel("deer", dayActivity, nightActivity)),
            () => playerPosition,
            frustumFactory is null ? (() => null!) : () => frustumFactory(playerPosition),
            new Random(42));
        entityManager.SpawnManager = spawnManager;

        return new SpawnTestSetup(entityManager, spawnManager, time, () => playerPosition, value => playerPosition = value);
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

    private static string CreateClimateDirectory(SpawnActivity activity, float spawnInterval, int maxCount = 1)
    {
        string directory = Path.Combine(Path.GetTempPath(), $"{nameof(SpawnManagerTests)}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);

        WriteClimateFile(directory, "temperate", true, activity, spawnInterval, maxCount);
        WriteClimateFile(directory, "taiga", false, SpawnActivity.Any, spawnInterval, maxCount);
        WriteClimateFile(directory, "steppe", false, SpawnActivity.Any, spawnInterval, maxCount);
        WriteClimateFile(directory, "savanna", false, SpawnActivity.Any, spawnInterval, maxCount);
        WriteClimateFile(directory, "desert", false, SpawnActivity.Any, spawnInterval, maxCount);
        WriteClimateFile(directory, "tropics", false, SpawnActivity.Any, spawnInterval, maxCount);
        return directory;
    }

    private static void WriteClimateFile(string directory, string id, bool includeSpawn, SpawnActivity activity, float spawnInterval, int maxCount)
    {
        string spawnJson = includeSpawn
            ? $$"""
              "spawns": [
                { "entity": "deer", "maxCount": {{maxCount}}, "minSpawnDistance": 8, "spawnInterval": {{spawnInterval}}, "activity": "{{activity.ToString().ToLowerInvariant()}}" }
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

    private static IVoxelModelDefinition CreateAnimalModel(
        string id,
        EntityTimeOfDayActivity dayActivity,
        EntityTimeOfDayActivity nightActivity)
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
                    WanderRadius = 0f,
                    DayActivity = dayActivity,
                    NightActivity = nightActivity
                }
            });

    private static ViewFrustum CreateOverheadFrustum(Vector3 center)
    {
        var eye = center + new Vector3(0f, 256f, 0f);
        var view = Matrix4x4.CreateLookAt(eye, center, -Vector3.UnitZ);
        var projection = Matrix4x4.CreateOrthographic(1024f, 1024f, 0.1f, 512f);
        return ViewFrustum.FromViewProjection(view * projection);
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

        public SpawnTestSetup(
            EntityManager entityManager,
            SpawnManager spawnManager,
            WorldTime time,
            Func<Vector3> getPlayerPosition,
            Action<Vector3> setPlayerPosition)
        {
            EntityManager = entityManager;
            SpawnManager = spawnManager;
            Time = time;
            _getPlayerPosition = getPlayerPosition;
            _setPlayerPosition = setPlayerPosition;
        }

        public EntityManager EntityManager { get; }
        public SpawnManager SpawnManager { get; }
        public WorldTime Time { get; }

        public Vector3 PlayerPosition
        {
            get => _getPlayerPosition();
            set => _setPlayerPosition(value);
        }
    }
}
