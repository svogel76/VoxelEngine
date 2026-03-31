using VoxelEngine.Api.World;

namespace VoxelEngine.Api;

public interface IGameMod
{
    void RegisterBlocks(IBlockRegistry registry);
    void Initialize(IGameContext context);
    void Update(double deltaTime);
    void Render(double deltaTime);
    void Shutdown();
}
