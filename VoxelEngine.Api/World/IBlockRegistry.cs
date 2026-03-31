namespace VoxelEngine.Api.World;

public interface IBlockRegistry
{
    void Register(BlockDefinition definition);
    BlockDefinition? Get(int id);
}
