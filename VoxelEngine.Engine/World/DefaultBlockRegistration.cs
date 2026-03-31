namespace VoxelEngine.World;

public static class DefaultBlockRegistration
{
    public static void RegisterDefaults(IBlockRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        RegisterIfMissing(registry, new BlockDefinition
        {
            Id = BlockType.Air,
            Name = "air",
            TopTextureIndex = 2,
            SideTextureIndex = 2,
            BottomTextureIndex = 2,
            Solid = false,
            Transparent = false,
            CollidesWithPlayer = false,
            Replaceable = true,
            Luminance = 0,
            Tags = ["empty"]
        });

        RegisterIfMissing(registry, new BlockDefinition
        {
            Id = BlockType.Grass,
            Name = "grass",
            TopTextureIndex = 0,
            SideTextureIndex = 3,
            BottomTextureIndex = 1,
            Solid = true,
            Transparent = false,
            Replaceable = false,
            Luminance = 0,
            Tags = ["natural", "soil"]
        });

        RegisterIfMissing(registry, new BlockDefinition
        {
            Id = BlockType.Dirt,
            Name = "dirt",
            TopTextureIndex = 1,
            SideTextureIndex = 1,
            BottomTextureIndex = 1,
            Solid = true,
            Transparent = false,
            Replaceable = false,
            Luminance = 0,
            Tags = ["natural", "soil"]
        });

        RegisterIfMissing(registry, new BlockDefinition
        {
            Id = BlockType.Stone,
            Name = "stone",
            TopTextureIndex = 2,
            SideTextureIndex = 2,
            BottomTextureIndex = 2,
            Solid = true,
            Transparent = false,
            Replaceable = false,
            Luminance = 0,
            Tags = ["natural", "rock"]
        });

        RegisterIfMissing(registry, new BlockDefinition
        {
            Id = BlockType.Sand,
            Name = "sand",
            TopTextureIndex = 4,
            SideTextureIndex = 4,
            BottomTextureIndex = 4,
            Solid = true,
            Transparent = false,
            Replaceable = false,
            Luminance = 0,
            Tags = ["natural", "sand"]
        });

        RegisterIfMissing(registry, new BlockDefinition
        {
            Id = BlockType.Water,
            Name = "water",
            TopTextureIndex = 8,
            SideTextureIndex = 8,
            BottomTextureIndex = 8,
            Solid = false,
            Transparent = true,
            CollidesWithPlayer = false,
            RenderBackfaces = true,
            Replaceable = true,
            Luminance = 0,
            Tags = ["natural", "liquid"]
        });

        RegisterIfMissing(registry, new BlockDefinition
        {
            Id = BlockType.Glass,
            Name = "glass",
            TopTextureIndex = 9,
            SideTextureIndex = 9,
            BottomTextureIndex = 9,
            Solid = false,
            Transparent = true,
            CollidesWithPlayer = false,
            Replaceable = false,
            Luminance = 0,
            Tags = ["crafted", "transparent"]
        });

        RegisterIfMissing(registry, new BlockDefinition
        {
            Id = BlockType.Ice,
            Name = "ice",
            TopTextureIndex = 10,
            SideTextureIndex = 10,
            BottomTextureIndex = 10,
            Solid = false,
            Transparent = true,
            CollidesWithPlayer = false,
            Replaceable = false,
            Luminance = 0,
            Tags = ["natural", "frozen"]
        });

        RegisterIfMissing(registry, new BlockDefinition
        {
            Id = BlockType.DryGrass,
            Name = "dry_grass",
            TopTextureIndex = 11,
            SideTextureIndex = 12,
            BottomTextureIndex = 1,
            Solid = true,
            Transparent = false,
            Replaceable = false,
            Luminance = 0,
            Tags = ["natural", "soil", "dry"]
        });

        RegisterIfMissing(registry, new BlockDefinition
        {
            Id = BlockType.Snow,
            Name = "snow",
            TopTextureIndex = 5,
            SideTextureIndex = 5,
            BottomTextureIndex = 5,
            Solid = true,
            Transparent = false,
            Replaceable = false,
            Luminance = 0,
            Tags = ["natural", "cold"]
        });

        RegisterIfMissing(registry, new BlockDefinition
        {
            Id = BlockType.Wood,
            Name = "wood",
            TopTextureIndex = 13,
            SideTextureIndex = 13,
            BottomTextureIndex = 13,
            Solid = true,
            Transparent = false,
            Replaceable = false,
            Luminance = 0,
            Tags = ["natural", "wood"]
        });

        RegisterIfMissing(registry, new BlockDefinition
        {
            Id = BlockType.Leaves,
            Name = "leaves",
            TopTextureIndex = 14,
            SideTextureIndex = 14,
            BottomTextureIndex = 14,
            Solid = false,
            Transparent = false,
            Cutout = true,
            CollidesWithPlayer = true,
            RenderBackfaces = true,
            Replaceable = false,
            Luminance = 0,
            Tags = ["natural", "foliage"]
        });
    }

    private static void RegisterIfMissing(IBlockRegistry registry, BlockDefinition definition)
    {
        if (registry.Get(definition.Id) is null)
        {
            registry.Register(definition);
        }
    }
}