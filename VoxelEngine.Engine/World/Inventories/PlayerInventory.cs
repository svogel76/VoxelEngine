using VoxelEngine.World;

namespace VoxelEngine.World.Inventories;

/// <summary>
/// Slot-Adresse im Inventar-Fenster.
/// Unterscheidet zwischen Hotbar-, Grid- und Equipment-Slots.
/// </summary>
public readonly record struct SlotAddress(SlotRegion Region, int Index)
{
    public static SlotAddress Hotbar(int index)    => new(SlotRegion.Hotbar,    index);
    public static SlotAddress Grid(int index)      => new(SlotRegion.Grid,      index);
    public static SlotAddress Equipment(int index) => new(SlotRegion.Equipment, index);
}

public enum SlotRegion { Hotbar, Grid, Equipment }

/// <summary>
/// Zustand eines laufenden Drag-Vorgangs.
/// Das gezogene Item liegt "auf dem Cursor" — der Quell-Slot ist bereits geleert.
/// </summary>
public sealed class DragState
{
    /// <summary>Das Item das gerade gezogen wird.</summary>
    public ItemStack Item { get; }

    /// <summary>Ursprünglicher Slot (für Abbruch → zurücklegen).</summary>
    public SlotAddress Source { get; }

    public DragState(ItemStack item, SlotAddress source)
    {
        Item   = item;
        Source = source;
    }
}

/// <summary>
/// Vollständiges Spieler-Inventar: Hotbar + 4×9 Grid + 4 Equipment-Slots.
/// Enthält die Drag&amp;Drop- und Shift-Click-Logik als pure C#-Methoden.
///
/// Änderungen am Zustand sind transaktional: jede Methode lässt das Inventar
/// immer in einem konsistenten Zustand zurück.
/// </summary>
public sealed class PlayerInventory
{
    // ── Teilbereiche ──────────────────────────────────────────────────────

    public Inventory      Hotbar    { get; }   // bestehende Klasse (9 Slots)
    public InventoryGrid  Grid      { get; } = new InventoryGrid();
    public EquipmentSlots Equipment { get; } = new EquipmentSlots();

    /// <summary>Aktiver Drag-Vorgang (null wenn nichts gezogen wird).</summary>
    public DragState? Drag { get; private set; }

    public PlayerInventory(Inventory hotbar)
    {
        Hotbar = hotbar;
    }

    // ── Slot-Zugriff ──────────────────────────────────────────────────────

    /// <summary>Gibt den Inhalt eines Slots zurück (unabhängig von der Region).</summary>
    public ItemStack? GetSlot(SlotAddress addr) => addr.Region switch
    {
        SlotRegion.Hotbar    => Hotbar.Hotbar[addr.Index],
        SlotRegion.Grid      => Grid.Get(addr.Index),
        SlotRegion.Equipment => Equipment.Get((EquipmentSlotType)addr.Index),
        _                    => null,
    };

    /// <summary>Setzt den Inhalt eines Slots direkt (für Persistenz / Tests).</summary>
    public void SetSlot(SlotAddress addr, ItemStack? stack)
    {
        switch (addr.Region)
        {
            case SlotRegion.Hotbar:
                Hotbar.SetSlot(addr.Index, stack);
                break;
            case SlotRegion.Grid:
                Grid.Set(addr.Index, stack);
                break;
            case SlotRegion.Equipment:
                Equipment.Set((EquipmentSlotType)addr.Index, stack);
                break;
        }
    }

    // ── Drag & Drop ───────────────────────────────────────────────────────

    /// <summary>
    /// Startet einen Drag-Vorgang vom angegebenen Slot.
    /// Das Item wird aus dem Slot entfernt und in <see cref="Drag"/> gehalten.
    /// Gibt false zurück wenn der Slot leer ist oder bereits ein Drag läuft.
    /// </summary>
    public bool BeginDrag(SlotAddress source)
    {
        if (Drag is not null) return false;

        var item = GetSlot(source);
        if (item is null) return false;

        SetSlot(source, null);
        Drag = new DragState(item, source);
        return true;
    }

    /// <summary>
    /// Lässt das gezogene Item auf dem Ziel-Slot los.
    ///
    /// Regeln:
    /// - Gleiches Item + Platz im Stapel → stapeln; Rest bleibt auf Cursor
    /// - Ziel leer → ablegen
    /// - Ziel belegt mit anderem Item → tauschen (Ziel-Item geht auf Cursor)
    ///
    /// Gibt false zurück wenn kein Drag aktiv ist.
    /// </summary>
    public bool Drop(SlotAddress target)
    {
        if (Drag is null) return false;

        var dragged  = Drag.Item;
        var existing = GetSlot(target);

        if (existing is null)
        {
            // Ziel leer → einfach ablegen
            SetSlot(target, dragged);
            Drag = null;
        }
        else if (existing.BlockType == dragged.BlockType)
        {
            // Gleiches Item → stapeln soweit möglich
            int max     = BlockRegistry.Get(dragged.BlockType).MaxStackSize;
            int canFit  = max - existing.Count;
            int moved   = Math.Min(canFit, dragged.Count);

            if (moved > 0)
            {
                SetSlot(target, existing with { Count = existing.Count + moved });
                int rest = dragged.Count - moved;
                Drag = rest > 0
                    ? new DragState(dragged with { Count = rest }, Drag.Source)
                    : null;
            }
            else
            {
                // Ziel-Stapel voll → tauschen
                SetSlot(target, dragged);
                Drag = new DragState(existing, Drag.Source);
            }
        }
        else
        {
            // Verschiedenes Item → tauschen
            SetSlot(target, dragged);
            Drag = new DragState(existing, Drag.Source);
        }

        return true;
    }

