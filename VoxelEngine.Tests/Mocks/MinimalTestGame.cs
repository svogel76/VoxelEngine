using VoxelEngine.Core;
using VoxelEngine.World;

namespace VoxelEngine.Tests.Mocks;

public sealed class MinimalTestGame : IGame
{
    public void RegisterBlocks(IBlockRegistry registry)
    {
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