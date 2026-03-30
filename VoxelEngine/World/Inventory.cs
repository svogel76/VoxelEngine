namespace VoxelEngine.World;

/// <summary>Ein Stack von gleichartigen Blöcken in einem Inventar-Slot.</summary>
public sealed record ItemStack(byte BlockType, int Count);

/// <summary>Spieler-Inventar mit 9-Slot-Hotbar.</summary>
public sealed class Inventory
{
    public const int HotbarSize = 9;

    private readonly ItemStack?[] _hotbar = new ItemStack?[HotbarSize];

    public IReadOnlyList<ItemStack?> Hotbar => _hotbar;

    public int SelectedSlot { get; private set; } = 0;

    /// <summary>Wählt den nächsten Slot (wraps around: 8 → 0).</summary>
    public void SelectNext()
    {
        SelectedSlot = (SelectedSlot + 1) % HotbarSize;
    }

    /// <summary>Wählt den vorherigen Slot (wraps around: 0 → 8).</summary>
    public void SelectPrevious()
    {
        SelectedSlot = (SelectedSlot - 1 + HotbarSize) % HotbarSize;
    }

    /// <summary>Wählt direkt einen bestimmten Slot (0–8, kein wrap).</summary>
    public void SelectSlot(int slot)
    {
        if (slot < 0 || slot >= HotbarSize)
            return;
        SelectedSlot = slot;
    }

    /// <summary>
    /// Fügt einen Block in den ersten passenden oder freien Slot ein.
    /// Stapelt auf bestehenden Slot wenn gleicher BlockType und Count &lt; MaxStackSize.
    /// Gibt true zurück wenn erfolgreich.
    /// </summary>
    public bool TryAdd(byte blockType)
    {
        int maxStack = BlockRegistry.Get(blockType).MaxStackSize;

        // Zuerst: existierenden Slot mit gleichem BlockType und Platz suchen
        for (int i = 0; i < HotbarSize; i++)
        {
            if (_hotbar[i] is { } stack && stack.BlockType == blockType && stack.Count < maxStack)
            {
                _hotbar[i] = stack with { Count = stack.Count + 1 };
                return true;
            }
        }

        // Dann: ersten leeren Slot suchen
        for (int i = 0; i < HotbarSize; i++)
        {
            if (_hotbar[i] is null)
            {
                _hotbar[i] = new ItemStack(blockType, 1);
                return true;
            }
        }

        return false; // Hotbar voll
    }

    /// <summary>
    /// Entfernt <paramref name="count"/> Blöcke aus dem angegebenen Slot.
    /// Bei Count ≤ 0 wird der Slot geleert (null). Gibt true zurück wenn erfolgreich.
    /// </summary>
    public bool TryRemove(int slot, int count)
    {
        if (slot < 0 || slot >= HotbarSize)
            return false;

        if (_hotbar[slot] is null)
            return false;

        var stack = _hotbar[slot]!;
        int newCount = stack.Count - count;
        _hotbar[slot] = newCount > 0 ? stack with { Count = newCount } : null;
        return true;
    }

    /// <summary>Entfernt einen Block aus dem angegebenen Slot (Count-1, bei 0 → null).</summary>
    public bool TryRemove(int slot) => TryRemove(slot, 1);

    /// <summary>Setzt einen Slot direkt (für Startzustand / Tests).</summary>
    public void SetSlot(int slot, ItemStack? stack)
    {
        if (slot >= 0 && slot < HotbarSize)
            _hotbar[slot] = stack;
    }
}
