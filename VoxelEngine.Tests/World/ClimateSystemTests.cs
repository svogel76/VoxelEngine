using FluentAssertions;
using VoxelEngine.World;

namespace VoxelEngine.Tests.World;

public sealed class ClimateSystemTests : IDisposable
{
    private readonly string _climateDirectory;

    public ClimateSystemTests()
    {
        _climateDirectory = Path.Combine(Path.GetTempPath(), $"{nameof(ClimateSystemTests)}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_climateDirectory);
        WriteClimateFiles(_climateDirectory);
    }

    [Fact]
    public void Constructor_LoadsZonesFromJsonAndBuildsExpectedClimateData()
    {
        // Arrange
        var settings = new NoiseSettings
        {
            Seed = 42,
            BaseHeight = 70f,
            Amplitude = 12f,
            Frequency = 0.02f,
            Octaves = 6
        };

        // Act
        var system = new ClimateSystem(settings, _climateDirectory);

        // Assert
        system.Zones.Should().HaveCount(6);

        ClimateZone temperate = system.Zones.Should().ContainSingle(zone => zone.Id == "temperate").Subject;
        temperate.Name.Should().Be("Temperate");
        temperate.Terrain.BaseHeight.Should().Be(72f);
        temperate.Terrain.Amplitude.Should().Be(28f);
        temperate.Terrain.Frequency.Should().Be(0.009f);
        temperate.Terrain.Octaves.Should().Be(5);
        temperate.SurfaceBlock.Should().Be(BlockType.Grass);
        temperate.SubsurfaceBlock.Should().Be(BlockType.Dirt);
        temperate.StoneBlock.Should().Be(BlockType.Stone);
        temperate.SeaBlock.Should().Be(BlockType.Water);
        temperate.SnowLine.Should().Be(999);
        temperate.TreeDensity.Should().Be(0.015f);
        temperate.TreeTemplate.Name.Should().Be("oak");
        temperate.FogDensity.Should().Be(1.4f);
        temperate.FogTintStrength.Should().Be(0.4f);
        temperate.Spawns.Should().ContainSingle();
        temperate.Spawns[0].Should().BeEquivalentTo(new ClimateSpawnDefinition("deer", 8, 16f, 30f, SpawnActivity.Diurnal));

        ClimateZone desert = system.Zones.Should().ContainSingle(zone => zone.Id == "desert").Subject;
        desert.SurfaceBlock.Should().Be(BlockType.Sand);
        desert.TreeTemplate.Name.Should().Be("cactus");
        desert.FogDensity.Should().Be(0.6f);
        desert.FogTintStrength.Should().Be(0.22f);
        desert.Spawns.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WhenJsonReferencesUnknownBlock_ThrowsFormatException()
    {
        // Arrange
        File.WriteAllText(
            Path.Combine(_climateDirectory, "temperate.json"),
            """
            {
              "id": "temperate",
              "terrain": { "baseHeight": 72, "amplitude": 28, "frequency": 0.009, "octaves": 5 },
              "blocks": { "surface": "unknown_block", "subsurface": "dirt", "stone": "stone", "sea": "water" },
              "snowLine": 999,
              "trees": { "density": 0.015, "template": "oak" },
              "fog": { "density": 1.4, "tintStrength": 0.4 },
              "spawns": []
            }
            """);

        // Act
        Action act = () => _ = new ClimateSystem(new NoiseSettings(), _climateDirectory);

        // Assert
        act.Should().Throw<FormatException>()
            .WithMessage("*temperate*unknown block*");
    }

    [Fact]
    public void Constructor_WhenRequiredClimateFileIsMissing_ThrowsKeyNotFoundException()
    {
        // Arrange
        File.Delete(Path.Combine(_climateDirectory, "taiga.json"));

        // Act
        Action act = () => _ = new ClimateSystem(new NoiseSettings(), _climateDirectory);

        // Assert
        act.Should().Throw<KeyNotFoundException>()
            .WithMessage("*taiga*");
    }

    [Fact]
    public void Constructor_WhenSpawnActivityIsMissing_DefaultsToAny()
    {
        // Arrange
        File.WriteAllText(
            Path.Combine(_climateDirectory, "temperate.json"),
            """
            {
              "id": "temperate",
              "terrain": { "baseHeight": 72, "amplitude": 28, "frequency": 0.009, "octaves": 5 },
              "blocks": { "surface": "grass", "subsurface": "dirt", "stone": "stone", "sea": "water" },
              "snowLine": 999,
              "trees": { "density": 0.015, "template": "oak" },
              "fog": { "density": 1.4, "tintStrength": 0.4 },
              "spawns": [
                { "entity": "deer", "maxCount": 8, "minSpawnDistance": 16, "spawnInterval": 30 }
              ]
            }
            """);

        // Act
        var system = new ClimateSystem(new NoiseSettings(), _climateDirectory);

        // Assert
        ClimateZone temperate = system.Zones.Should().ContainSingle(zone => zone.Id == "temperate").Subject;
        temperate.Spawns.Should().ContainSingle();
        temperate.Spawns[0].Activity.Should().Be(SpawnActivity.Any);
    }

    public void Dispose()
    {
        if (Directory.Exists(_climateDirectory))
            Directory.Delete(_climateDirectory, recursive: true);
    }

    private static void WriteClimateFiles(string directory)
    {
        File.WriteAllText(
            Path.Combine(directory, "temperate.json"),
            """
            {
              "id": "temperate",
              "terrain": { "baseHeight": 72, "amplitude": 28, "frequency": 0.009, "octaves": 5 },
              "blocks": { "surface": "grass", "subsurface": "dirt", "stone": "stone", "sea": "water" },
              "snowLine": 999,
              "trees": { "density": 0.015, "template": "oak" },
              "fog": { "density": 1.4, "tintStrength": 0.4 },
              "spawns": [
                { "entity": "deer", "maxCount": 8, "minSpawnDistance": 16, "spawnInterval": 30, "activity": "diurnal" }
              ]
            }
            """);

        File.WriteAllText(
            Path.Combine(directory, "taiga.json"),
            """
            {
              "id": "taiga",
              "terrain": { "baseHeight": 78, "amplitude": 40, "frequency": 0.007, "octaves": 5 },
              "blocks": { "surface": "grass", "subsurface": "dirt", "stone": "stone", "sea": "water" },
              "snowLine": 100,
              "trees": { "density": 0.02, "template": "spruce" },
              "fog": { "density": 1.2, "tintStrength": 0.3 },
              "spawns": []
            }
            """);

        File.WriteAllText(
            Path.Combine(directory, "steppe.json"),
            """
            {
              "id": "steppe",
              "terrain": { "baseHeight": 66, "amplitude": 14, "frequency": 0.0075, "octaves": 3 },
              "blocks": { "surface": "dry_grass", "subsurface": "dirt", "stone": "stone", "sea": "water" },
              "snowLine": 999,
              "trees": { "density": 0.005, "template": "shrub" },
              "fog": { "density": 0.95, "tintStrength": 0.3 },
              "spawns": []
            }
            """);

        File.WriteAllText(
            Path.Combine(directory, "savanna.json"),
            """
            {
              "id": "savanna",
              "terrain": { "baseHeight": 68, "amplitude": 18, "frequency": 0.0085, "octaves": 4 },
              "blocks": { "surface": "dry_grass", "subsurface": "dirt", "stone": "stone", "sea": "water" },
              "snowLine": 999,
              "trees": { "density": 0.008, "template": "acacia" },
              "fog": { "density": 0.72, "tintStrength": 0.22 },
              "spawns": []
            }
            """);

        File.WriteAllText(
            Path.Combine(directory, "desert.json"),
            """
            {
              "id": "desert",
              "terrain": { "baseHeight": 62, "amplitude": 10, "frequency": 0.006, "octaves": 3 },
              "blocks": { "surface": "sand", "subsurface": "sand", "stone": "stone", "sea": "water" },
              "snowLine": 999,
              "trees": { "density": 0.003, "template": "cactus" },
              "fog": { "density": 0.6, "tintStrength": 0.22 },
              "spawns": []
            }
            """);

        File.WriteAllText(
            Path.Combine(directory, "tropics.json"),
            """
            {
              "id": "tropics",
              "terrain": { "baseHeight": 74, "amplitude": 30, "frequency": 0.011, "octaves": 4 },
              "blocks": { "surface": "grass", "subsurface": "dirt", "stone": "stone", "sea": "water" },
              "snowLine": 999,
              "trees": { "density": 0.025, "template": "palm" },
              "fog": { "density": 0.98, "tintStrength": 0.3 },
              "spawns": []
            }
            """);
    }
}
