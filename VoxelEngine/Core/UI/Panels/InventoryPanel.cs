using Silk.NET.Input;
using VoxelEngine.Rendering;
using VoxelEngine.World;
using VoxelEngine.World.Inventories;

namespace VoxelEngine.Core.UI.Panels;

/// <summary>
/// Inventar-Fenster — öffnet/schließt sich mit Taste E.
///
/// Bereiche:
///   4×9 Haupt-Inventar   (oben, 36 Slots)
///   Hotbar               (unten, 9 Slots — gleiche Daten wie HotbarHudElement)
///   4 Ausrüstungs-Slots  (rechts, vertikal stapelbar)
///
/// Drag &amp; Drop:
///   Linksklick auf Slot  → BeginDrag (Item folgt Mauszeiger)
///   Linksklick auf Slot  → Drop (ablegen / tauschen / stapeln)
///   Linksklick außerhalb → CancelDrag
///
/// Shift-Click:
///   Shift+Linksklick  → ShiftClick → verschiebt zwischen Hotbar und Grid
///
/// Architektur:
///   Keine direkten new-Instanzen auf Engine-Interna.
///   TextRenderer und IconRenderer werden per Konstruktor injiziert.
///   ArrayTexture wird per Property injiziert.
/// </summary>
public sealed class InventoryPanel : IUIPanel, IDisposable
{
    // ── IUIPanel ──────────────────────────────────────────────────────────

    public string Id        => "inventory";
    public Key?   ToggleKey => Key.E;

    // ── Layout ────────────────────────────────────────────────────────────

    private const float SlotSize    = 40f;
    private const float SlotPad     = 4f;
    private const float Gap         = 2f;
    private const float IconPad     = 5f;
    private const float SectionGap  = 8f;   // Abstand Grid ↔ Hotbar-Bereich
    private const float EqSlotW     = 44f;
    private const float EqSlotH     = 44f;
    private const float EqGap       = 4f;
    private const float PanelPadX   = 16f;
    private const float PanelPadY   = 16f;
    private const float CharW       = 12f;
    private const float CharH       = 16f;
    private const float SmallCharW  = 8f;
    private const float SmallCharH  = 11f;

    // Hotbar-Zeile ist unten im Panel, Grid darüber
    private const int   GridRows    = InventoryGrid.Rows;
    private const int   GridCols    = InventoryGrid.Cols;
    private const int   HotbarSlots = Inventory.HotbarSize;

    // Gesamtbreite: Grid (9 Slots + Gaps) + EqSektion (Padding + 1 Spalte)
    private float GridW      => GridCols * SlotSize + (GridCols - 1) * Gap;
    private float HotbarW    => HotbarSlots * SlotSize + (HotbarSlots - 1) * Gap;
    private float GridH      => GridRows * SlotSize + (GridRows - 1) * Gap;
    private float HotbarH    => SlotSize;
    private float EqSectionW => EqSlotW + PanelPadX;
    private float EqSectionH => 4 * EqSlotH + 3 * EqGap;
    private float PanelW     => PanelPadX + GridW + EqSectionW + PanelPadX;
    private float PanelH     => PanelPadY + GridH + SectionGap + HotbarH + PanelPadY + CharH + 4f;

    // ── Abhängigkeiten (injiziert) ────────────────────────────────────────

    private readonly TextRenderer _text;
    private readonly IconRenderer _icon;
    private ArrayTexture?         _atlas;

    /// <summary>
    /// Block-ArrayTexture für Icons. Nach GL-Init per Property setzen.
    /// </summary>
    public ArrayTexture? Atlas { get => _atlas; set => _atlas = value; }

    // ── Zustand ───────────────────────────────────────────────────────────

    private int _lastScreenW;
    private int _lastScreenH;

    // Zuletzt berechnete Panel-Position (aus Render, für Update-HitTest)
    private float _panelX;
    private float _panelY;

    // ── Konstruktor ───────────────────────────────────────────────────────

