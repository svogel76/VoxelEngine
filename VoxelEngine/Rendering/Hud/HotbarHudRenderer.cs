using Silk.NET.OpenGL;
using VoxelEngine.Core;
using VoxelEngine.Core.Hud;
using VoxelEngine.World;

namespace VoxelEngine.Rendering.Hud;

/// <summary>Rendert die 9-Slot-Hotbar am unteren Bildschirmrand.</summary>
public sealed class HotbarHudRenderer : IHudRenderer
{
    private readonly TextRenderer  _textRenderer;
    private readonly EngineSettings _settings;
    private const float Gap = 2f;

    public HotbarHudRenderer(GL gl, EngineSettings settings, int windowWidth, int windowHeight)
    {
        _settings = settings;
        var font  = new BitmapFont(gl, "Assets/Fonts/font.png");
        _textRenderer = new TextRenderer(gl, font, windowWidth, windowHeight);
    }

    public void Render(IHudElement element, int screenW, int screenH)
    {
        if (element is not HotbarHudElement el)
            return;

        float slotSize    = _settings.HotbarSlotSize * el.Config.Scale;
        float totalWidth  = 9 * slotSize + 8 * Gap;
        float totalHeight = slotSize;

        var (startX, startY) = HudUtils.ResolveAnchor(
            el.Config.Anchor,
            el.Config.OffsetX,
            el.Config.OffsetY,
            screenW, screenH,
            totalWidth, totalHeight);

        _textRenderer.BeginFrame(screenW, screenH);

        for (int i = 0; i < 9; i++)
        {
            float x = startX + i * (slotSize + Gap);
            float y = startY;

            // Slot-Hintergrund
            _textRenderer.DrawQuad(x, y, slotSize, slotSize, r: 0.1f, g: 0.1f, b: 0.1f, a: 0.75f);

            // Highlight für ausgewählten Slot (4 Rahmenstreifen à 2px)
            if (i == el.SelectedSlot)
            {
                float h = 0.9f; float v = 0.3f; float a = 0.9f;
                _textRenderer.DrawQuad(x,              y,              slotSize, 2f,      r: h, g: h, b: v, a: a);
                _textRenderer.DrawQuad(x,              y + slotSize - 2f, slotSize, 2f,  r: h, g: h, b: v, a: a);
                _textRenderer.DrawQuad(x,              y,              2f,      slotSize, r: h, g: h, b: v, a: a);
                _textRenderer.DrawQuad(x + slotSize - 2f, y,          2f,      slotSize, r: h, g: h, b: v, a: a);
            }

            // Slot-Nummer (1–9) klein oben links
            _textRenderer.DrawText($"{i + 1}", x + 3f, y + 3f, r: 0.5f, g: 0.5f, b: 0.5f);

            // Block-Inhalt
            if (el.Slots[i] is { } stack)
            {
                string name  = BlockRegistry.Get(stack.BlockType).Name;
                string label = name.Length > 4 ? name[..4] : name;
                // Block-Name (4 Zeichen) vertikal zentriert
                _textRenderer.DrawText(label, x + 4f, y + slotSize / 2f - 8f, r: 1f, g: 1f, b: 1f);
                // Anzahl rechts unten
                _textRenderer.DrawText($"{stack.Count}", x + slotSize - 18f, y + slotSize - 18f, r: 1f, g: 1f, b: 1f);
            }
        }

        _textRenderer.EndFrame();
    }

    public void Dispose()
    {
        _textRenderer.Dispose();
        GC.SuppressFinalize(this);
    }
}
