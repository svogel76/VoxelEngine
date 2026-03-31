using System.Text.Json;
using Silk.NET.Input;
using VoxelEngine.Core;

namespace VoxelEngine.Game;

public static class KeyBindingLoader
{
    public static KeyBindings LoadFrom(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        string directoryPath = ResolveDirectoryPath(path);
        string filePath = Path.Combine(directoryPath, "keybindings.json");
        var defaults = new KeyBindings();

        if (!File.Exists(filePath))
            return defaults;

        KeyBindingsDocument? document;
        try
        {
            document = JsonSerializer.Deserialize<KeyBindingsDocument>(File.ReadAllText(filePath), JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Invalid key binding JSON in '{filePath}'.", ex);
        }

        if (document?.Bindings is null)
            return defaults;

        Key moveForward = defaults.MoveForward;
        Key moveBackward = defaults.MoveBackward;
        Key moveLeft = defaults.MoveLeft;
        Key moveRight = defaults.MoveRight;
        Key jump = defaults.Jump;
        Key sneak = defaults.Sneak;
        MouseButton blockPlace = defaults.BlockPlace;
        MouseButton blockBreak = defaults.BlockBreak;
        Key toggleInventory = defaults.ToggleInventory;
        Key hotbar1 = defaults.Hotbar1;
        Key hotbar2 = defaults.Hotbar2;
        Key hotbar3 = defaults.Hotbar3;
        Key hotbar4 = defaults.Hotbar4;
        Key hotbar5 = defaults.Hotbar5;
        Key hotbar6 = defaults.Hotbar6;
        Key hotbar7 = defaults.Hotbar7;
        Key hotbar8 = defaults.Hotbar8;
        Key hotbar9 = defaults.Hotbar9;
        ScrollBinding hotbarScrollUp = defaults.HotbarScrollUp;
        ScrollBinding hotbarScrollDown = defaults.HotbarScrollDown;
        Key pause = defaults.Pause;
        Key debugConsole = defaults.DebugConsole;

        foreach (var entry in document.Bindings)
        {
            switch (entry.Key)
            {
                case "move_forward":
                    moveForward = ParseKey(entry.Value, entry.Key, filePath); break;
                case "move_backward":
                    moveBackward = ParseKey(entry.Value, entry.Key, filePath); break;
                case "move_left":
                    moveLeft = ParseKey(entry.Value, entry.Key, filePath); break;
                case "move_right":
                    moveRight = ParseKey(entry.Value, entry.Key, filePath); break;
                case "jump":
                    jump = ParseKey(entry.Value, entry.Key, filePath); break;
                case "sneak":
                    sneak = ParseKey(entry.Value, entry.Key, filePath); break;
                case "block_place":
                    blockPlace = ParseMouseButton(entry.Value, entry.Key, filePath); break;
                case "block_break":
                    blockBreak = ParseMouseButton(entry.Value, entry.Key, filePath); break;
                case "toggle_inventory":
                    toggleInventory = ParseKey(entry.Value, entry.Key, filePath); break;
                case "hotbar_1":
                    hotbar1 = ParseKey(entry.Value, entry.Key, filePath); break;
                case "hotbar_2":
                    hotbar2 = ParseKey(entry.Value, entry.Key, filePath); break;
                case "hotbar_3":
                    hotbar3 = ParseKey(entry.Value, entry.Key, filePath); break;
                case "hotbar_4":
                    hotbar4 = ParseKey(entry.Value, entry.Key, filePath); break;
                case "hotbar_5":
                    hotbar5 = ParseKey(entry.Value, entry.Key, filePath); break;
                case "hotbar_6":
                    hotbar6 = ParseKey(entry.Value, entry.Key, filePath); break;
                case "hotbar_7":
                    hotbar7 = ParseKey(entry.Value, entry.Key, filePath); break;
                case "hotbar_8":
                    hotbar8 = ParseKey(entry.Value, entry.Key, filePath); break;
                case "hotbar_9":
                    hotbar9 = ParseKey(entry.Value, entry.Key, filePath); break;
                case "hotbar_scroll_up":
                    hotbarScrollUp = ParseScrollBinding(entry.Value, entry.Key, filePath); break;
                case "hotbar_scroll_down":
                    hotbarScrollDown = ParseScrollBinding(entry.Value, entry.Key, filePath); break;
                case "pause":
                    pause = ParseKey(entry.Value, entry.Key, filePath); break;
                case "debug_console":
                    debugConsole = ParseKey(entry.Value, entry.Key, filePath); break;
                default:
                    Console.WriteLine($"Ignoring unknown key binding action '{entry.Key}' in '{filePath}'.");
                    break;
            }
        }

        return new KeyBindings
        {
            MoveForward = moveForward,
            MoveBackward = moveBackward,
            MoveLeft = moveLeft,
            MoveRight = moveRight,
            Jump = jump,
            Sneak = sneak,
            BlockPlace = blockPlace,
            BlockBreak = blockBreak,
            ToggleInventory = toggleInventory,
            Hotbar1 = hotbar1,
            Hotbar2 = hotbar2,
            Hotbar3 = hotbar3,
            Hotbar4 = hotbar4,
            Hotbar5 = hotbar5,
            Hotbar6 = hotbar6,
            Hotbar7 = hotbar7,
            Hotbar8 = hotbar8,
            Hotbar9 = hotbar9,
            HotbarScrollUp = hotbarScrollUp,
            HotbarScrollDown = hotbarScrollDown,
            Pause = pause,
            DebugConsole = debugConsole
        };
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static Key ParseKey(string value, string actionName, string filePath)
    {
        if (Enum.TryParse<Key>(value, ignoreCase: true, out var key))
            return key;

        throw new InvalidOperationException($"Invalid key '{value}' for action '{actionName}' in '{filePath}'.");
    }

    private static MouseButton ParseMouseButton(string value, string actionName, string filePath)
    {
        string normalized = value.StartsWith("Mouse", StringComparison.OrdinalIgnoreCase)
            ? value[5..]
            : value;

        if (Enum.TryParse<MouseButton>(normalized, ignoreCase: true, out var button))
            return button;

        throw new InvalidOperationException($"Invalid mouse button '{value}' for action '{actionName}' in '{filePath}'.");
    }

    private static ScrollBinding ParseScrollBinding(string value, string actionName, string filePath)
    {
        return value switch
        {
            "ScrollUp" => ScrollBinding.Up,
            "ScrollDown" => ScrollBinding.Down,
            _ => throw new InvalidOperationException($"Invalid scroll binding '{value}' for action '{actionName}' in '{filePath}'.")
        };
    }

    private static string ResolveDirectoryPath(string path)
    {
        if (Path.IsPathRooted(path))
            return path;

        string currentDirectoryPath = Path.GetFullPath(path);
        if (Directory.Exists(currentDirectoryPath))
            return currentDirectoryPath;

        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
    }

    private sealed class KeyBindingsDocument
    {
        public Dictionary<string, string>? Bindings { get; init; }
    }
}
