using VoxelEngine.Api;
using VoxelEngine.Api.Entity;
using VoxelEngine.Game.Blocks;

namespace VoxelEngine.Game;

public sealed class VoxelGame : IGameMod, IModAssetAware
{
    private string _assetBasePath = Path.GetFullPath(Path.Combine("Mods", "VoxelGame", "Assets"));

    public string Id => "voxelgame";

    public void SetAssetBasePath(string assetBasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assetBasePath);
        _assetBasePath = Path.GetFullPath(assetBasePath);
    }

    public void RegisterComponents(IComponentRegistry registry)
    {
    }

    public void RegisterBehaviours(IBehaviourRegistry registry)
    {
    }

    public void RegisterBlocks(IBlockRegistry registry)
    {
        new BlockDefinitionLoader(_assetBasePath).LoadInto(registry);
    }

    public void Initialize(IModContext context)
    {
        _assetBasePath = context.AssetBasePath;
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
