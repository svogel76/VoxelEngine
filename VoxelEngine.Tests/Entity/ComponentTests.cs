using System.Numerics;
using FluentAssertions;
using Silk.NET.Maths;
using VoxelEngine.Api;
using VoxelEngine.Api.Entity;
using VoxelEngine.Core;
using VoxelEngine.Entity.Components;
using VoxelEngine.Entity.Models;
using VoxelEngine.Persistence;
using VoxelEngine.Rendering;
using VoxelEngine.World;

namespace VoxelEngine.Tests.Entity;

public class ComponentTests
{
    private const float PhysicsWidth = 0.6f;
    private const float PhysicsHeight = 1.0f;
    private const float GroundY = 0f;
    private const float StartY = 6f;
    private const double TickDeltaTime = 1.0 / 60.0;
    private const int MaxSimulationTicks = 240;

    [Fact]
    public void HealthComponent_TakeDamage_ReducesCurrentHp()
    {
        // Arrange
        var entity = new global::VoxelEngine.Entity.Entity("test", Vector3.Zero);
        var health = new HealthComponent(20f);
        entity.AddComponent(health);

        // Act
        health.TakeDamage(5f);

        // Assert
        health.CurrentHp.Should().Be(15f);
        health.IsDead.Should().BeFalse();
    }

    [Fact]
    public void HealthComponent_TakeDamage_DiesAtZeroHp()
    {
        // Arrange
        var entity = new global::VoxelEngine.Entity.Entity("test", Vector3.Zero);
        var health = new HealthComponent(10f);
        entity.AddComponent(health);

        // Act
        health.TakeDamage(10f);

        // Assert
        health.CurrentHp.Should().Be(0f);
        health.IsDead.Should().BeTrue();
    }

    [Fact]
    public void PhysicsComponent_HardLanding_AppliesFallDamage()
    {
        // Arrange
        var settings = new EngineSettings
        {
            FallDamageThreshold = 0f,
            FallDamageMultiplier = 1f
        };
        var world = CreateWorldWithFloor();
        var entity = new global::VoxelEngine.Entity.Entity("test", new Vector3(1.5f, StartY, 1.5f));
        var health = new HealthComponent(20f);
        entity.AddComponent(new PhysicsComponent(
            world,
            PhysicsWidth,
            PhysicsHeight,
            settings.Gravity,
            settings.MaxFallSpeed,
            settings.FallDamageThreshold,
            settings.FallDamageMultiplier));
        entity.AddComponent(health);

        var manager = new global::VoxelEngine.Entity.EntityManager(world, settings);
        manager.Add(entity);

        // Act
        SimulateUntilGrounded(manager, entity);

        // Assert
        health.CurrentHp.Should().BeLessThan(health.MaxHp);
    }

    [Fact]
    public void PhysicsComponent_SoftLanding_DoesNotApplyFallDamage()
    {
        // Arrange
        var settings = new EngineSettings
        {
            FallDamageThreshold = 100f,
            FallDamageMultiplier = 1f
        };
        var world = CreateWorldWithFloor();
        var entity = new global::VoxelEngine.Entity.Entity("test", new Vector3(1.5f, StartY, 1.5f));
        var health = new HealthComponent(20f);
        entity.AddComponent(new PhysicsComponent(
            world,
            PhysicsWidth,
            PhysicsHeight,
            settings.Gravity,
            settings.MaxFallSpeed,
            settings.FallDamageThreshold,
            settings.FallDamageMultiplier));
        entity.AddComponent(health);

        var manager = new global::VoxelEngine.Entity.EntityManager(world, settings);
        manager.Add(entity);

        // Act
        SimulateUntilGrounded(manager, entity);

        // Assert
        health.CurrentHp.Should().Be(health.MaxHp);
    }

