using VoxelEngine.Game.Blocks;

namespace VoxelEngine.Tests.Mocks;

public sealed class MinimalTestGame : IGameMod
{
    private readonly string? _assetRoot;

    public MinimalTestGame(string? assetRoot = null)
    {
        _assetRoot = assetRoot;
    }

    public void RegisterBlocks(IBlockRegistry registry)
    {
        if (!string.IsNullOrWhiteSpace(_assetRoot))
            new BlockDefinitionLoader(_assetRoot).LoadInto(registry);
    }

    public void Initialize(IGameContext context)
    {
    }

    public void Update(double deltaTime)
    {
    }

    public void Render(double deltaTime)
    {
    }

    public void Shutdown()
    {
    }
}
