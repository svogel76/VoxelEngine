using FluentAssertions;
using VoxelEngine.Core;

namespace VoxelEngine.Tests.Core;

public sealed class EngineSettingsTests : IDisposable
{
    private readonly string _tempRoot = Path.Combine(Path.GetTempPath(), $"VoxelEngine.EngineSettings.{Guid.NewGuid():N}");

    [Fact]
    public void LoadFrom_ValidJson_LoadsConfiguredValues()
    {
        // Arrange
        WriteEngineJson("""
        {
          "engine": {
            "updates_per_second": 72,
            "max_gl_uploads_per_frame": 4
          },
          "world": {
            "render_distance": 8,
            "unload_distance": 10,
            "sea_level": 70
          },
          "physics": {
            "gravity": -24.0,
            "max_fall_speed": -50.0,
            "jump_velocity": 8.5,
            "step_height": 1.25,
            "enable_step_up": false
          },
          "fog": {
            "start_percent": 0.4,
            "end_percent": 0.85
          },
          "debug": {
            "show_fps": false
          }
        }
        """);

        // Act
        var settings = EngineSettings.LoadFrom(_tempRoot);

        // Assert
        settings.TargetUPS.Should().Be(72);
        settings.MaxGlUploadsPerFrame.Should().Be(4);
        settings.RenderDistance.Should().Be(8);
        settings.UnloadDistance.Should().Be(10);
        settings.SeaLevel.Should().Be(70);
        settings.Gravity.Should().Be(24f);
        settings.MaxFallSpeed.Should().Be(50f);
        settings.JumpVelocity.Should().Be(8.5f);
        settings.StepHeight.Should().Be(1.25f);
        settings.EnableStepUp.Should().BeFalse();
        settings.FogStartFactor.Should().Be(0.4f);
        settings.FogEndFactor.Should().Be(0.85f);
        settings.ShowFps.Should().BeFalse();
    }

    [Fact]
    public void LoadFrom_MissingKey_FallsBackToDefaultValue()
    {
        // Arrange
        var defaults = new EngineSettings();
        WriteEngineJson("""
        {
          "world": {
            "render_distance": 9
          }
        }
        """);

        // Act
        var settings = EngineSettings.LoadFrom(_tempRoot);

        // Assert
        settings.RenderDistance.Should().Be(9);
        settings.UnloadDistance.Should().Be(defaults.UnloadDistance);
        settings.SeaLevel.Should().Be(defaults.SeaLevel);
        settings.Gravity.Should().Be(defaults.Gravity);
        settings.ShowFps.Should().Be(defaults.ShowFps);
    }

    [Fact]
    public void LoadFrom_FileNotFound_ReturnsDefaults()
    {
        // Arrange
        var defaults = new EngineSettings();

        // Act
        var settings = EngineSettings.LoadFrom(_tempRoot);

        // Assert
        settings.Should().BeEquivalentTo(defaults);
    }

    [Fact]
    public void LoadFrom_InvalidJson_ThrowsWithFilename()
    {
        // Arrange
        WriteEngineJson("{ invalid json }");

        // Act
        Action act = () => EngineSettings.LoadFrom(_tempRoot);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*engine.json*");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
            Directory.Delete(_tempRoot, recursive: true);
    }

    private void WriteEngineJson(string content)
    {
        Directory.CreateDirectory(_tempRoot);
        File.WriteAllText(Path.Combine(_tempRoot, "engine.json"), content);
    }
}
