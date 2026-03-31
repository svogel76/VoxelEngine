using VoxelEngine.World;

namespace VoxelEngine.Core;

public interface IGame
{
    void RegisterBlocks(IBlockRegistry registry);
    void Initialize(IGameContext context);
    void Update(double deltaTime);
    void Render(double deltaTime);
    void Shutdown();
}