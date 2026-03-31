using Silk.NET.Input;
using VoxelEngine.Rendering;
using VoxelEngine.World;
using VoxelEngine.World.Inventories;

namespace VoxelEngine.Core.UI.Panels;

public sealed class InventoryPanel : IUIPanel, IDisposable
{
    public string Id => "inventory";
    public Key? ToggleKey { get; }

    private const float SlotSize = 40f;
    private const float Gap = 2f;
    private const float IconPad = 5f;
    private const float SectionGap = 8f;
    private const float EqSlotW = 44f;
    private const float EqSlotH = 44f;
    private const float EqGap = 4f;
    private const float PanelPadX = 16f;
    private const float PanelPadY = 16f;
    private const float CharH = 16f;
    private const float SmallCharW = 8f;
    private const float SmallCharH = 11f;

    private const int GridRows = InventoryGrid.Rows;
    private const int GridCols = InventoryGrid.Cols;
    private const int HotbarSlots = Inventory.HotbarSize;

    private float GridW => GridCols * SlotSize + (GridCols - 1) * Gap;
    private float GridH => GridRows * SlotSize + (GridRows - 1) * Gap;
    private float HotbarH => SlotSize;
    private float EqSectionW => EqSlotW + PanelPadX;
    private float EqSectionH => 4 * EqSlotH + 3 * EqGap;
    private float PanelW => PanelPadX + GridW + EqSectionW + PanelPadX;
    private float PanelH => PanelPadY + GridH + SectionGap + HotbarH + PanelPadY + CharH + 4f;

    private readonly TextRenderer _text;
    private readonly IconRenderer _icon;
    private ArrayTexture? _atlas;

    public ArrayTexture? Atlas { get => _atlas; set => _atlas = value; }

    private float _panelX;
    private float _panelY;

    public InventoryPanel(TextRenderer text, IconRenderer icon, Key toggleKey)
    {
        _text = text;
        _icon = icon;
        ToggleKey = toggleKey;
    }

    public void OnOpen(GameContext ctx)
    {
        if (ctx.Inventory.Drag is not null)
            ctx.Inventory.CancelDrag();
    }

    public void OnClose(GameContext ctx)
    {
        if (ctx.Inventory.Drag is not null)
            ctx.Inventory.CancelDrag();
    }

    public void Update(GameContext ctx)
    {
        var inv = ctx.Inventory;
        var (mx, my) = ctx.Input.MousePosition;
        bool shiftHeld = ctx.Input.IsKeyPressed(ctx.KeyBindings.Sneak);
        int clicks = ctx.Input.ConsumeLeftClicks();

        if (clicks <= 0)
            return;

        var hit = HitTestAll(mx, my);
        if (hit.HasValue)
        {
            if (shiftHeld && inv.Drag is null)
                inv.ShiftClick(hit.Value);
            else if (inv.Drag is null)
                inv.BeginDrag(hit.Value);
            else
                inv.Drop(hit.Value);
        }
        else if (inv.Drag is not null)
        {
            inv.CancelDrag();
        }
    }

