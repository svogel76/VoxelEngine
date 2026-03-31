using Silk.NET.Input;
using VoxelEngine.Core;

namespace VoxelEngine.Game;

public sealed class KeyBindings : IKeyBindings
{
    public Key MoveForward { get; init; } = Key.W;
    public Key MoveBackward { get; init; } = Key.S;
    public Key MoveLeft { get; init; } = Key.A;
    public Key MoveRight { get; init; } = Key.D;
    public Key Jump { get; init; } = Key.Space;
    public Key Sneak { get; init; } = Key.ShiftLeft;
    public MouseButton BlockPlace { get; init; } = MouseButton.Right;
    public MouseButton BlockBreak { get; init; } = MouseButton.Left;
    public Key ToggleInventory { get; init; } = Key.Tab;
    public Key Hotbar1 { get; init; } = Key.Number1;
    public Key Hotbar2 { get; init; } = Key.Number2;
    public Key Hotbar3 { get; init; } = Key.Number3;
    public Key Hotbar4 { get; init; } = Key.Number4;
    public Key Hotbar5 { get; init; } = Key.Number5;
    public Key Hotbar6 { get; init; } = Key.Number6;
    public Key Hotbar7 { get; init; } = Key.Number7;
    public Key Hotbar8 { get; init; } = Key.Number8;
    public Key Hotbar9 { get; init; } = Key.Number9;
    public ScrollBinding HotbarScrollUp { get; init; } = ScrollBinding.Up;
    public ScrollBinding HotbarScrollDown { get; init; } = ScrollBinding.Down;
    public Key Pause { get; init; } = Key.Escape;
    public Key DebugConsole { get; init; } = Key.F1;
}
