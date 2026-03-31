namespace VoxelEngine.Api.World;

public interface IWorldAccess
{
    int LoadedChunkCount { get; }
    byte GetBlock(int worldX, int worldY, int worldZ);
    void SetBlock(int worldX, int worldY, int worldZ, byte type);
}
