namespace VoxelEngine.Api.World;

public sealed record BlockDefinition
{
    public required byte Id { get; init; }
    public required string Name { get; init; }
    public required int TopTextureIndex { get; init; }
    public required int SideTextureIndex { get; init; }
    public required int BottomTextureIndex { get; init; }
    public required bool Solid { get; init; }
    public required bool Transparent { get; init; }
    public bool Cutout { get; init; }
    public bool CollidesWithPlayer { get; init; } = true;
    public bool RenderBackfaces { get; init; }
    public required bool Replaceable { get; init; }
    public int Luminance { get; init; }
    public required int SkyLightAttenuation { get; init; }
    public string[] Tags { get; init; } = [];
    public int MaxStackSize { get; init; } = 64;

    public int GetTile(FaceDirection face) => face switch
    {
        FaceDirection.Top => TopTextureIndex,
        FaceDirection.Bottom => BottomTextureIndex,
        _ => SideTextureIndex,
    };
}
