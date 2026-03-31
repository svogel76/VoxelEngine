using VoxelEngine.Core;
using VoxelEngine.Core.Hud;
using VoxelEngine.World;

namespace VoxelEngine.Rendering.Hud;

/// <summary>HUD-Element das den Zustand der Spieler-Hotbar spiegelt.</summary>
public sealed class HotbarHudElement : IHudElement
{
    public string Id => "hotbar";
    public bool   Visible { get; set; } = true;

    private HudElementConfig _config = new(
        "hotbar", HudAnchor.BottomCenter,
        OffsetX: 0f, OffsetY: 0.02f,
        Scale: 1f, Visible: true, ZOrder: 20);

    public HudElementConfig Config => _config;

    // Snapshot aus Player.Inventory (wird pro Frame in Update() aktualisiert)
    public ItemStack?[] Slots       { get; } = new ItemStack?[Inventory.HotbarSize];
    public int          SelectedSlot { get; private set; }

    public void ApplyConfig(HudElementConfig config) => _config = config;

    public void Update(GameContext ctx)
    {
        var hotbar = ctx.Player.Inventory.Hotbar;
        for (int i = 0; i < Inventory.HotbarSize; i++)
            Slots[i] = hotbar[i];
        SelectedSlot = ctx.Player.Inventory.SelectedSlot;
    }
}
