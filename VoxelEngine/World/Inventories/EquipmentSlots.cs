using VoxelEngine.World;

namespace VoxelEngine.World.Inventories;

/// <summary>
/// Die vier Ausrüstungs-Slots des Spielers.
/// Pure C# — kein Silk.NET, vollständig testbar.
/// </summary>
public sealed class EquipmentSlots
{
    public const int Count = 4;

    private readonly ItemStack?[] _slots = new ItemStack?[Count];

    /// <summary>Alle Slots in der Reihenfolge Helm, Brust, Beine, Schuhe.</summary>
    public IReadOnlyList<ItemStack?> Slots => _slots;

    public ItemStack? Get(EquipmentSlotType slot) => _slots[(int)slot];

    /// <summary>Setzt einen Slot direkt (für Tests / Laden von Spielstand).</summary>
    public void Set(EquipmentSlotType slot, ItemStack? stack)
        => _slots[(int)slot] = stack;

    /// <summary>
    /// Legt ein Item in den passenden Ausrüstungs-Slot.
    /// Aktuell: jeder Block-Typ kann in jeden Slot gelegt werden (Platzhalter für späteres Mod-System).
    /// Gibt den zuvor belegten Slot zurück (null wenn leer).
    /// </summary>
    public ItemStack? Equip(EquipmentSlotType slot, ItemStack? item)
    {
        var prev = _slots[(int)slot];
        _slots[(int)slot] = item;
        return prev;
    }

    /// <summary>Entfernt das Item aus dem Slot und gibt es zurück.</summary>
    public ItemStack? Unequip(EquipmentSlotType slot)
    {
        var item = _slots[(int)slot];
        _slots[(int)slot] = null;
        return item;
    }
}
