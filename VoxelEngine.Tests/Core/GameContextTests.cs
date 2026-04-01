using FluentAssertions;
using Silk.NET.Maths;
using System.Numerics;
using VoxelEngine.Core;
using VoxelEngine.Entity.Components;
using VoxelEngine.Entity.Models;
using VoxelEngine.Persistence;
using VoxelEngine.Rendering;
using VoxelEngine.World;

namespace VoxelEngine.Tests.Core;

public class GameContextTests
{
    [Fact]
    public async Task SaveAndLoadGameState_RoundTripsLookDirection()
    {
        // Arrange
        var persistence = new InMemoryPersistence();
        var settings    = new EngineSettings();
        var world       = new global::VoxelEngine.World.World();
        var generator   = new WorldGenerator(settings);

        var sourcePlayer = CreatePlayer(new Vector3(10f, 65f, -5f), settings, world);
        var sourceEye    = GetEyePosition(sourcePlayer);
        var sourceCamera = new Camera(new Vector3D<float>(sourceEye.X, sourceEye.Y, sourceEye.Z), 16f / 9f, settings);
        sourceCamera.SetRotation(48f, -18f);

        var sourceContext = new GameContext(
            settings, new KeyBindings(), world,
            sourcePlayer, sourceCamera,
            renderer: null!, inputHandler: null!,
            generator, entityModels: CreateModels(), persistence);

        var destPlayer = CreatePlayer(Vector3.Zero, settings, world);
        var destCamera = new Camera(new Vector3D<float>(0f, 0f, 0f), 16f / 9f, settings);
        destCamera.SetRotation(-10f, 12f);

        var destContext = new GameContext(
            settings, new KeyBindings(), world,
            destPlayer, destCamera,
            renderer: null!, inputHandler: null!,
            generator, entityModels: CreateModels(), persistence);

        // Act
        await sourceContext.SaveGameStateAsync();
        var (playerState, worldMeta) = await destContext.LoadGameStateAsync();
        destContext.ApplyLoadedState(playerState!, worldMeta!);

        // Assert
        destContext.Camera.Yaw.Should().BeApproximately(48f, 0.001f);
        destContext.Camera.Pitch.Should().BeApproximately(-18f, 0.001f);

        var destEye = GetEyePosition(destContext.Player);
        destContext.Camera.Position.X.Should().BeApproximately(destEye.X, 0.001f);
        destContext.Camera.Position.Y.Should().BeApproximately(destEye.Y, 0.001f);
        destContext.Camera.Position.Z.Should().BeApproximately(destEye.Z, 0.001f);
    }

    private static global::VoxelEngine.Entity.Entity CreatePlayer(
        Vector3 position, EngineSettings settings, global::VoxelEngine.World.World world)
    {
        var entity = new global::VoxelEngine.Entity.Entity("player", position);
        var phys   = new PhysicsComponent(world, 0.6f, 1.8f, settings.Gravity, settings.MaxFallSpeed, settings.FallDamageThreshold, settings.FallDamageMultiplier);
        phys.EyeOffset = 1.62f;
        entity.AddComponent(phys);
        entity.AddComponent(new HealthComponent(20f));
        return entity;
    }

    private static Vector3 GetEyePosition(global::VoxelEngine.Entity.Entity entity)
    {
        var phys = entity.GetComponent<PhysicsComponent>();
        return phys?.GetEyePosition(entity.InternalPosition) ?? entity.InternalPosition;
    }

    private static IEntityModelLibrary CreateModels() => new TestModelLibrary();

    private sealed class TestModelLibrary : IEntityModelLibrary
    {
        private readonly IVoxelModelDefinition _model = new VoxelModelDefinition(
            "test", 0.25f, [new VoxelModelVoxel(0, 0, 0, 0, 0, VoxelTint.White)]);

        public EntityAtlasDefinition Atlas { get; } = new("Assets/Entities/entity_atlas.png", 4, 2);

        public IReadOnlyCollection<IVoxelModelDefinition> GetAllModels() => [_model];
        public IVoxelModelDefinition GetModel(string modelId)            => _model;
    }
}