    [Fact]
    public void PhysicsComponent_HardLanding_WithoutHealthComponent_DoesNotCrash()
    {
        // Arrange
        var settings = new EngineSettings
        {
            FallDamageThreshold = 0f,
            FallDamageMultiplier = 1f
        };
        var world = CreateWorldWithFloor();
        var entity = new global::VoxelEngine.Entity.Entity("test", new Vector3(1.5f, StartY, 1.5f));
        entity.AddComponent(new PhysicsComponent(
            world,
            PhysicsWidth,
            PhysicsHeight,
            settings.Gravity,
            settings.MaxFallSpeed,
            settings.FallDamageThreshold,
            settings.FallDamageMultiplier));

        var manager = new global::VoxelEngine.Entity.EntityManager(world, settings);
        manager.Add(entity);

        // Act
        Action act = () => SimulateUntilGrounded(manager, entity);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void PlayerDeath_RespawnsAtSpawnAndRestoresFullHealth()
    {
        // Arrange
        var settings = new EngineSettings();
        var world = new global::VoxelEngine.World.World();
        var spawnPoint = new Vector3(8f, 10f, -4f);
        var player = new global::VoxelEngine.Entity.Entity("player", Vector3.Zero);
        var physics = new PhysicsComponent(world, settings.PlayerWidth, settings.PlayerHeight, settings.Gravity, settings.MaxFallSpeed, settings.FallDamageThreshold, settings.FallDamageMultiplier);
        physics.EyeOffset = settings.EyeHeight;
        var health = new HealthComponent(20f);
        player.AddComponent(physics);
        player.AddComponent(health);

        var camera = new Camera(new Vector3D<float>(0f, 0f, 0f), 16f / 9f, settings);
        var context = new GameContext(
            settings,
            new KeyBindings(),
            world,
            player,
            camera,
            renderer: null!,
            inputHandler: null!,
            new WorldGenerator(settings),
            CreateModels(),
            new InMemoryPersistence(),
            spawnPoint);

        health.TakeDamage(health.MaxHp);

        // Act
        bool respawned = context.RespawnPlayerIfDead();

        // Assert
        respawned.Should().BeTrue();
        health.CurrentHp.Should().Be(health.MaxHp);
        player.InternalPosition.Should().Be(spawnPoint);
        context.Console.GetOutput().Should().Contain("You died.");
    }

    [Fact]
    public void Entity_GetComponent_ReturnsNullForUnregisteredComponent()
    {
        // Arrange
        var entity = new global::VoxelEngine.Entity.Entity("test", Vector3.Zero);

        // Act
        var result = entity.GetComponent<HealthComponent>();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ComponentRegistry_Create_ThrowsForUnknownComponentName()
    {
        // Arrange
        var registry = new global::VoxelEngine.Entity.ComponentRegistry();

        // Act
        var act = () => registry.Create("nonexistent", default);

        // Assert
        act.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void ComponentRegistry_RegisterAndCreate_ResolvesCustomComponent()
    {
        // Arrange
        var registry = new global::VoxelEngine.Entity.ComponentRegistry();
        var created = new StubComponent();
        registry.Register("stub", _ => created);

        // Act
        var result = registry.Create("stub", default);

        // Assert
        result.Should().BeSameAs(created);
    }

    private static global::VoxelEngine.World.World CreateWorldWithFloor()
    {
        var world = new global::VoxelEngine.World.World();
        world.AddChunk(new Chunk(0, 0));

        for (int x = 1; x <= 2; x++)
        for (int z = 1; z <= 2; z++)
            world.SetBlock(x, (int)GroundY, z, BlockType.Stone);

        return world;
    }

    private static void SimulateUntilGrounded(global::VoxelEngine.Entity.EntityManager manager, global::VoxelEngine.Entity.Entity entity)
    {
        var physics = entity.GetComponent<PhysicsComponent>();
        physics.Should().NotBeNull();

        for (int i = 0; i < MaxSimulationTicks; i++)
        {
            manager.Update(TickDeltaTime);
            if (physics!.IsOnGround)
                return;
        }

        throw new Xunit.Sdk.XunitException("Entity did not land within the expected simulation window.");
    }

    private static IEntityModelLibrary CreateModels() => new TestModelLibrary();

    private sealed class StubComponent : IComponent
    {
        public string ComponentId => "stub";

        public void Update(IEntity entity, IModContext context, double deltaTime) { }
    }

    private sealed class TestModelLibrary : IEntityModelLibrary
    {
        private readonly IVoxelModelDefinition _model = new VoxelModelDefinition(
            "test", 0.25f, [new VoxelModelVoxel(0, 0, 0, 0, 0, VoxelTint.White)]);

        public EntityAtlasDefinition Atlas { get; } = new("Assets/Entities/entity_atlas.png", 4, 2);

        public IReadOnlyCollection<IVoxelModelDefinition> GetAllModels() => [_model];
        public IVoxelModelDefinition GetModel(string modelId) => _model;
    }
}





