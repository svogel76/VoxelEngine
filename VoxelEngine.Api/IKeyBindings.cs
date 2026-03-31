using VoxelEngine.Api.Input;

namespace VoxelEngine.Api;

public enum ScrollBinding
{
    Up,
    Down
}

public interface IKeyBindings
{
    Key MoveForward { get; }
    Key MoveBackward { get; }
    Key MoveLeft { get; }
    Key MoveRight { get; }
    Key Jump { get; }
    Key Sneak { get; }
    MouseButton BlockPlace { get; }
    MouseButton BlockBreak { get; }
    Key ToggleInventory { get; }
    Key Hotbar1 { get; }
    Key Hotbar2 { get; }
    Key Hotbar3 { get; }
    Key Hotbar4 { get; }
    Key Hotbar5 { get; }
    Key Hotbar6 { get; }
    Key Hotbar7 { get; }
    Key Hotbar8 { get; }
    Key Hotbar9 { get; }
    ScrollBinding HotbarScrollUp { get; }
    ScrollBinding HotbarScrollDown { get; }
    Key Pause { get; }
    Key DebugConsole { get; }
}
