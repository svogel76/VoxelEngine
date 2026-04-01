using FluentAssertions;
using Silk.NET.Maths;
using System.Numerics;
using VoxelEngine.Core;
using VoxelEngine.Core.Debug.Commands;
using VoxelEngine.Entity.Components;
using VoxelEngine.Entity.Models;
using VoxelEngine.Persistence;
using VoxelEngine.Rendering;
using VoxelEngine.World;

namespace VoxelEngine.Tests.Core.Debug.Commands;

public class DamageCommandTests
{
    [Fact]
    public void Execute_WithAmount_DamagesPlayerHealth()
    {
        // Arrange
        var context = CreateContext();
        var command = new DamageCommand();
        var health = context.Player.GetComponent<HealthComponent>()!;

        // Act
        command.Execute(["3.5"], context);

        // Assert
        health.CurrentHp.Should().Be(health.MaxHp - 3.5f);
    }

    [Fact]
    public void Execute_WithoutAmount_LogsUsage()
    {
        // Arrange
        var context = CreateContext();
        var command = new DamageCommand();

        // Act
        command.Execute([], context);

        // Assert
        context.Console.GetOutput().Should().Contain("Verwendung: damage <amount>");
    }

    private static GameContext CreateContext()
    {
        var settings = new EngineSettings();
        var world = new global::VoxelEngine.World.World();

        var player = new global::VoxelEngine.Entity.Entity("player", new Vector3(0f, 64f, 0f));
        var physics = new PhysicsComponent(
            world,
            settings.PlayerWidth,
            settings.PlayerHeight,
            settings.Gravity,
            settings.MaxFallSpeed,
            settings.FallDamageThreshold,
            settings.FallDamageMultiplier);
        physics.EyeOffset = settings.EyeHeight;
        player.AddComponent(physics);
        player.AddComponent(new HealthComponent(20f));

        var eyePos = physics.GetEyePosition(player.InternalPosition);
        var camera = new Camera(new Vector3D<float>(eyePos.X, eyePos.Y, eyePos.Z), 16f / 9f, settings);

        return new GameContext(
            settings,
            new KeyBindings(),
            world,
            player,
            camera,
            renderer: null!,
            inputHandler: null!,
            new WorldGenerator(settings),
            new TestModelLibrary(),
            new InMemoryPersistence());
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

