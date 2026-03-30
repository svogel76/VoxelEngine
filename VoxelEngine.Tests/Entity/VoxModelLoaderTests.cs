using FluentAssertions;
using VoxelEngine.Entity.Models;

namespace VoxelEngine.Tests.Entity;

public class VoxModelLoaderTests
{
    [Fact]
    public void Parse_UsesEmbeddedPaletteAndMapsColorIndicesToTintAndAtlasTile()
    {
        // Arrange
        byte[] data = VoxTestFileBuilder.Create(
            new VoxTestModelSize(2, 2, 2),
            [new VoxTestVoxel(1, 0, 1, 2)],
            paletteEntries: new Dictionary<byte, uint> { [2] = 0xFF112233 });

        // Act
        var model = VoxModelLoader.Parse(data, "test");

        // Assert
        model.Id.Should().Be("test");
        model.VoxelSize.Should().Be(1.0f);
        model.Voxels.Should().ContainSingle();
        model.Voxels[0].X.Should().Be(1);
        model.Voxels[0].Y.Should().Be(1);
        model.Voxels[0].Z.Should().Be(1);
        model.Voxels[0].TileX.Should().Be(0);
        model.Voxels[0].TileY.Should().Be(0);
        model.Voxels[0].Tint.Should().Be(new VoxelTint(0x33, 0x22, 0x11, 0xFF));
    }

    [Fact]
    public void Parse_UsesDefaultPaletteWhenRgbaChunkIsMissing()
    {
        // Arrange
        byte[] data = VoxTestFileBuilder.Create(
            new VoxTestModelSize(1, 1, 1),
            [new VoxTestVoxel(0, 0, 0, 1)],
            paletteEntries: null);

        // Act
        var model = VoxModelLoader.Parse(data, "default");

        // Assert
        model.Voxels.Should().ContainSingle();
        model.Voxels[0].Tint.Should().Be(new VoxelTint(0xFF, 0xFF, 0xFF, 0xFF));
    }

    [Fact]
    public void Parse_AppliesConfiguredVoxelScale()
    {
        // Arrange
        byte[] data = VoxTestFileBuilder.Create(
            new VoxTestModelSize(1, 1, 1),
            [new VoxTestVoxel(0, 0, 0, 1)],
            paletteEntries: null);

        // Act
        var model = VoxModelLoader.Parse(data, "scaled", 0.5f);

        // Assert
        model.VoxelSize.Should().Be(0.5f);
        model.PlacementBounds.Max.X.Should().BeApproximately(0.25f, 0.0001f);
        model.PlacementBounds.Max.Y.Should().BeApproximately(0.5f, 0.0001f);
    }

    [Fact]
    public void Parse_RotatesMagicaVoxelCoordinatesFromZUpToYUp()
    {
        // Arrange
        byte[] data = VoxTestFileBuilder.Create(
            new VoxTestModelSize(2, 3, 4),
            [new VoxTestVoxel(1, 2, 3, 1)],
            paletteEntries: null);

        // Act
        var model = VoxModelLoader.Parse(data, "rotated");

        // Assert
        model.Voxels.Should().ContainSingle();
        model.Voxels[0].X.Should().Be(1);
        model.Voxels[0].Y.Should().Be(3);
        model.Voxels[0].Z.Should().Be(0);
    }

    [Fact]
    public void Parse_UsesFirstModelWhenFileContainsMultipleModels()
    {
        // Arrange
        byte[] data = VoxTestFileBuilder.CreateWithPack(
            [new VoxTestModelSize(1, 1, 1), new VoxTestModelSize(1, 1, 1)],
            [[new VoxTestVoxel(0, 0, 0, 1)], [new VoxTestVoxel(0, 0, 0, 2)]],
            paletteEntries: new Dictionary<byte, uint>
            {
                [1] = 0xFF010203,
                [2] = 0xFF040506
            });

        // Act
        var model = VoxModelLoader.Parse(data, "multi");

        // Assert
        model.Voxels.Should().ContainSingle();
        model.Voxels[0].Tint.Should().Be(new VoxelTint(0x03, 0x02, 0x01, 0xFF));
    }

