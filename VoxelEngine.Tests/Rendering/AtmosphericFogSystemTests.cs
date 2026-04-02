using System.Numerics;
using FluentAssertions;
using VoxelEngine.Core;
using VoxelEngine.Rendering;
using VoxelEngine.World;

namespace VoxelEngine.Tests.Rendering;

public sealed class AtmosphericFogSystemTests
{
    [Fact]
    public void Build_TemperateZone_ShouldProduceDenserFogThanDesert()
    {
        // Arrange
        var settings = new EngineSettings();
        ClimateSample temperate = CreateClimateSample("temperate", "temperate", transitionFactor: 0f, surfaceHeight: 70);
        ClimateSample desert = CreateClimateSample("desert", "desert", transitionFactor: 0f, surfaceHeight: 70);

        // Act
        FogProfile temperateFog = AtmosphericFogSystem.Build(
            settings.FogStartFactor,
            settings.FogEndFactor,
            settings.RenderDistance,
            AtmosphericFogSystem.NoonClearHour,
            cameraHeight: 90f,
            temperate,
            baseSkyFogColor: new Vector3(0.72f, 0.8f, 0.9f));

        FogProfile desertFog = AtmosphericFogSystem.Build(
            settings.FogStartFactor,
            settings.FogEndFactor,
            settings.RenderDistance,
            AtmosphericFogSystem.NoonClearHour,
            cameraHeight: 90f,
            desert,
            baseSkyFogColor: new Vector3(0.72f, 0.8f, 0.9f));

        // Assert
        temperateFog.StartDistance.Should().BeLessThan(desertFog.StartDistance);
        temperateFog.EndDistance.Should().BeLessThan(desertFog.EndDistance);
    }

    [Fact]
    public void Build_Morning_ShouldProduceDenserFogThanNoon()
    {
        // Arrange
        var settings = new EngineSettings();
        ClimateSample climate = CreateClimateSample("temperate", "temperate", transitionFactor: 0f, surfaceHeight: 72);

        // Act
        FogProfile morningFog = AtmosphericFogSystem.Build(
            settings.FogStartFactor,
            settings.FogEndFactor,
            settings.RenderDistance,
            AtmosphericFogSystem.MorningPeakHour,
            cameraHeight: 88f,
            climate,
            baseSkyFogColor: new Vector3(0.68f, 0.77f, 0.88f));

        FogProfile noonFog = AtmosphericFogSystem.Build(
            settings.FogStartFactor,
            settings.FogEndFactor,
            settings.RenderDistance,
            AtmosphericFogSystem.NoonClearHour,
            cameraHeight: 88f,
            climate,
            baseSkyFogColor: new Vector3(0.68f, 0.77f, 0.88f));

        // Assert
        morningFog.StartDistance.Should().BeLessThan(noonFog.StartDistance);
        morningFog.EndDistance.Should().BeLessThan(noonFog.EndDistance);
    }

    [Fact]
    public void Build_ValleyCameraHeight_ShouldProduceDenserFogThanMountainHeight()
    {
        // Arrange
        var settings = new EngineSettings();
        ClimateSample climate = CreateClimateSample("temperate", "temperate", transitionFactor: 0f, surfaceHeight: 68);

        // Act
        FogProfile valleyFog = AtmosphericFogSystem.Build(
            settings.FogStartFactor,
            settings.FogEndFactor,
            settings.RenderDistance,
            AtmosphericFogSystem.NoonClearHour,
            cameraHeight: 70f,
            climate,
            baseSkyFogColor: new Vector3(0.7f, 0.79f, 0.89f));

        FogProfile mountainFog = AtmosphericFogSystem.Build(
            settings.FogStartFactor,
            settings.FogEndFactor,
            settings.RenderDistance,
            AtmosphericFogSystem.NoonClearHour,
            cameraHeight: 138f,
            climate,
            baseSkyFogColor: new Vector3(0.7f, 0.79f, 0.89f));

        // Assert
        valleyFog.StartDistance.Should().BeLessThan(mountainFog.StartDistance);
        valleyFog.EndDistance.Should().BeLessThan(mountainFog.EndDistance);
    }

    [Fact]
    public void Build_FogColor_ShouldShiftFromBluishMorningToWarmerEvening()
    {
        // Arrange
        var settings = new EngineSettings();
        ClimateSample climate = CreateClimateSample("temperate", "temperate", transitionFactor: 0f, surfaceHeight: 70);
        var baseSkyFog = new Vector3(0.74f, 0.82f, 0.9f);

        // Act
        FogProfile morningFog = AtmosphericFogSystem.Build(
            settings.FogStartFactor,
            settings.FogEndFactor,
            settings.RenderDistance,
            AtmosphericFogSystem.MorningPeakHour,
            cameraHeight: 88f,
            climate,
            baseSkyFog);

        FogProfile noonFog = AtmosphericFogSystem.Build(
            settings.FogStartFactor,
            settings.FogEndFactor,
            settings.RenderDistance,
            AtmosphericFogSystem.NoonClearHour,
            cameraHeight: 88f,
            climate,
            baseSkyFog);

        FogProfile eveningFog = AtmosphericFogSystem.Build(
            settings.FogStartFactor,
            settings.FogEndFactor,
            settings.RenderDistance,
            AtmosphericFogSystem.EveningPeakHour,
            cameraHeight: 88f,
            climate,
            baseSkyFog);

        // Assert
        morningFog.Color.Z.Should().BeGreaterThan(morningFog.Color.X);
        noonFog.Color.X.Should().BeGreaterThan(0.8f);
        noonFog.Color.Y.Should().BeGreaterThan(0.8f);
        noonFog.Color.Z.Should().BeGreaterThan(0.8f);
        eveningFog.Color.X.Should().BeGreaterThan(eveningFog.Color.Z);
    }

    private static ClimateSample CreateClimateSample(string primaryZoneId, string secondaryZoneId, float transitionFactor, int surfaceHeight)
    {
        ClimateZone primaryZone = CreateZone(primaryZoneId);
        ClimateZone secondaryZone = CreateZone(secondaryZoneId);

        return new ClimateSample(
            Temperature: 0.5f,
            Humidity: 0.5f,
            PrimaryZone: primaryZone,
            SecondaryZone: secondaryZone,
            TransitionFactor: transitionFactor,
            SurfaceHeight: surfaceHeight,
            SurfaceBlock: BlockType.Grass,
            SubsurfaceBlock: BlockType.Dirt,
            StoneBlock: BlockType.Stone,
            SeaBlock: BlockType.Water);
    }

    private static ClimateZone CreateZone(string id)
    {
        (float fogDensity, float fogTintStrength) = id.ToLowerInvariant() switch
        {
            "temperate" => (1.4f, 0.4f),
            "taiga" => (1.2f, 0.3f),
            "steppe" => (0.95f, 0.3f),
            "savanna" => (0.72f, 0.22f),
            "desert" => (0.6f, 0.22f),
            "tropics" => (0.98f, 0.3f),
            _ => (0.9f, 0.3f)
        };

        return new ClimateZone(
            id,
            id,
            new NoiseSettings(),
            BlockType.Grass,
            BlockType.Dirt,
            BlockType.Stone,
            BlockType.Water,
            int.MaxValue,
            0f,
            TreeTemplate.Oak(),
            fogDensity,
            fogTintStrength,
            []);
    }
}
