using VoxelEngine.World;
using VoxelEngine.World.Inventories;
using Xunit;

namespace VoxelEngine.Tests.Inventories;

public class PlayerInventoryTests
{
    // ── Hilfsmethoden ─────────────────────────────────────────────────────

    private static PlayerInventory MakeInventory()
    {
        var hotbar = new Inventory();
        return new PlayerInventory(hotbar);
    }

    // ── Grid ──────────────────────────────────────────────────────────────

    [Fact]
    public void Grid_TryAdd_AddsToFirstEmptySlot()
    {
        var inv = MakeInventory();
        bool ok = inv.Grid.TryAdd(BlockType.Grass);
        Assert.True(ok);
        Assert.NotNull(inv.Grid.Get(0));
        Assert.Equal(BlockType.Grass, inv.Grid.Get(0)!.BlockType);
    }

    [Fact]
    public void Grid_TryAdd_Stacks()
    {
        var inv = MakeInventory();
        inv.Grid.TryAdd(BlockType.Stone);
        inv.Grid.TryAdd(BlockType.Stone);
        Assert.Equal(2, inv.Grid.Get(0)!.Count);
        Assert.Null(inv.Grid.Get(1));
    }

    [Fact]
    public void Grid_TryRemove_DecrementsCount()
    {
        var inv = MakeInventory();
        inv.Grid.TryAdd(BlockType.Dirt);
        inv.Grid.TryAdd(BlockType.Dirt);   // Count = 2
        inv.Grid.TryRemove(0, 1);
        Assert.Equal(1, inv.Grid.Get(0)!.Count);
    }

    [Fact]
    public void Grid_TryRemove_ClearsSlotAtZero()
    {
        var inv = MakeInventory();
        inv.Grid.TryAdd(BlockType.Stone);
        inv.Grid.TryRemove(0, 1);
        Assert.Null(inv.Grid.Get(0));
    }

    [Fact]
    public void Grid_TryRemove_EmptySlot_ReturnsFalse()
    {
        var inv = MakeInventory();
        Assert.False(inv.Grid.TryRemove(0));
    }

