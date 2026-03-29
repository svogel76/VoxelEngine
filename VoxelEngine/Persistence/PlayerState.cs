using System.Numerics;

namespace VoxelEngine.Persistence;

/// <summary>Gespeicherter Spielerstand: Position, Flugmodus und Inventar.</summary>
public sealed record PlayerState(
    Vector3 Position,
    bool FlyMode,
    int SelectedSlot,
    IReadOnlyList<ItemStackData?> Hotbar);

/// <summary>Serialisierbarer ItemStack (blockType + count).</summary>
public sealed record ItemStackData(byte BlockType, int Count);