    [Fact]
    public void LoadFromDirectory_LoadsVoxFilesAlongsideVxmFiles()
    {
        // Arrange
        string directory = Path.Combine(Path.GetTempPath(), $"vox-import-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        File.Copy(Path.Combine(AppContext.BaseDirectory, "Assets", "Entities", "entity_atlas.png"), Path.Combine(directory, "entity_atlas.png"));
        File.WriteAllText(Path.Combine(directory, "cube.vxm"), "model cube\nvoxelSize 0.25\nvoxel 0 0 0 0 0\n");
        File.WriteAllBytes(
            Path.Combine(directory, "sheep.vox"),
            VoxTestFileBuilder.Create(new VoxTestModelSize(1, 1, 1), [new VoxTestVoxel(0, 0, 0, 1)], null));

        try
        {
            // Act
            var library = FileSystemEntityModelLibrary.LoadFromDirectory(directory, voxelScale: 2.0f);

            // Assert
            library.GetAllModels().Select(model => model.Id).Should().BeEquivalentTo(["cube", "sheep"]);
            library.GetModel("cube").VoxelSize.Should().Be(0.5f);
            library.GetModel("sheep").VoxelSize.Should().Be(2.0f);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void LoadFromDirectory_UsesCompanionJsonScaleOverride()
    {
        // Arrange
        string directory = Path.Combine(Path.GetTempPath(), $"vox-import-scale-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        File.Copy(Path.Combine(AppContext.BaseDirectory, "Assets", "Entities", "entity_atlas.png"), Path.Combine(directory, "entity_atlas.png"));
        File.WriteAllBytes(
            Path.Combine(directory, "deer.vox"),
            VoxTestFileBuilder.Create(new VoxTestModelSize(1, 1, 1), [new VoxTestVoxel(0, 0, 0, 1)], null));
        File.WriteAllText(Path.Combine(directory, "deer.json"), "{ \"scale\": 0.1 }");

        try
        {
            // Act
            var library = FileSystemEntityModelLibrary.LoadFromDirectory(directory, voxelScale: 2.0f);

            // Assert
            library.GetModel("deer").VoxelSize.Should().Be(0.1f);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void LoadFromDirectory_UsesGroupedDisplayMetadataAndBehaviour()
    {
        // Arrange
        string directory = Path.Combine(Path.GetTempPath(), $"vox-import-grouped-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        File.Copy(Path.Combine(AppContext.BaseDirectory, "Assets", "Entities", "entity_atlas.png"), Path.Combine(directory, "entity_atlas.png"));
        File.WriteAllBytes(
            Path.Combine(directory, "deer.vox"),
            VoxTestFileBuilder.Create(new VoxTestModelSize(1, 1, 1), [new VoxTestVoxel(0, 0, 0, 1)], null));
        File.WriteAllText(
            Path.Combine(directory, "deer.json"),
            """
            {
              "display": {
                "scale": 0.1,
                "boundingBox": {
                  "width": 1.2,
                  "height": 1.8
                }
              },
              "behaviour": {
                "moveSpeed": 3.0,
                "fleeSpeed": 6.0,
                "fleeRadius": 8.0,
                "idleTimeMin": 2.0,
                "idleTimeMax": 6.0,
                "wanderRadius": 12.0,
                "dayActivity": "active",
                "nightActivity": "sleep"
              }
            }
            """);

        try
        {
            // Act
            var model = FileSystemEntityModelLibrary.LoadFromDirectory(directory).GetModel("deer");

            // Assert
            model.VoxelSize.Should().Be(0.1f);
            model.PlacementBounds.Min.X.Should().BeApproximately(-0.6f, 0.0001f);
            model.PlacementBounds.Max.Y.Should().BeApproximately(1.8f, 0.0001f);
            model.Metadata.Behaviour.Should().NotBeNull();
            model.Metadata.Behaviour!.FleeRadius.Should().Be(8.0f);
            model.Metadata.Behaviour.DayActivity.Should().Be(EntityTimeOfDayActivity.Active);
            model.Metadata.Behaviour.NightActivity.Should().Be(EntityTimeOfDayActivity.Sleep);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void LoadFromDirectory_PrefersExplicitDisplayModelOverJsonFileName()
    {
        // Arrange
        string directory = Path.Combine(Path.GetTempPath(), $"vox-import-explicit-model-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        File.Copy(Path.Combine(AppContext.BaseDirectory, "Assets", "Entities", "entity_atlas.png"), Path.Combine(directory, "entity_atlas.png"));
        File.WriteAllBytes(
            Path.Combine(directory, "shared.vox"),
            VoxTestFileBuilder.Create(new VoxTestModelSize(1, 1, 1), [new VoxTestVoxel(0, 0, 0, 1)], null));
        File.WriteAllText(
            Path.Combine(directory, "stag.json"),
            """
            {
              "display": {
                "model": "shared.vox",
                "scale": 0.25
              }
            }
            """);

        try
        {
            // Act
            var library = FileSystemEntityModelLibrary.LoadFromDirectory(directory);

            // Assert
            library.GetAllModels().Select(model => model.Id).Should().Contain("stag");
            library.GetModel("stag").VoxelSize.Should().Be(0.25f);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void LoadFromDirectory_AppliesCompanionJsonRotation()
    {
        // Arrange
        string directory = Path.Combine(Path.GetTempPath(), $"vox-import-rotation-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        File.Copy(Path.Combine(AppContext.BaseDirectory, "Assets", "Entities", "entity_atlas.png"), Path.Combine(directory, "entity_atlas.png"));
        File.WriteAllText(
            Path.Combine(directory, "sign.vxm"),
            """
            model sign
            voxelSize 1
            voxel 0 0 0 0 0
            voxel 1 0 0 0 0
            """);
        File.WriteAllText(Path.Combine(directory, "sign.json"), "{ \"rotation\": { \"z\": 90 } }");

        try
        {
            // Act
            var model = FileSystemEntityModelLibrary.LoadFromDirectory(directory).GetModel("sign");

            // Assert
            model.Voxels.Select(voxel => (voxel.X, voxel.Y, voxel.Z))
                .Should()
                .BeEquivalentTo([(0, 0, 0), (0, 1, 0)]);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private readonly record struct VoxTestModelSize(int X, int Y, int Z);
    private readonly record struct VoxTestVoxel(byte X, byte Y, byte Z, byte ColorIndex);

    private static class VoxTestFileBuilder
    {
        public static byte[] Create(VoxTestModelSize size, IReadOnlyList<VoxTestVoxel> voxels, Dictionary<byte, uint>? paletteEntries)
            => CreateWithPack([size], [voxels], paletteEntries);

        public static byte[] CreateWithPack(IReadOnlyList<VoxTestModelSize> sizes, IReadOnlyList<IReadOnlyList<VoxTestVoxel>> modelVoxels, Dictionary<byte, uint>? paletteEntries = null)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            writer.Write("VOX "u8.ToArray());
            writer.Write(150);

            using var mainChildren = new MemoryStream();
            using var childWriter = new BinaryWriter(mainChildren);

            if (sizes.Count > 1)
                WriteChunk(childWriter, "PACK", content => content.Write(sizes.Count));

            for (int i = 0; i < sizes.Count; i++)
            {
                var size = sizes[i];
                WriteChunk(childWriter, "SIZE", content =>
                {
                    content.Write(size.X);
                    content.Write(size.Y);
                    content.Write(size.Z);
                });

                WriteChunk(childWriter, "XYZI", content =>
                {
                    content.Write(modelVoxels[i].Count);
                    foreach (var voxel in modelVoxels[i])
                    {
                        content.Write(voxel.X);
                        content.Write(voxel.Y);
                        content.Write(voxel.Z);
                        content.Write(voxel.ColorIndex);
                    }
                });
            }

            if (paletteEntries is not null)
            {
                WriteChunk(childWriter, "RGBA", content =>
                {
                    for (int i = 1; i < 256; i++)
                    {
                        uint value = paletteEntries.TryGetValue((byte)i, out uint entry)
                            ? entry
                            : 0u;
                        content.Write(value);
                    }
                });
            }

            writer.Write("MAIN"u8.ToArray());
            writer.Write(0);
            writer.Write((int)mainChildren.Length);
            mainChildren.Position = 0;
            mainChildren.CopyTo(stream);

            return stream.ToArray();
        }

        private static void WriteChunk(BinaryWriter writer, string id, Action<BinaryWriter> writeContent)
        {
            using var contentStream = new MemoryStream();
            using var contentWriter = new BinaryWriter(contentStream);
            writeContent(contentWriter);
            contentWriter.Flush();

            writer.Write(System.Text.Encoding.ASCII.GetBytes(id));
            writer.Write((int)contentStream.Length);
            writer.Write(0);
            writer.Write(contentStream.ToArray());
        }
    }
}