    public void Render(GameContext ctx, double frameTime, int screenW, int screenH)
    {
        var inv = ctx.Inventory;
        var (mx, my) = ctx.Input.MousePosition;

        _panelX = (screenW - PanelW) / 2f;
        _panelY = (screenH - PanelH) / 2f;

        float gridX = _panelX + PanelPadX;
        float gridY = _panelY + PanelPadY;
        float hotbarY = gridY + GridH + SectionGap;
        float eqX = gridX + GridW + PanelPadX;
        float eqY = gridY + (GridH - EqSectionH) / 2f;

        _text.BeginFrame(screenW, screenH);
        _text.DrawQuad(0, 0, screenW, screenH, r: 0f, g: 0f, b: 0f, a: 0.45f);
        _text.DrawQuad(_panelX, _panelY, PanelW, PanelH, r: 0.10f, g: 0.10f, b: 0.10f, a: 0.93f);

        for (int i = 0; i < InventoryGrid.TotalSlots; i++)
        {
            var (sx, sy) = GridSlotPos(i, gridX, gridY);
            DrawSlotBackground(sx, sy, SlotSize, inv.Grid.Get(i) is not null);
        }

        for (int i = 0; i < HotbarSlots; i++)
        {
            var (sx, sy) = HotbarSlotPos(i, gridX, hotbarY);
            bool isSelected = i == inv.Hotbar.SelectedSlot;
            DrawSlotBackground(sx, sy, SlotSize, inv.Hotbar.Hotbar[i] is not null, isSelected);
        }

        var eqLabels = new[] { "Helm", "Brust", "Beine", "Schuh" };
        for (int i = 0; i < 4; i++)
        {
            float sx = eqX;
            float sy = eqY + i * (EqSlotH + EqGap);
            var eq = inv.Equipment.Get((EquipmentSlotType)i);
            DrawSlotBackground(sx, sy, EqSlotW, eq is not null);
            _text.DrawText(eqLabels[i], sx + EqSlotW + 4f, sy + (EqSlotH - SmallCharH) / 2f,
                r: 0.55f, g: 0.55f, b: 0.55f, charWidth: SmallCharW, charHeight: SmallCharH);
        }

        float sepY = hotbarY - SectionGap * 0.5f - 1f;
        _text.DrawQuad(gridX, sepY, GridW, 1f, r: 0.35f, g: 0.35f, b: 0.35f, a: 0.7f);
        _text.DrawText("Inventar", _panelX + PanelPadX, _panelY + _panelY - 26f, r: 0.85f, g: 0.85f, b: 0.85f);
        _text.EndFrame();

        if (_atlas is not null)
        {
            _icon.BeginFrame(screenW, screenH, _atlas);

            for (int i = 0; i < InventoryGrid.TotalSlots; i++)
            {
                if (inv.Grid.Get(i) is not { } stack) continue;
                var (sx, sy) = GridSlotPos(i, gridX, gridY);
                _icon.DrawIcon(sx + IconPad, sy + IconPad, SlotSize - 2 * IconPad, BlockRegistry.Get(stack.BlockType).TopTextureIndex);
            }

            for (int i = 0; i < HotbarSlots; i++)
            {
                if (inv.Hotbar.Hotbar[i] is not { } stack) continue;
                var (sx, sy) = HotbarSlotPos(i, gridX, hotbarY);
                _icon.DrawIcon(sx + IconPad, sy + IconPad, SlotSize - 2 * IconPad, BlockRegistry.Get(stack.BlockType).TopTextureIndex);
            }

            for (int i = 0; i < 4; i++)
            {
                if (inv.Equipment.Get((EquipmentSlotType)i) is not { } stack) continue;
                float sx = eqX;
                float sy = eqY + i * (EqSlotH + EqGap);
                _icon.DrawIcon(sx + IconPad, sy + IconPad, EqSlotW - 2 * IconPad, BlockRegistry.Get(stack.BlockType).TopTextureIndex);
            }

            if (inv.Drag is { } drag)
            {
                float dragSize = SlotSize - 2 * IconPad;
                _icon.DrawIcon(mx - dragSize / 2f, my - dragSize / 2f, dragSize, BlockRegistry.Get(drag.Item.BlockType).TopTextureIndex);
            }

            _icon.EndFrame();
        }

        _text.BeginFrame(screenW, screenH);
        for (int i = 0; i < InventoryGrid.TotalSlots; i++)
        {
            if (inv.Grid.Get(i) is not { Count: > 1 } stack) continue;
            var (sx, sy) = GridSlotPos(i, gridX, gridY);
            DrawCount(stack.Count, sx, sy, SlotSize);
        }

        for (int i = 0; i < HotbarSlots; i++)
        {
            if (inv.Hotbar.Hotbar[i] is not { Count: > 1 } stack) continue;
            var (sx, sy) = HotbarSlotPos(i, gridX, hotbarY);
            DrawCount(stack.Count, sx, sy, SlotSize);
        }

        if (inv.Drag is { } d && d.Item.Count > 1)
        {
            float dragSize = SlotSize - 2 * IconPad;
            DrawCount(d.Item.Count, mx - dragSize / 2f, my - dragSize / 2f, dragSize);
        }

        _text.EndFrame();
    }

