using VoxelEngine.Api.Entity;
using VoxelEngine.Api.World;

namespace VoxelEngine.Api;

public interface IGameMod
{
    string Id { get; }
    void RegisterComponents(IComponentRegistry registry);
    void RegisterBehaviours(IBehaviourRegistry registry);
    void RegisterBlocks(IBlockRegistry registry);
    void Initialize(IModContext context);
    void Update(double deltaTime);
    void Render(double deltaTime);
    void Shutdown();
}
