using VoxelEngine.World;
using Xunit;

namespace VoxelEngine.Tests.World;

public class InventoryTests
{
    [Fact]
    public void NewInventory_AllSlotsEmpty()
    {
        var inv = new Inventory();
        Assert.All(inv.Hotbar, slot => Assert.Null(slot));
    }

    [Fact]
    public void TryAdd_AddsToFirstEmptySlot()
    {
        var inv = new Inventory();
        bool result = inv.TryAdd(BlockType.Grass);
        Assert.True(result);
        Assert.NotNull(inv.Hotbar[0]);
        Assert.Equal(BlockType.Grass, inv.Hotbar[0]!.BlockType);
        Assert.Equal(1, inv.Hotbar[0]!.Count);
    }

    [Fact]
    public void TryAdd_StacksWithExistingItems()
    {
        var inv = new Inventory();
        inv.TryAdd(BlockType.Grass);
        inv.TryAdd(BlockType.Grass);
        Assert.Equal(2, inv.Hotbar[0]!.Count);
    }

    [Fact]
    public void TryAdd_RespectsMaxStackSize()
    {
        var inv      = new Inventory();
        int maxStack = BlockRegistry.Get(BlockType.Grass).MaxStackSize;
        for (int i = 0; i < maxStack; i++)
            inv.TryAdd(BlockType.Grass);

        // Nächster Add muss in Slot 1 landen
        bool result = inv.TryAdd(BlockType.Grass);
        Assert.True(result);
        Assert.Equal(maxStack, inv.Hotbar[0]!.Count);
        Assert.NotNull(inv.Hotbar[1]);
        Assert.Equal(1, inv.Hotbar[1]!.Count);
    }

    [Fact]
    public void TryRemove_EmptiesSlot()
    {
        var inv = new Inventory();
        inv.TryAdd(BlockType.Stone);
        bool result = inv.TryRemove(0);
        Assert.True(result);
        Assert.Null(inv.Hotbar[0]);
    }

    [Fact]
    public void SelectNext_WrapsAround()
    {
        var inv = new Inventory();
        inv.SelectSlot(8);
        inv.SelectNext();
        Assert.Equal(0, inv.SelectedSlot);
    }

    [Fact]
    public void SelectPrevious_WrapsAround()
    {
        var inv = new Inventory();
        inv.SelectSlot(0);
        inv.SelectPrevious();
        Assert.Equal(8, inv.SelectedSlot);
    }

    [Fact]
    public void SelectedBlock_ReflectsSelectedSlot()
    {
        var inv = new Inventory();
        inv.TryAdd(BlockType.Grass); // geht in Slot 0
        inv.SelectSlot(0);
        Assert.Equal(BlockType.Grass, inv.Hotbar[inv.SelectedSlot]?.BlockType ?? BlockType.Air);
    }

    [Fact]
    public void TryAdd_StacksOnExistingSlot()
    {
        var inv = new Inventory();
        inv.TryAdd(BlockType.Stone);
        bool result = inv.TryAdd(BlockType.Stone);
        Assert.True(result);
        Assert.Equal(2, inv.Hotbar[0]!.Count);
        Assert.Null(inv.Hotbar[1]);
    }

    [Fact]
    public void TryAdd_UsesNextFreeSlot()
    {
        var inv = new Inventory();
        inv.TryAdd(BlockType.Grass);
        bool result = inv.TryAdd(BlockType.Stone);
        Assert.True(result);
        Assert.Equal(BlockType.Grass, inv.Hotbar[0]!.BlockType);
        Assert.Equal(BlockType.Stone, inv.Hotbar[1]!.BlockType);
    }

    [Fact]
    public void TryAdd_InventoryFull_ReturnsFalse()
    {
        var inv = new Inventory();
        // 9 verschiedene Block-Typen belegen alle 9 Slots
        byte[] blocks = [BlockType.Grass, BlockType.Dirt, BlockType.Stone, BlockType.Sand,
                         BlockType.Water, BlockType.Glass, BlockType.Ice, BlockType.Wood, BlockType.Leaves];
        foreach (byte b in blocks)
            inv.TryAdd(b);
        // Alle Slots belegt — ein weiterer anderer Typ muss scheitern
        bool result = inv.TryAdd(BlockType.DryGrass);
        Assert.False(result);
    }

    [Fact]
    public void TryRemove_DecrementsCount()
    {
        var inv = new Inventory();
        inv.TryAdd(BlockType.Dirt);
        inv.TryAdd(BlockType.Dirt); // Count = 2
        bool result = inv.TryRemove(0, 1);
        Assert.True(result);
        Assert.NotNull(inv.Hotbar[0]);
        Assert.Equal(1, inv.Hotbar[0]!.Count);
    }

    [Fact]
    public void TryRemove_ClearsSlotAtZero()
    {
        var inv = new Inventory();
        inv.TryAdd(BlockType.Stone); // Count = 1
        bool result = inv.TryRemove(0, 1);
        Assert.True(result);
        Assert.Null(inv.Hotbar[0]);
    }

    [Fact]
    public void TryRemove_EmptySlot_ReturnsFalse()
    {
        var inv = new Inventory();
        bool result = inv.TryRemove(0, 1);
        Assert.False(result);
    }
}
