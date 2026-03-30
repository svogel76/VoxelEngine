using FluentAssertions;
using Silk.NET.Maths;
using VoxelEngine.Core;
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
        var settings = new EngineSettings();
        var world = new global::VoxelEngine.World.World();
        var generator = new WorldGenerator(settings);

        var sourcePlayer = new Player(new System.Numerics.Vector3(10f, 65f, -5f));
        var sourceCamera = new Camera(new Vector3D<float>(sourcePlayer.EyePosition.X, sourcePlayer.EyePosition.Y, sourcePlayer.EyePosition.Z), 16f / 9f, settings);
        sourceCamera.SetRotation(48f, -18f);

        var sourceContext = new GameContext(
            settings,
            world,
            sourcePlayer,
            sourceCamera,
            renderer: null!,
            inputHandler: null!,
            generator,
            persistence);

        var destinationPlayer = new Player(System.Numerics.Vector3.Zero);
        var destinationCamera = new Camera(new Vector3D<float>(0f, 0f, 0f), 16f / 9f, settings);
        destinationCamera.SetRotation(-10f, 12f);

        var destinationContext = new GameContext(
            settings,
            world,
            destinationPlayer,
            destinationCamera,
            renderer: null!,
            inputHandler: null!,
            generator,
            persistence);

        // Act
        await sourceContext.SaveGameStateAsync();
        var (playerState, worldMeta) = await destinationContext.LoadGameStateAsync();
        destinationContext.ApplyLoadedState(playerState!, worldMeta!);

        // Assert
        destinationContext.Camera.Yaw.Should().BeApproximately(48f, 0.001f);
        destinationContext.Camera.Pitch.Should().BeApproximately(-18f, 0.001f);
        destinationContext.Camera.Position.X.Should().BeApproximately(destinationContext.Player.EyePosition.X, 0.001f);
        destinationContext.Camera.Position.Y.Should().BeApproximately(destinationContext.Player.EyePosition.Y, 0.001f);
        destinationContext.Camera.Position.Z.Should().BeApproximately(destinationContext.Player.EyePosition.Z, 0.001f);
    }
}
