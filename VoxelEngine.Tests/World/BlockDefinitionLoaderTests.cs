using System.Text.Json;
using FluentAssertions;
using VoxelEngine.Game.Blocks;
using VoxelEngine.Tests.Mocks;
using VoxelEngine.World;

namespace VoxelEngine.Tests.World;

public sealed class BlockDefinitionLoaderTests : IDisposable
{
    private readonly string _tempRoot = Path.Combine(Path.GetTempPath(), $"VoxelEngine.BlockLoader.{Guid.NewGuid():N}");

    [Fact]
    public void LoadInto_ValidJson_RegistersBlockDefinition()
    {
        // Arrange
        WriteTextureManifest("stone", "grass_top", "grass_side", "dirt");
        WriteBlockJson("grass.json", """
        {
          "id": 1,
          "name": "grass",
          "textures": {
            "top": "grass_top",
            "side": "grass_side",
            "bottom": "dirt"
          },
          "properties": {
            "solid": true,
            "transparent": false,
            "replaceable": false,
            "max_stack": 64
          },
          "behaviour": "default"
        }
        """);

        var registry = new RecordingBlockRegistry();
        var game = new MinimalTestGame(_tempRoot);

        // Act
        game.RegisterBlocks(registry);

        // Assert
        registry.Definitions.Should().ContainSingle();
        var definition = registry.Definitions[0];
        definition.Id.Should().Be(1);
        definition.Name.Should().Be("grass");
        definition.TopTextureIndex.Should().Be(1);
        definition.SideTextureIndex.Should().Be(2);
        definition.BottomTextureIndex.Should().Be(3);
        definition.Solid.Should().BeTrue();
        definition.Transparent.Should().BeFalse();
        definition.Replaceable.Should().BeFalse();
        definition.MaxStackSize.Should().Be(64);
    }

    [Fact]
    public void LoadInto_DuplicateId_ThrowsWithFilename()
    {
        // Arrange
        WriteTextureManifest("stone");
        WriteBlockJson("stone.json", """
        {
          "id": 3,
          "name": "stone",
          "textures": {
            "top": "stone",
            "side": "stone",
            "bottom": "stone"
          },
          "properties": {
            "solid": true,
            "transparent": false,
            "replaceable": false
          },
          "behaviour": "default"
        }
        """);
        WriteBlockJson("smooth_stone.json", """
        {
          "id": 3,
          "name": "smooth_stone",
          "textures": {
            "top": "stone",
            "side": "stone",
            "bottom": "stone"
          },
          "properties": {
            "solid": true,
            "transparent": false,
            "replaceable": false
          },
          "behaviour": "default"
        }
        """);

        var loader = new BlockDefinitionLoader(_tempRoot);
        var registry = new RecordingBlockRegistry();

        // Act
        Action act = () => loader.LoadInto(registry);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Duplicate block ID 3*stone.json*smooth_stone.json*");
    }

    [Fact]
    public void LoadInto_UnknownTextureReference_ThrowsWithBlockNameAndTextureKey()
    {
        // Arrange
        WriteTextureManifest("stone");
        WriteBlockJson("glass.json", """
        {
          "id": 6,
          "name": "glass",
          "textures": {
            "top": "glass",
            "side": "glass",
            "bottom": "glass"
          },
          "properties": {
            "solid": false,
            "transparent": true,
            "replaceable": false
          },
          "behaviour": "default"
        }
        """);

        var loader = new BlockDefinitionLoader(_tempRoot);
        var registry = new RecordingBlockRegistry();

        // Act
        Action act = () => loader.LoadInto(registry);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Unknown texture reference 'glass'*block 'glass'*key 'top'*");
    }

    [Fact]
    public void LoadInto_MissingRequiredField_ThrowsWithFilename()
    {
        // Arrange
        WriteTextureManifest("stone");
        WriteBlockJson("broken.json", """
        {
          "textures": {
            "top": "stone",
            "side": "stone",
            "bottom": "stone"
          },
          "properties": {
            "solid": true,
            "transparent": false,
            "replaceable": false
          },
          "behaviour": "default"
        }
        """);

        var loader = new BlockDefinitionLoader(_tempRoot);
        var registry = new RecordingBlockRegistry();

        // Act
        Action act = () => loader.LoadInto(registry);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*broken.json*'id'*");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
            Directory.Delete(_tempRoot, recursive: true);
    }

    private void WriteTextureManifest(params string[] layers)
    {
        string texturesDirectory = Path.Combine(_tempRoot, "Textures");
        Directory.CreateDirectory(texturesDirectory);

        string content = JsonSerializer.Serialize(new { layers }, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(texturesDirectory, "blocks.manifest.json"), content);
    }

    private void WriteBlockJson(string fileName, string content)
    {
        string blocksDirectory = Path.Combine(_tempRoot, "Blocks");
        Directory.CreateDirectory(blocksDirectory);
        File.WriteAllText(Path.Combine(blocksDirectory, fileName), content);
    }

    private sealed class RecordingBlockRegistry : IBlockRegistry
    {
        public List<BlockDefinition> Definitions { get; } = [];

        public void Register(BlockDefinition definition)
        {
            Definitions.Add(definition);
        }

        public BlockDefinition? Get(int id) => Definitions.FirstOrDefault(definition => definition.Id == id);
    }
}
