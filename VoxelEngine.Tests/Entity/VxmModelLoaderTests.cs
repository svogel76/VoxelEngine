using FluentAssertions;
using VoxelEngine.Entity.Models;

namespace VoxelEngine.Tests.Entity;

public class VxmModelLoaderTests
{
    [Fact]
    public void Parse_ReadsModelMetadataAndTint()
    {
        // Arrange
        const string content = """
            model sheep
            voxelSize 0.5
            voxel 0 1 2 1 0 12 34 56 200
            """;

        // Act
        var model = VxmModelLoader.Parse(content, "test.vxm");

        // Assert
        model.Id.Should().Be("sheep");
        model.VoxelSize.Should().Be(0.5f);
        model.Voxels.Should().ContainSingle();
        model.Voxels[0].Tint.Should().Be(new VoxelTint(12, 34, 56, 200));
    }

    [Fact]
    public void Parse_ComputesPlacementBoundsAroundGroundPivot()
    {
        // Arrange
        const string content = """
            model test
            voxelSize 0.25
            voxel 0 0 0 0 0
            voxel 1 1 1 1 1
            """;

        // Act
        var model = VxmModelLoader.Parse(content, "bounds.vxm");

        // Assert
        model.PlacementBounds.Min.X.Should().BeApproximately(-0.25f, 0.0001f);
        model.PlacementBounds.Min.Y.Should().BeApproximately(0f, 0.0001f);
        model.PlacementBounds.Min.Z.Should().BeApproximately(-0.25f, 0.0001f);
        model.PlacementBounds.Max.X.Should().BeApproximately(0.25f, 0.0001f);
        model.PlacementBounds.Max.Y.Should().BeApproximately(0.5f, 0.0001f);
        model.PlacementBounds.Max.Z.Should().BeApproximately(0.25f, 0.0001f);
    }

    [Fact]
    public void Parse_AppliesConfiguredVoxelScale()
    {
        // Arrange
        const string content = """
            model scaled
            voxelSize 0.5
            voxel 0 0 0 0 0
            """;

        // Act
        var model = VxmModelLoader.Parse(content, "scaled.vxm", voxelScale: 2.0f);

        // Assert
        model.VoxelSize.Should().Be(1.0f);
        model.PlacementBounds.Max.X.Should().BeApproximately(0.5f, 0.0001f);
        model.PlacementBounds.Max.Y.Should().BeApproximately(1.0f, 0.0001f);
    }
}
