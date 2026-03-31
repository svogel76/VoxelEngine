using System.Numerics;
using FluentAssertions;
using VoxelEngine.Core;
using VoxelEngine.Entity.Components;
using VoxelEngine.World;

namespace VoxelEngine.Tests.World;

public class PlayerStepUpTests
{
    [Fact]
    public void ProcessInput_RepeatedStepUps_ClampsVisualDrop()
    {
        // Arrange
        var settings = new EngineSettings();
        var world    = new global::VoxelEngine.World.World();
        BuildAscendingSlope(world, 8);

        var entity = new global::VoxelEngine.Entity.Entity("player", new Vector3(-0.5f, 1f, 0.5f));
        var phys   = new PhysicsComponent(
            world,
            0.6f, 1.8f,
            settings.Gravity, settings.MaxFallSpeed,
            settings.FallDamageThreshold, settings.FallDamageMultiplier,
            settings.StepHeight, settings.EnableStepUp,
            settings.StepUpMaxVisualDrop, settings.StepUpSmoothingSpeed);
        phys.EyeOffset = 1.62f;
        entity.AddComponent(phys);
        phys.SyncPhysics(entity);

        float minAllowedEyeY = float.NegativeInfinity;

        // Act
        for (int i = 0; i < 12; i++)
        {
            phys.ProcessPlayerInput(
                entity,
                new PlayerInput(1f, 0f, 0f, false),
                Vector3.UnitX,
                Vector3.UnitZ,
                Vector3.UnitY,
                moveSpeed: 6f,
                deltaTime: 0.1);

            var   eyePos  = phys.GetEyePosition(entity.InternalPosition);
            float eyeDrop = eyePos.Y - (entity.InternalPosition.Y + 1.62f);
            minAllowedEyeY = float.IsNegativeInfinity(minAllowedEyeY)
                ? eyeDrop
                : MathF.Min(minAllowedEyeY, eyeDrop);
        }

        // Assert
        minAllowedEyeY.Should().BeGreaterThanOrEqualTo(-settings.StepUpMaxVisualDrop - 0.001f);
        entity.InternalPosition.Y.Should().BeGreaterThan(2f);
        phys.IsOnGround.Should().BeTrue();
    }

    private static void BuildAscendingSlope(global::VoxelEngine.World.World world, int stepCount)
    {
        for (int x = -2; x < 0; x++)
            world.SetBlock(x, 0, 0, BlockType.Stone);

        for (int x = 0; x < stepCount; x++)
        {
            int columnHeight = x + 2;
            for (int y = 0; y < columnHeight; y++)
                world.SetBlock(x, y, 0, BlockType.Stone);
        }
    }
}