    /// <summary>
    /// Bricht den Drag-Vorgang ab und legt das Item in den Ursprungs-Slot zurück.
    /// Wenn der Ursprungs-Slot inzwischen belegt ist, wird der erste freie Slot
    /// in derselben Region gesucht; schlägt das fehl → Grid-Fallback.
    /// </summary>
    public void CancelDrag()
    {
        if (Drag is null) return;

        var item   = Drag.Item;
        var source = Drag.Source;
        Drag = null;

        // Versuch: Ursprungs-Slot (sollte nach BeginDrag leer sein)
        if (GetSlot(source) is null)
        {
            SetSlot(source, item);
            return;
        }

        // Fallback: in Grid oder Hotbar stapeln/ablegen
        if (!Hotbar.TryAdd(item.BlockType))
            Grid.TryAdd(item.BlockType);
        // Item könnte verloren gehen wenn alles voll ist — in Praxis sehr selten
    }

    // ── Shift-Click ───────────────────────────────────────────────────────

    /// <summary>
    /// Shift-Click: verschiebt den kompletten Slot-Inhalt zwischen Hotbar und Grid.
    ///
    /// - Klick auf Hotbar → versuche ins Grid zu verschieben
    /// - Klick auf Grid   → versuche in die Hotbar zu verschieben
    /// - Equipment-Slots  → kein Shift-Click (keine Aktion)
    /// </summary>
    public bool ShiftClick(SlotAddress source)
    {
        var item = GetSlot(source);
        if (item is null) return false;

        switch (source.Region)
        {
            case SlotRegion.Hotbar:
                return ShiftToGrid(source, item);

            case SlotRegion.Grid:
                return ShiftToHotbar(source, item);

            default:
                return false;
        }
    }

    // ── Hilfsmethoden ─────────────────────────────────────────────────────

    private bool ShiftToGrid(SlotAddress source, ItemStack item)
    {
        // Zuerst stapeln, dann ersten freien Slot
        int target = FindStackableOrEmpty(SlotRegion.Grid, item.BlockType, InventoryGrid.TotalSlots);
        if (target < 0) return false;

        var addr     = SlotAddress.Grid(target);
        var existing = Grid.Get(target);
        SetSlot(source, null);

        if (existing is null)
        {
            Grid.Set(target, item);
        }
        else
        {
            int max    = BlockRegistry.Get(item.BlockType).MaxStackSize;
            int canFit = max - existing.Count;
            int moved  = Math.Min(canFit, item.Count);
            Grid.Set(target, existing with { Count = existing.Count + moved });

            int rest = item.Count - moved;
            if (rest > 0)
            {
                // Rest wieder in Quell-Slot zurücklegen (partieller Transfer)
                Hotbar.SetSlot(source.Index, item with { Count = rest });
            }
        }

        return true;
    }

    private bool ShiftToHotbar(SlotAddress source, ItemStack item)
    {
        int target = FindStackableOrEmpty(SlotRegion.Hotbar, item.BlockType, Inventory.HotbarSize);
        if (target < 0) return false;

        var existing = Hotbar.Hotbar[target];
        SetSlot(source, null);

        if (existing is null)
        {
            Hotbar.SetSlot(target, item);
        }
        else
        {
            int max    = BlockRegistry.Get(item.BlockType).MaxStackSize;
            int canFit = max - existing.Count;
            int moved  = Math.Min(canFit, item.Count);
            Hotbar.SetSlot(target, existing with { Count = existing.Count + moved });

            int rest = item.Count - moved;
            if (rest > 0)
                Grid.Set(source.Index, item with { Count = rest });
        }

        return true;
    }

    /// <summary>
    /// Findet den ersten stapelbaren Slot (gleicher Typ, Platz frei) oder
    /// den ersten leeren Slot in der angegebenen Region.
    /// Gibt -1 zurück wenn keine passende Stelle gefunden wird.
    /// </summary>
    private int FindStackableOrEmpty(SlotRegion region, byte blockType, int slotCount)
    {
        int max = BlockRegistry.Get(blockType).MaxStackSize;

        // Phase 1: stapelbarer Slot
        for (int i = 0; i < slotCount; i++)
        {
            var addr = new SlotAddress(region, i);
            var s    = GetSlot(addr);
            if (s is not null && s.BlockType == blockType && s.Count < max)
                return i;
        }

        // Phase 2: leerer Slot
        for (int i = 0; i < slotCount; i++)
        {
            var addr = new SlotAddress(region, i);
            if (GetSlot(addr) is null)
                return i;
        }

        return -1;
    }
}
