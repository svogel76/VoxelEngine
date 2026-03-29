using System.Numerics;
using FluentAssertions;
using VoxelEngine.Core;
using VoxelEngine.World;

namespace VoxelEngine.Tests.World;

public class PlayerStepUpTests
{
    [Fact]
    public void ProcessInput_RepeatedStepUps_ClampsVisualDrop()
    {
        // Arrange
        var settings = new EngineSettings();
        var physics = new PlayerPhysicsSettings(
            settings.Gravity,
            settings.MaxFallSpeed,
            settings.JumpVelocity,
            settings.StepHeight,
            settings.StepUpMaxVisualDrop,
            settings.StepUpSmoothingSpeed,
            settings.EnableStepUp);

        var world = new global::VoxelEngine.World.World();
        BuildAscendingSlope(world, 8);

        var player = new Player(new Vector3(-0.5f, 1f, 0.5f));
        player.SyncPhysics(world);

        float minAllowedEyeY = float.NegativeInfinity;

        // Act
        for (int i = 0; i < 12; i++)
        {
            player.ProcessInput(
                new PlayerInput(1f, 0f, 0f, false),
                Vector3.UnitX,
                Vector3.UnitZ,
                Vector3.UnitY,
                moveSpeed: 6f,
                physics,
                world,
                deltaTime: 0.1);

            float eyeDrop = player.EyePosition.Y - (player.Position.Y + Player.EyeHeight);
            minAllowedEyeY = float.IsNegativeInfinity(minAllowedEyeY)
                ? eyeDrop
                : MathF.Min(minAllowedEyeY, eyeDrop);
        }

        // Assert
        minAllowedEyeY.Should().BeGreaterThanOrEqualTo(-settings.StepUpMaxVisualDrop - 0.001f);
        player.Position.Y.Should().BeGreaterThan(2f);
        player.IsOnGround.Should().BeTrue();
    }

    private static void BuildAscendingSlope(global::VoxelEngine.World.World world, int stepCount)
    {
        for (int x = -2; x < 0; x++)
        {
            world.SetBlock(x, 0, 0, BlockType.Stone);
        }

        for (int x = 0; x < stepCount; x++)
        {
            int columnHeight = x + 2;
            for (int y = 0; y < columnHeight; y++)
                world.SetBlock(x, y, 0, BlockType.Stone);
        }
    }
}
