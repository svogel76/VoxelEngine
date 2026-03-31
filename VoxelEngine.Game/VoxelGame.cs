using VoxelEngine.Game.Blocks;

namespace VoxelEngine.Game;

public sealed class VoxelGame : IGameMod
{
    public void RegisterBlocks(IBlockRegistry registry)
    {
        new BlockDefinitionLoader("Assets/").LoadInto(registry);
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
