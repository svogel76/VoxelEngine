using VoxelEngine.Core;
using VoxelEngine.Game.Blocks;
using VoxelEngine.World;

namespace VoxelEngine.Game;

public sealed class VoxelGame : IGame
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
