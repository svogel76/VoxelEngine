using Silk.NET.OpenGL;
using VoxelEngine.Core;
using VoxelEngine.Core.Hud;

namespace VoxelEngine.Rendering.Hud;

/// <summary>
/// Rendert die Hungerleiste als Reihe von 10 Fleischkeulen-ähnlichen Symbolen.
///
/// Darstellung:
///   - 10 Symbole; jedes steht für 2 Hunger-Punkte
///   - Volle Einheit: warm-orange; halb: halb hell / halb dunkel; leer: dunkelbraun
///   - Beim Verhungern (Hunger = 0) Farbe wechselt zu einem matten Rotton
///   - Reihe wächst von rechts nach links (gespiegelt zur Herzleiste)
/// </summary>
public sealed class HungerHudRenderer : IHudRenderer
{
    private readonly TextRenderer   _text;
    private readonly EngineSettings _settings;

    private const int   Icons    = 10;
    private const float IconSize = 9f;
    private const float IconGap  = 2f;

    public HungerHudRenderer(GL gl, EngineSettings settings, int windowWidth, int windowHeight, string fontPath)
    {
        _settings = settings;
        var font  = new BitmapFont(gl, fontPath);
        _text     = new TextRenderer(gl, font, windowWidth, windowHeight);
    }

    public void Render(IHudElement element, int screenW, int screenH)
    {
        if (element is not HungerHudElement el) return;

        float scale  = el.Config.Scale;
        float is_    = IconSize * scale;
        float gap    = IconGap  * scale;
        float totalW = Icons * is_ + (Icons - 1) * gap;
        float totalH = is_;

        var (startX, startY) = HudUtils.ResolveAnchor(
            el.Config.Anchor,
            el.Config.OffsetX,
            el.Config.OffsetY,
            screenW, screenH,
            totalW, totalH);

        float hungerFloat = el.Hunger / (el.MaxHunger / Icons);   // 0..Icons

        _text.BeginFrame(screenW, screenH);

        // Symbole von rechts nach links rendern (Index 0 = ganz rechts)
        for (int i = 0; i < Icons; i++)
        {
            // Gespiegelt: Icon 0 ist das rechteste
            float x = startX + (Icons - 1 - i) * (is_ + gap);
            float y = startY;

            float filled = Math.Clamp(hungerFloat - i, 0f, 1f);
            DrawFoodIcon(x, y, is_, filled, el.IsStarving);
        }

        _text.EndFrame();
    }

    /// <summary>
    /// Zeichnet ein Hunger-Symbol (Fleischkeule-Näherung via Quads).
    ///
    /// Form: runder Kopf (oben) + Stiel (unten) — grob via 2 Quads.
    /// </summary>
    private void DrawFoodIcon(float x, float y, float size, float filled, bool starving)
    {
        float w = size;
        float h = size;

        // Leer: dunkelbraun
        float er = 0.20f; float eg = 0.12f; float eb = 0.06f;
        DrawFoodShape(x, y, w, h, r: er, g: eg, b: eb, a: 0.8f);

        if (filled <= 0f) return;

        // Füllfarbe: orange-braun normal, gedämpftes Rot beim Verhungern
        float fr = starving ? 0.55f : 0.88f;
        float fg = starving ? 0.08f : 0.48f;
        float fb = starving ? 0.08f : 0.08f;

        if (filled >= 1f)
        {
            DrawFoodShape(x, y, w, h, r: fr, g: fg, b: fb, a: 1f);
        }
        else
        {
            // Halbe Füllung: nur obere Hälfte des Kopfes
            float half = h * 0.5f;
            _text.DrawQuad(x + w * 0.15f, y,            w * 0.70f, half,         r: fr, g: fg, b: fb, a: 1f);
            _text.DrawQuad(x + w * 0.35f, y + half,      w * 0.30f, h * 0.35f,   r: fr, g: fg, b: fb, a: 1f);
        }
    }

    private void DrawFoodShape(float x, float y, float w, float h,
                                float r, float g, float b, float a)
    {
        // "Kopf" der Keule (oben, breiter)
        _text.DrawQuad(x + w * 0.10f, y,            w * 0.80f, h * 0.55f, r: r, g: g, b: b, a: a);
        // "Stiel" (unten, schmaler)
        _text.DrawQuad(x + w * 0.35f, y + h * 0.55f, w * 0.30f, h * 0.40f, r: r, g: g, b: b, a: a);
    }

    public void Dispose()
    {
        _text.Dispose();
        GC.SuppressFinalize(this);
    }
}

