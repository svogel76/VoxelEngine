using VoxelEngine.World;

namespace VoxelEngine.Core;

public interface IGameContext
{
    IBlockRegistry BlockRegistry { get; }
    IWorldAccess World { get; }
    IInputState Input { get; }
    IKeyBindings KeyBindings { get; }
}
