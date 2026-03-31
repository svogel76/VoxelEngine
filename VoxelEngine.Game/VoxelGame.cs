using VoxelEngine.Core;
using VoxelEngine.World;

namespace VoxelEngine.Game;

public sealed class VoxelGame : IGame
{
    public void RegisterBlocks(IBlockRegistry registry)
    {
        DefaultBlockRegistration.RegisterDefaults(registry);
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