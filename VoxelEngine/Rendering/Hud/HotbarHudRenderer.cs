using Silk.NET.OpenGL;
using VoxelEngine.Core;
using VoxelEngine.Core.Hud;
using VoxelEngine.World;

namespace VoxelEngine.Rendering.Hud;

/// <summary>
/// Rendert die 9-Slot-Hotbar am unteren Bildschirmrand.
///
/// Slot-Aufbau:
///   - Dunkler Hintergrund
///   - Gelber Rahmen für ausgewählten Slot
///   - Block-Icon (Top-Face-Textur aus der ArrayTexture), füllt Slot mit je 4px Rand
///   - Stack-Anzahl rechts unten (nur wenn > 1)
///   - Slot-Nummer 1–9 klein oben links
///   - Leere Slots bleiben leer
/// </summary>
public sealed class HotbarHudRenderer : IHudRenderer
{
    private readonly TextRenderer   _textRenderer;
    private readonly IconRenderer   _iconRenderer;
    private readonly EngineSettings _settings;

    // ArrayTexture wird nach Init über die Renderer-Property injiziert
    private ArrayTexture? _atlas;
    public  ArrayTexture? Atlas { get => _atlas; set => _atlas = value; }

    private const float Gap      = 2f;
    private const float IconPad  = 5f;   // Abstand Icon-Rand zum Slot-Rand (px)

    public HotbarHudRenderer(GL gl, EngineSettings settings, int windowWidth, int windowHeight)
    {
        _settings     = settings;
        var font      = new BitmapFont(gl, "Assets/Fonts/font.png");
        _textRenderer = new TextRenderer(gl, font, windowWidth, windowHeight);
        _iconRenderer = new IconRenderer(gl);
    }

    public void Render(IHudElement element, int screenW, int screenH)
    {
        if (element is not HotbarHudElement el)
            return;

        float slotSize   = _settings.HotbarSlotSize * el.Config.Scale;
        float totalWidth = 9 * slotSize + 8 * Gap;
        float totalHeight = slotSize;

        var (startX, startY) = HudUtils.ResolveAnchor(
            el.Config.Anchor,
            el.Config.OffsetX,
            el.Config.OffsetY,
            screenW, screenH,
            totalWidth, totalHeight);

        // ── Hintergründe + Rahmen (TextRenderer) ─────────────────────────
        _textRenderer.BeginFrame(screenW, screenH);

        for (int i = 0; i < 9; i++)
        {
            float x = startX + i * (slotSize + Gap);
            float y = startY;

            // Slot-Hintergrund
            _textRenderer.DrawQuad(x, y, slotSize, slotSize,
                r: 0.1f, g: 0.1f, b: 0.1f, a: 0.75f);

            // Highlight für ausgewählten Slot (4 Rahmenstreifen à 2px)
            if (i == el.SelectedSlot)
            {
                const float h = 0.9f; const float v = 0.3f; const float a = 0.9f;
                _textRenderer.DrawQuad(x,                 y,                 slotSize, 2f,      r: h, g: h, b: v, a: a);
                _textRenderer.DrawQuad(x,                 y + slotSize - 2f, slotSize, 2f,      r: h, g: h, b: v, a: a);
                _textRenderer.DrawQuad(x,                 y,                 2f,       slotSize, r: h, g: h, b: v, a: a);
                _textRenderer.DrawQuad(x + slotSize - 2f, y,                 2f,       slotSize, r: h, g: h, b: v, a: a);
            }

            // Slot-Nummer (1–9) klein oben links
            _textRenderer.DrawText($"{i + 1}", x + 3f, y + 3f,
                r: 0.5f, g: 0.5f, b: 0.5f);
        }

        _textRenderer.EndFrame();

        // ── Block-Icons (IconRenderer — nur wenn Atlas verfügbar) ─────────
        if (_atlas is not null)
        {
            _iconRenderer.BeginFrame(screenW, screenH, _atlas);

            for (int i = 0; i < 9; i++)
            {
                if (el.Slots[i] is not { } stack)
                    continue;

                float x    = startX + i * (slotSize + Gap);
                float y    = startY;
                float size = slotSize - 2 * IconPad;

                var def   = BlockRegistry.Get(stack.BlockType);
                int layer = def.TopTextureIndex;

                _iconRenderer.DrawIcon(x + IconPad, y + IconPad, size, layer);
            }

            _iconRenderer.EndFrame();
        }

        // ── Stack-Anzahl (TextRenderer — nur wenn > 1) ────────────────────
        bool hasCount = false;
        for (int i = 0; i < 9; i++)
            if (el.Slots[i] is { Count: > 1 }) { hasCount = true; break; }

        if (hasCount)
        {
            _textRenderer.BeginFrame(screenW, screenH);
            for (int i = 0; i < 9; i++)
            {
                if (el.Slots[i] is not { Count: > 1 } stack)
                    continue;

                float x = startX + i * (slotSize + Gap);
                float y = startY;

                string countStr = stack.Count.ToString();
                float  textX    = x + slotSize - countStr.Length * 12f - 3f;
                float  textY    = y + slotSize - 18f;
                // Schatten
                _textRenderer.DrawText(countStr, textX + 1f, textY + 1f,
                    r: 0f, g: 0f, b: 0f, a: 0.8f);
                // Text
                _textRenderer.DrawText(countStr, textX, textY,
                    r: 1f, g: 1f, b: 1f);
            }
            _textRenderer.EndFrame();
        }
    }

    public void Dispose()
    {
        _textRenderer.Dispose();
        _iconRenderer.Dispose();
        GC.SuppressFinalize(this);
    }
}