    public void Dispose()
    {
        _text.Dispose();
        _icon.Dispose();
        GC.SuppressFinalize(this);
    }

    private void DrawSlotBackground(float x, float y, float size, bool occupied, bool selected = false)
    {
        float r = selected ? 0.85f : (occupied ? 0.16f : 0.13f);
        float g = selected ? 0.85f : (occupied ? 0.16f : 0.13f);
        float b = selected ? 0.20f : (occupied ? 0.16f : 0.13f);
        _text.DrawQuad(x, y, size, size, r: r, g: g, b: b, a: 0.88f);
        float br = selected ? 0.9f : 0.28f;
        float bg = selected ? 0.9f : 0.28f;
        float bb = selected ? 0.3f : 0.28f;
        _text.DrawQuad(x, y, size, 1f, r: br, g: bg, b: bb, a: 1f);
        _text.DrawQuad(x, y + size - 1, size, 1f, r: br, g: bg, b: bb, a: 1f);
        _text.DrawQuad(x, y, 1f, size, r: br, g: bg, b: bb, a: 1f);
        _text.DrawQuad(x + size - 1, y, 1f, size, r: br, g: bg, b: bb, a: 1f);
    }

    private void DrawCount(int count, float slotX, float slotY, float slotSize)
    {
        string s = count.ToString();
        float x = slotX + slotSize - s.Length * SmallCharW - 2f;
        float y = slotY + slotSize - SmallCharH - 2f;
        _text.DrawText(s, x + 1f, y + 1f, r: 0f, g: 0f, b: 0f, a: 0.8f, charWidth: SmallCharW, charHeight: SmallCharH);
        _text.DrawText(s, x, y, r: 1f, g: 1f, b: 1f, charWidth: SmallCharW, charHeight: SmallCharH);
    }

    private static (float x, float y) GridSlotPos(int index, float originX, float originY)
    {
        int row = index / GridCols;
        int col = index % GridCols;
        return (originX + col * (SlotSize + Gap), originY + row * (SlotSize + Gap));
    }

    private static (float x, float y) HotbarSlotPos(int index, float originX, float originY) =>
        (originX + index * (SlotSize + Gap), originY);

    private SlotAddress? HitTestAll(float mx, float my)
    {
        float gridX = _panelX + PanelPadX;
        float gridY = _panelY + PanelPadY;
        float hotbarY = gridY + GridH + SectionGap;
        float eqX = gridX + GridW + PanelPadX;
        float eqY = gridY + (GridH - (4 * EqSlotH + 3 * EqGap)) / 2f;

        for (int i = 0; i < InventoryGrid.TotalSlots; i++)
        {
            var (sx, sy) = GridSlotPos(i, gridX, gridY);
            if (InSlot(mx, my, sx, sy, SlotSize))
                return SlotAddress.Grid(i);
        }

        for (int i = 0; i < HotbarSlots; i++)
        {
            var (sx, sy) = HotbarSlotPos(i, gridX, hotbarY);
            if (InSlot(mx, my, sx, sy, SlotSize))
                return SlotAddress.Hotbar(i);
        }

        for (int i = 0; i < 4; i++)
        {
            float sx = eqX;
            float sy = eqY + i * (EqSlotH + EqGap);
            if (InSlot(mx, my, sx, sy, EqSlotW, EqSlotH))
                return SlotAddress.Equipment(i);
        }

        return null;
    }

    private static bool InSlot(float mx, float my, float sx, float sy, float w, float h = -1)
    {
        if (h < 0) h = w;
        return mx >= sx && mx <= sx + w && my >= sy && my <= sy + h;
    }
}