    [Fact]
    public void Grid_IndexOutOfRange_Throws()
    {
        var inv = MakeInventory();
        Assert.Throws<ArgumentOutOfRangeException>(() => inv.Grid.Get(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => inv.Grid.Get(InventoryGrid.TotalSlots));
    }

    // ── Equipment ─────────────────────────────────────────────────────────

    [Fact]
    public void Equipment_Equip_SetsSlot()
    {
        var inv  = MakeInventory();
        var item = new ItemStack(BlockType.Grass, 1);
        inv.Equipment.Equip(EquipmentSlotType.Helmet, item);
        Assert.Equal(item, inv.Equipment.Get(EquipmentSlotType.Helmet));
    }

    [Fact]
    public void Equipment_Equip_ReturnsOldItem()
    {
        var inv   = MakeInventory();
        var item1 = new ItemStack(BlockType.Grass, 1);
        var item2 = new ItemStack(BlockType.Stone, 1);
        inv.Equipment.Equip(EquipmentSlotType.Helmet, item1);
        var prev = inv.Equipment.Equip(EquipmentSlotType.Helmet, item2);
        Assert.Equal(item1, prev);
    }

    [Fact]
    public void Equipment_Unequip_ReturnsItem_AndClearsSlot()
    {
        var inv  = MakeInventory();
        var item = new ItemStack(BlockType.Dirt, 1);
        inv.Equipment.Equip(EquipmentSlotType.Boots, item);
        var removed = inv.Equipment.Unequip(EquipmentSlotType.Boots);
        Assert.Equal(item, removed);
        Assert.Null(inv.Equipment.Get(EquipmentSlotType.Boots));
    }

    [Fact]
    public void Equipment_AllSlotsInitiallyEmpty()
    {
        var inv = MakeInventory();
        foreach (EquipmentSlotType slot in Enum.GetValues<EquipmentSlotType>())
            Assert.Null(inv.Equipment.Get(slot));
    }

    // ── Drag & Drop ───────────────────────────────────────────────────────

    [Fact]
    public void BeginDrag_RemovesItemFromSlot()
    {
        var inv = MakeInventory();
        inv.Grid.Set(0, new ItemStack(BlockType.Grass, 3));
        bool ok = inv.BeginDrag(SlotAddress.Grid(0));
        Assert.True(ok);
        Assert.Null(inv.Grid.Get(0));
        Assert.NotNull(inv.Drag);
        Assert.Equal(BlockType.Grass, inv.Drag!.Item.BlockType);
        Assert.Equal(3, inv.Drag.Item.Count);
    }

    [Fact]
    public void BeginDrag_EmptySlot_ReturnsFalse()
    {
        var inv = MakeInventory();
        bool ok = inv.BeginDrag(SlotAddress.Grid(0));
        Assert.False(ok);
        Assert.Null(inv.Drag);
    }

    [Fact]
    public void BeginDrag_WhenAlreadyDragging_ReturnsFalse()
    {
        var inv = MakeInventory();
        inv.Grid.Set(0, new ItemStack(BlockType.Grass, 1));
        inv.Grid.Set(1, new ItemStack(BlockType.Stone, 1));
        inv.BeginDrag(SlotAddress.Grid(0));
        bool ok = inv.BeginDrag(SlotAddress.Grid(1));
        Assert.False(ok);
        // Ursprünglicher Drag bleibt erhalten
        Assert.Equal(BlockType.Grass, inv.Drag!.Item.BlockType);
    }

    [Fact]
    public void Drop_OnEmptySlot_PlacesItem()
    {
        var inv = MakeInventory();
        inv.Grid.Set(0, new ItemStack(BlockType.Stone, 2));
        inv.BeginDrag(SlotAddress.Grid(0));
        inv.Drop(SlotAddress.Grid(5));
        Assert.Null(inv.Grid.Get(0));
        Assert.Equal(BlockType.Stone, inv.Grid.Get(5)!.BlockType);
        Assert.Equal(2, inv.Grid.Get(5)!.Count);
        Assert.Null(inv.Drag);
    }

    [Fact]
    public void Drop_OnOccupiedSlot_SameType_Stacks()
    {
        var inv = MakeInventory();
        inv.Grid.Set(0, new ItemStack(BlockType.Dirt, 2));
        inv.Grid.Set(1, new ItemStack(BlockType.Dirt, 1));
        inv.BeginDrag(SlotAddress.Grid(0));
        inv.Drop(SlotAddress.Grid(1));
        Assert.Equal(3, inv.Grid.Get(1)!.Count);
        Assert.Null(inv.Drag);
    }

    [Fact]
    public void Drop_OnOccupiedSlot_DifferentType_Swaps()
    {
        var inv = MakeInventory();
        inv.Grid.Set(0, new ItemStack(BlockType.Grass, 1));
        inv.Grid.Set(1, new ItemStack(BlockType.Stone, 1));
        inv.BeginDrag(SlotAddress.Grid(0));
        inv.Drop(SlotAddress.Grid(1));
        // Slot 1 hat jetzt Grass, Drag hält Stone
        Assert.Equal(BlockType.Grass, inv.Grid.Get(1)!.BlockType);
        Assert.NotNull(inv.Drag);
        Assert.Equal(BlockType.Stone, inv.Drag!.Item.BlockType);
    }

    [Fact]
    public void Drop_OnFullSameTypeSlot_Swaps()
    {
        var inv = MakeInventory();
        int max = BlockRegistry.Get(BlockType.Grass).MaxStackSize;
        inv.Grid.Set(0, new ItemStack(BlockType.Grass, 1));      // gezogen
        inv.Grid.Set(1, new ItemStack(BlockType.Grass, max));    // Ziel: voll
        inv.BeginDrag(SlotAddress.Grid(0));
        inv.Drop(SlotAddress.Grid(1));
        // Ziel war voll → gezogenes Item (Count=1) landet in Slot 1,
        // Slot-1-Inhalt (Count=max) geht auf den Cursor
        Assert.Equal(1,   inv.Grid.Get(1)!.Count);
        Assert.NotNull(inv.Drag);
        Assert.Equal(max, inv.Drag!.Item.Count);
    }

    [Fact]
    public void Drop_PartialStack_LeavesRemainderOnCursor()
    {
        var inv = MakeInventory();
        int max = BlockRegistry.Get(BlockType.Stone).MaxStackSize;
        inv.Grid.Set(0, new ItemStack(BlockType.Stone, max - 1));
        inv.Grid.Set(1, new ItemStack(BlockType.Stone, 3));
        inv.BeginDrag(SlotAddress.Grid(1));   // 3 auf Cursor
        inv.Drop(SlotAddress.Grid(0));         // max-1 in Ziel, kann 1 aufnehmen
        Assert.Equal(max, inv.Grid.Get(0)!.Count);
        Assert.NotNull(inv.Drag);
        Assert.Equal(2, inv.Drag!.Item.Count);
    }

    [Fact]
    public void CancelDrag_ReturnsItemToSourceSlot()
    {
        var inv = MakeInventory();
        inv.Grid.Set(0, new ItemStack(BlockType.Grass, 2));
        inv.BeginDrag(SlotAddress.Grid(0));
        inv.CancelDrag();
        Assert.Null(inv.Drag);
        Assert.Equal(BlockType.Grass, inv.Grid.Get(0)!.BlockType);
        Assert.Equal(2, inv.Grid.Get(0)!.Count);
    }

    [Fact]
    public void Drop_NoDragActive_ReturnsFalse()
    {
        var inv = MakeInventory();
        bool ok = inv.Drop(SlotAddress.Grid(0));
        Assert.False(ok);
    }

    // ── Drag zwischen Regionen ────────────────────────────────────────────

    [Fact]
    public void Drop_GridToHotbar_Works()
    {
        var inv = MakeInventory();
        inv.Grid.Set(0, new ItemStack(BlockType.Sand, 1));
        inv.BeginDrag(SlotAddress.Grid(0));
        inv.Drop(SlotAddress.Hotbar(0));
        Assert.Null(inv.Grid.Get(0));
        Assert.Equal(BlockType.Sand, inv.Hotbar.Hotbar[0]!.BlockType);
        Assert.Null(inv.Drag);
    }

    [Fact]
    public void Drop_HotbarToEquipment_Works()
    {
        var inv = MakeInventory();
        inv.Hotbar.SetSlot(0, new ItemStack(BlockType.Grass, 1));
        inv.BeginDrag(SlotAddress.Hotbar(0));
        inv.Drop(SlotAddress.Equipment((int)EquipmentSlotType.Helmet));
        Assert.Null(inv.Hotbar.Hotbar[0]);
        Assert.Equal(BlockType.Grass, inv.Equipment.Get(EquipmentSlotType.Helmet)!.BlockType);
    }

    // ── Shift-Click ───────────────────────────────────────────────────────

    [Fact]
    public void ShiftClick_GridToHotbar_MovesItem()
    {
        var inv = MakeInventory();
        inv.Grid.Set(0, new ItemStack(BlockType.Stone, 5));
        bool ok = inv.ShiftClick(SlotAddress.Grid(0));
        Assert.True(ok);
        Assert.Null(inv.Grid.Get(0));
        Assert.Equal(BlockType.Stone, inv.Hotbar.Hotbar[0]!.BlockType);
        Assert.Equal(5, inv.Hotbar.Hotbar[0]!.Count);
    }

    [Fact]
    public void ShiftClick_HotbarToGrid_MovesItem()
    {
        var inv = MakeInventory();
        inv.Hotbar.SetSlot(0, new ItemStack(BlockType.Grass, 3));
        bool ok = inv.ShiftClick(SlotAddress.Hotbar(0));
        Assert.True(ok);
        Assert.Null(inv.Hotbar.Hotbar[0]);
        Assert.Equal(BlockType.Grass, inv.Grid.Get(0)!.BlockType);
        Assert.Equal(3, inv.Grid.Get(0)!.Count);
    }

    [Fact]
    public void ShiftClick_HotbarToGrid_StacksWithExisting()
    {
        var inv = MakeInventory();
        inv.Grid.Set(0, new ItemStack(BlockType.Dirt, 2));
        inv.Hotbar.SetSlot(0, new ItemStack(BlockType.Dirt, 3));
        bool ok = inv.ShiftClick(SlotAddress.Hotbar(0));
        Assert.True(ok);
        Assert.Equal(5, inv.Grid.Get(0)!.Count);
        Assert.Null(inv.Hotbar.Hotbar[0]);
    }

    [Fact]
    public void ShiftClick_EmptySlot_ReturnsFalse()
    {
        var inv = MakeInventory();
        bool ok = inv.ShiftClick(SlotAddress.Grid(0));
        Assert.False(ok);
    }

    [Fact]
    public void ShiftClick_EquipmentSlot_ReturnsFalse()
    {
        var inv = MakeInventory();
        inv.Equipment.Equip(EquipmentSlotType.Helmet, new ItemStack(BlockType.Grass, 1));
        bool ok = inv.ShiftClick(SlotAddress.Equipment((int)EquipmentSlotType.Helmet));
        Assert.False(ok);
    }

    [Fact]
    public void ShiftClick_HotbarFull_ReturnsFalse()
    {
        var inv = MakeInventory();
        // Alle 9 Hotbar-Slots mit verschiedenen Items füllen
        byte[] types = [BlockType.Grass, BlockType.Dirt, BlockType.Stone, BlockType.Sand,
                        BlockType.Water, BlockType.Glass, BlockType.Ice, BlockType.Wood, BlockType.Leaves];
        for (int i = 0; i < Inventory.HotbarSize; i++)
            inv.Hotbar.SetSlot(i, new ItemStack(types[i], 1));

        inv.Grid.Set(0, new ItemStack(BlockType.DryGrass, 1));
        bool ok = inv.ShiftClick(SlotAddress.Grid(0));
        Assert.False(ok);
    }

    // ── SlotAddress ───────────────────────────────────────────────────────

    [Fact]
    public void GetSlot_ReturnsCorrectItem_ForAllRegions()
    {
        var inv   = MakeInventory();
        var grass = new ItemStack(BlockType.Grass, 1);
        var stone = new ItemStack(BlockType.Stone, 2);
        var dirt  = new ItemStack(BlockType.Dirt, 3);

        inv.Hotbar.SetSlot(3, grass);
        inv.Grid.Set(7, stone);
        inv.Equipment.Equip(EquipmentSlotType.Leggings, dirt);

        Assert.Equal(grass, inv.GetSlot(SlotAddress.Hotbar(3)));
        Assert.Equal(stone, inv.GetSlot(SlotAddress.Grid(7)));
        Assert.Equal(dirt,  inv.GetSlot(SlotAddress.Equipment((int)EquipmentSlotType.Leggings)));
    }

    [Fact]
    public void SetSlot_UpdatesCorrectRegion()
    {
        var inv  = MakeInventory();
        var item = new ItemStack(BlockType.Wood, 5);
        inv.SetSlot(SlotAddress.Grid(10), item);
        Assert.Equal(item, inv.Grid.Get(10));
    }
}
