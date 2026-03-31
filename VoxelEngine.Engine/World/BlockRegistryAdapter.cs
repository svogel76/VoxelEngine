namespace VoxelEngine.World;

public sealed class BlockRegistryAdapter : IBlockRegistry
{
    public static BlockRegistryAdapter Instance { get; } = new();

    private BlockRegistryAdapter()
    {
    }

    public void Register(BlockDefinition definition) => BlockRegistry.Register(definition);

    public BlockDefinition? Get(int id) => BlockRegistry.TryGet(id);
}