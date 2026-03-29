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
}
