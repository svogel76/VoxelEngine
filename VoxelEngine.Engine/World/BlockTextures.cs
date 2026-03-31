namespace VoxelEngine.World;

public static class BlockTextures
{
    public static int GetTileIndex(byte blockType, FaceDirection face) =>
        BlockRegistry.Get(blockType).GetTile(face);
}
