using System.Numerics;

namespace VoxelEngine.Persistence;

/// <summary>Gespeicherter Spielerstand: Position, Flugmodus, Inventar und Vitalwerte.</summary>
public sealed record PlayerState(
    Vector3 Position,
    float Yaw,
    float Pitch,
    bool FlyMode,
    int SelectedSlot,
    IReadOnlyList<ItemStackData?> Hotbar,
    IReadOnlyList<ItemStackData?> InventoryGrid,
    IReadOnlyList<ItemStackData?> EquipmentSlots,
    float Health = 20f,
    float Hunger = 20f);

/// <summary>Serialisierbarer ItemStack (blockType + count).</summary>
public sealed record ItemStackData(byte BlockType, int Count);