    /// <param name="text">TextRenderer-Instanz (injiziert — keine new hier)</param>
    /// <param name="icon">IconRenderer-Instanz (injiziert — keine new hier)</param>
    public InventoryPanel(TextRenderer text, IconRenderer icon)
    {
        _text = text;
        _icon = icon;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────

    public void OnOpen(GameContext ctx)
    {
        // Sicherstellen: kein Drag offen bei erneutem Öffnen
        if (ctx.Inventory.Drag is not null)
            ctx.Inventory.CancelDrag();
    }

    public void OnClose(GameContext ctx)
    {
        // Drag abbrechen wenn Inventar geschlossen wird
        if (ctx.Inventory.Drag is not null)
            ctx.Inventory.CancelDrag();
    }

    // ── Tick ──────────────────────────────────────────────────────────────

    public void Update(GameContext ctx)
    {
        var inv = ctx.Inventory;
        var (mx, my) = ctx.Input.MousePosition;

        bool shiftHeld = ctx.Input.IsKeyPressed(Key.ShiftLeft)
                      || ctx.Input.IsKeyPressed(Key.ShiftRight);

        int clicks = ctx.Input.ConsumeLeftClicks();

        if (clicks > 0)
        {
            var hit = HitTestAll(mx, my);

            if (hit.HasValue)
            {
                if (shiftHeld && inv.Drag is null)
                {
                    inv.ShiftClick(hit.Value);
                }
                else if (inv.Drag is null)
                {
                    inv.BeginDrag(hit.Value);
                }
                else
                {
                    inv.Drop(hit.Value);
                }
            }
            else
            {
                // Außerhalb → Drag abbrechen
                if (inv.Drag is not null)
                    inv.CancelDrag();
            }
        }
    }

    // ── Render ────────────────────────────────────────────────────────────

    public void Render(GameContext ctx, double frameTime, int screenW, int screenH)
    {
        _lastScreenW = screenW;
        _lastScreenH = screenH;

        var inv = ctx.Inventory;
        var (mx, my) = ctx.Input.MousePosition;

        _panelX = (screenW - PanelW) / 2f;
        _panelY = (screenH - PanelH) / 2f;

        float gridX    = _panelX + PanelPadX;
        float gridY    = _panelY + PanelPadY;
        float hotbarY  = gridY + GridH + SectionGap;
        float eqX      = gridX + GridW + PanelPadX;
        float eqY      = gridY + (GridH - EqSectionH) / 2f;   // vertikal zentriert neben Grid

        // ── Pass 1: Quads / Hintergründe ──────────────────────────────────
        _text.BeginFrame(screenW, screenH);

        // Halbtransparentes Overlay
        _text.DrawQuad(0, 0, screenW, screenH, r: 0f, g: 0f, b: 0f, a: 0.45f);

        // Panel-Hintergrund
        _text.DrawQuad(_panelX, _panelY, PanelW, PanelH,
            r: 0.10f, g: 0.10f, b: 0.10f, a: 0.93f);

        // Grid-Slots
        for (int i = 0; i < InventoryGrid.TotalSlots; i++)
        {
            var (sx, sy) = GridSlotPos(i, gridX, gridY);
            DrawSlotBackground(sx, sy, SlotSize, inv.Grid.Get(i) is not null);
        }

        // Hotbar-Slots
        for (int i = 0; i < HotbarSlots; i++)
        {
            var (sx, sy) = HotbarSlotPos(i, gridX, hotbarY);
            bool isSelected = i == inv.Hotbar.SelectedSlot;
            DrawSlotBackground(sx, sy, SlotSize, inv.Hotbar.Hotbar[i] is not null, isSelected);
        }

        // Equipment-Slots
        var eqLabels = new[] { "Helm", "Brust", "Beine", "Schuh" };
        for (int i = 0; i < 4; i++)
        {
            float sx = eqX;
            float sy = eqY + i * (EqSlotH + EqGap);
            var   eq = inv.Equipment.Get((EquipmentSlotType)i);
            DrawSlotBackground(sx, sy, EqSlotW, eq is not null);

            // Label-Beschriftung rechts neben Slot (nur wenn kein Icon)
            float lx = sx + EqSlotW + 4f;
            float ly = sy + (EqSlotH - SmallCharH) / 2f;
            _text.DrawText(eqLabels[i], lx, ly,
                r: 0.55f, g: 0.55f, b: 0.55f,
                charWidth: SmallCharW, charHeight: SmallCharH);
        }

        // Abschnitts-Trennlinie (Grid / Hotbar)
        float sepY = hotbarY - SectionGap * 0.5f - 1f;
        _text.DrawQuad(gridX, sepY, GridW, 1f,
            r: 0.35f, g: 0.35f, b: 0.35f, a: 0.7f);

        // Titel "Inventar"
        const string title = "Inventar";
        _text.DrawText(title, _panelX + PanelPadX, _panelY + _panelY - 26f,
            r: 0.85f, g: 0.85f, b: 0.85f);

        _text.EndFrame();

        // ── Pass 2: Block-Icons ────────────────────────────────────────────
        if (_atlas is not null)
        {
            _icon.BeginFrame(screenW, screenH, _atlas);

            for (int i = 0; i < InventoryGrid.TotalSlots; i++)
            {
                if (inv.Grid.Get(i) is not { } stack) continue;
                var (sx, sy) = GridSlotPos(i, gridX, gridY);
                var def = BlockRegistry.Get(stack.BlockType);
                _icon.DrawIcon(sx + IconPad, sy + IconPad, SlotSize - 2 * IconPad, def.TopTextureIndex);
            }

            for (int i = 0; i < HotbarSlots; i++)
            {
                if (inv.Hotbar.Hotbar[i] is not { } stack) continue;
                var (sx, sy) = HotbarSlotPos(i, gridX, hotbarY);
                var def = BlockRegistry.Get(stack.BlockType);
                _icon.DrawIcon(sx + IconPad, sy + IconPad, SlotSize - 2 * IconPad, def.TopTextureIndex);
            }

            for (int i = 0; i < 4; i++)
            {
                if (inv.Equipment.Get((EquipmentSlotType)i) is not { } stack) continue;
                float sx = eqX;
                float sy = eqY + i * (EqSlotH + EqGap);
                var def = BlockRegistry.Get(stack.BlockType);
                _icon.DrawIcon(sx + IconPad, sy + IconPad, EqSlotW - 2 * IconPad, def.TopTextureIndex);
            }

            // Gezogenes Item am Mauszeiger
            if (inv.Drag is { } drag && _atlas is not null)
            {
                var def = BlockRegistry.Get(drag.Item.BlockType);
                float dragSize = SlotSize - 2 * IconPad;
                _icon.DrawIcon(mx - dragSize / 2f, my - dragSize / 2f, dragSize, def.TopTextureIndex);
            }

            _icon.EndFrame();
        }

        // ── Pass 3: Stack-Zahlen + Drag-Label ────────────────────────────
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

        // Drag-Item Anzahl am Cursor (wenn > 1)
        if (inv.Drag is { } d && d.Item.Count > 1)
        {
            float dragSize = SlotSize - 2 * IconPad;
            float ox = mx - dragSize / 2f;
            float oy = my - dragSize / 2f;
            DrawCount(d.Item.Count, ox, oy, dragSize);
        }

        _text.EndFrame();
    }

    // ── IDisposable ───────────────────────────────────────────────────────

    public void Dispose()
    {
        // Panel besitzt die Renderer (wurden vom Engine für dieses Panel erstellt)
        _text.Dispose();
        _icon.Dispose();
        GC.SuppressFinalize(this);
    }

    // ── Hilfsmethoden ─────────────────────────────────────────────────────

    private void DrawSlotBackground(float x, float y, float size, bool occupied, bool selected = false)
    {
        float r = selected ? 0.85f : (occupied ? 0.16f : 0.13f);
        float g = selected ? 0.85f : (occupied ? 0.16f : 0.13f);
        float b = selected ? 0.20f : (occupied ? 0.16f : 0.13f);
        _text.DrawQuad(x, y, size, size, r: r, g: g, b: b, a: 0.88f);

        // Rahmen
        float br = selected ? 0.9f : 0.28f;
        float bg = selected ? 0.9f : 0.28f;
        float bb = selected ? 0.3f : 0.28f;
        _text.DrawQuad(x,          y,          size, 1f,   r: br, g: bg, b: bb, a: 1f);
        _text.DrawQuad(x,          y + size - 1, size, 1f, r: br, g: bg, b: bb, a: 1f);
        _text.DrawQuad(x,          y,          1f,   size,  r: br, g: bg, b: bb, a: 1f);
        _text.DrawQuad(x + size - 1, y,        1f,   size,  r: br, g: bg, b: bb, a: 1f);
    }

    private void DrawCount(int count, float slotX, float slotY, float slotSize)
    {
        string s = count.ToString();
        float  x = slotX + slotSize - s.Length * SmallCharW - 2f;
        float  y = slotY + slotSize - SmallCharH - 2f;
        _text.DrawText(s, x + 1f, y + 1f, r: 0f, g: 0f, b: 0f, a: 0.8f,
            charWidth: SmallCharW, charHeight: SmallCharH);
        _text.DrawText(s, x, y,           r: 1f, g: 1f, b: 1f,
            charWidth: SmallCharW, charHeight: SmallCharH);
    }

    private static (float x, float y) GridSlotPos(int index, float originX, float originY)
    {
        int row = index / GridCols;
        int col = index % GridCols;
        return (originX + col * (SlotSize + Gap),
                originY + row * (SlotSize + Gap));
    }

    private static (float x, float y) HotbarSlotPos(int index, float originX, float originY)
        => (originX + index * (SlotSize + Gap), originY);

    /// <summary>
    /// Trifft alle Slots (Grid, Hotbar, Equipment) und gibt die SlotAddress zurück.
    /// Null wenn der Mauszeiger keinen Slot trifft.
    /// </summary>
    private SlotAddress? HitTestAll(float mx, float my)
    {
        float gridX   = _panelX + PanelPadX;
        float gridY   = _panelY + PanelPadY;
        float hotbarY = gridY + GridH + SectionGap;
        float eqX     = gridX + GridW + PanelPadX;
        float eqY     = gridY + (GridH - (4 * EqSlotH + 3 * EqGap)) / 2f;

        // Grid
        for (int i = 0; i < InventoryGrid.TotalSlots; i++)
        {
            var (sx, sy) = GridSlotPos(i, gridX, gridY);
            if (InSlot(mx, my, sx, sy, SlotSize))
                return SlotAddress.Grid(i);
        }

        // Hotbar
        for (int i = 0; i < HotbarSlots; i++)
        {
            var (sx, sy) = HotbarSlotPos(i, gridX, hotbarY);
            if (InSlot(mx, my, sx, sy, SlotSize))
                return SlotAddress.Hotbar(i);
        }

        // Equipment
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
