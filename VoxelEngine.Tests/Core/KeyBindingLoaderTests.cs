using FluentAssertions;
using VoxelEngine.Game;

namespace VoxelEngine.Tests.Core;

public sealed class KeyBindingLoaderTests : IDisposable
{
    private readonly string _tempRoot = Path.Combine(Path.GetTempPath(), $"VoxelEngine.KeyBindings.{Guid.NewGuid():N}");

    [Fact]
    public void LoadFrom_ValidJson_LoadsAllValues()
    {
        WriteKeyBindingsJson("""
        {
          "bindings": {
            "move_forward": "Up",
            "move_backward": "Down",
            "move_left": "Left",
            "move_right": "Right",
            "jump": "Space",
            "sneak": "ControlLeft",
            "block_place": "MouseRight",
            "block_break": "MouseMiddle",
            "toggle_inventory": "I",
            "hotbar_1": "F1",
            "hotbar_2": "F2",
            "hotbar_3": "F3",
            "hotbar_4": "F4",
            "hotbar_5": "F5",
            "hotbar_6": "F6",
            "hotbar_7": "F7",
            "hotbar_8": "F8",
            "hotbar_9": "F9",
            "hotbar_scroll_up": "ScrollDown",
            "hotbar_scroll_down": "ScrollUp",
            "pause": "P",
            "debug_console": "GraveAccent"
          }
        }
        """);

        var bindings = KeyBindingLoader.LoadFrom(_tempRoot);

        bindings.MoveForward.Should().Be(Key.Up);
        bindings.MoveBackward.Should().Be(Key.Down);
        bindings.MoveLeft.Should().Be(Key.Left);
        bindings.MoveRight.Should().Be(Key.Right);
        bindings.Jump.Should().Be(Key.Space);
        bindings.Sneak.Should().Be(Key.ControlLeft);
        bindings.BlockPlace.Should().Be(MouseButton.Right);
        bindings.BlockBreak.Should().Be(MouseButton.Middle);
        bindings.ToggleInventory.Should().Be(Key.I);
        bindings.Hotbar1.Should().Be(Key.F1);
        bindings.Hotbar9.Should().Be(Key.F9);
        bindings.HotbarScrollUp.Should().Be(ScrollBinding.Down);
        bindings.HotbarScrollDown.Should().Be(ScrollBinding.Up);
        bindings.Pause.Should().Be(Key.P);
        bindings.DebugConsole.Should().Be(Key.GraveAccent);
    }

    [Fact]
    public void LoadFrom_MissingKey_FallsBackToDefault()
    {
        var defaults = new KeyBindings();
        WriteKeyBindingsJson("""
        {
          "bindings": {
            "move_forward": "Up"
          }
        }
        """);

        var bindings = KeyBindingLoader.LoadFrom(_tempRoot);

        bindings.MoveForward.Should().Be(Key.Up);
        bindings.MoveBackward.Should().Be(defaults.MoveBackward);
        bindings.BlockBreak.Should().Be(defaults.BlockBreak);
        bindings.ToggleInventory.Should().Be(defaults.ToggleInventory);
    }

    [Fact]
    public void LoadFrom_FileNotFound_ReturnsDefaults()
    {
        var defaults = new KeyBindings();
        var bindings = KeyBindingLoader.LoadFrom(_tempRoot);
        bindings.Should().BeEquivalentTo(defaults);
    }

    [Fact]
    public void LoadFrom_InvalidJson_ThrowsWithFilename()
    {
        WriteKeyBindingsJson("{ invalid json }");
        Action act = () => KeyBindingLoader.LoadFrom(_tempRoot);
        act.Should().Throw<InvalidOperationException>().WithMessage("*keybindings.json*");
    }

    [Fact]
    public void LoadFrom_UnknownAction_IgnoresEntry()
    {
        var defaults = new KeyBindings();
        WriteKeyBindingsJson("""
        {
          "bindings": {
            "unknown_action": "Q",
            "pause": "P"
          }
        }
        """);

        var bindings = KeyBindingLoader.LoadFrom(_tempRoot);

        bindings.Pause.Should().Be(Key.P);
        bindings.MoveForward.Should().Be(defaults.MoveForward);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
            Directory.Delete(_tempRoot, recursive: true);
    }

    private void WriteKeyBindingsJson(string content)
    {
        Directory.CreateDirectory(_tempRoot);
        File.WriteAllText(Path.Combine(_tempRoot, "keybindings.json"), content);
    }
}
