using VoxelEngine.World;

namespace VoxelEngine.World.Inventories;

/// <summary>
/// Das 4×9 Haupt-Inventar des Spielers (36 Slots, Zeilen 0–3, Spalten 0–8).
/// Pure C# — kein Silk.NET, vollständig testbar.
/// </summary>
public sealed class InventoryGrid
{
    public const int Rows = 4;
    public const int Cols = 9;
    public const int TotalSlots = Rows * Cols;   // 36

    private readonly ItemStack?[] _slots = new ItemStack?[TotalSlots];

    /// <summary>Alle Slots als flaches Array (Index = row * Cols + col).</summary>
    public IReadOnlyList<ItemStack?> Slots => _slots;

    // ── Zugriff ────────────────────────────────────────────────────────────

    public ItemStack? Get(int index)
    {
        ValidateIndex(index);
        return _slots[index];
    }

    public ItemStack? Get(int row, int col) => Get(row * Cols + col);

    /// <summary>Setzt einen Slot direkt (für Tests / Laden von Spielstand).</summary>
    public void Set(int index, ItemStack? stack)
    {
        ValidateIndex(index);
        _slots[index] = stack;
    }

    public void Set(int row, int col, ItemStack? stack) => Set(row * Cols + col, stack);

    // ── Hinzufügen / Entfernen ────────────────────────────────────────────

    /// <summary>
    /// Versucht einen Block ins Inventar zu legen.
    /// Stapelt auf bestehenden Slot (gleicher Typ, Platz frei) oder füllt ersten leeren Slot.
    /// Gibt true zurück bei Erfolg.
    /// </summary>
    public bool TryAdd(byte blockType)
    {
        int max = BlockRegistry.Get(blockType).MaxStackSize;

        for (int i = 0; i < TotalSlots; i++)
            if (_slots[i] is { } s && s.BlockType == blockType && s.Count < max)
            {
                _slots[i] = s with { Count = s.Count + 1 };
                return true;
            }

        for (int i = 0; i < TotalSlots; i++)
            if (_slots[i] is null)
            {
                _slots[i] = new ItemStack(blockType, 1);
                return true;
            }

        return false;
    }

    /// <summary>
    /// Entfernt <paramref name="count"/> Einheiten aus einem Slot.
    /// Bei Count ≤ 0 → Slot wird geleert. Gibt true zurück wenn erfolgreich.
    /// </summary>
    public bool TryRemove(int index, int count = 1)
    {
        ValidateIndex(index);
        if (_slots[index] is not { } stack) return false;

        int newCount  = stack.Count - count;
        _slots[index] = newCount > 0 ? stack with { Count = newCount } : null;
        return true;
    }

    // ── Hilfsmethoden ─────────────────────────────────────────────────────

    private static void ValidateIndex(int index)
    {
        if (index < 0 || index >= TotalSlots)
            throw new ArgumentOutOfRangeException(nameof(index), index, $"Index must be 0–{TotalSlots - 1}.");
    }
}
