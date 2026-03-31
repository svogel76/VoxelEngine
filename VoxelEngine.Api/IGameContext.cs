using VoxelEngine.Api.World;

namespace VoxelEngine.Api;

public interface IGameContext
{
    IBlockRegistry BlockRegistry { get; }
    IWorldAccess World { get; }
    IInputState Input { get; }
    IKeyBindings KeyBindings { get; }
}
