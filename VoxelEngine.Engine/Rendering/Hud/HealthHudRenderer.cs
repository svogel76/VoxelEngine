using Silk.NET.OpenGL;
using VoxelEngine.Core;
using VoxelEngine.Core.Hud;

namespace VoxelEngine.Rendering.Hud;

/// <summary>
/// Rendert die Gesundheitsleiste als Reihe von 10 Herzen.
///
/// Darstellung:
///   - 10 Herz-Symbole; jedes Herz steht für 2 HP
///   - Volles Herz: rot; halbes Herz: halb rot / halb dunkel; leeres Herz: dunkelgrau
///   - Bei kritisch niedriger Gesundheit (&lt;20%) pulsiert die Farbe (hell → dunkelrot)
///   - Herz-Silhouette via zwei Quads (grobe Pixelnäherung — kein Sprite nötig)
/// </summary>
public sealed class HealthHudRenderer : IHudRenderer
{
    private readonly TextRenderer   _text;
    private readonly EngineSettings _settings;

    private const int   Hearts    = 10;        // Herzen insgesamt (= MaxHealth / 2)
    private const float HeartSize = 9f;        // Breite/Höhe eines Herzsymbols
    private const float HeartGap  = 2f;

    // Pulsieren bei kritischer HP
    private double _pulseTimer;

    public HealthHudRenderer(GL gl, EngineSettings settings, int windowWidth, int windowHeight)
    {
        _settings = settings;
        var font  = new BitmapFont(gl, "Assets/Fonts/font.png");
        _text     = new TextRenderer(gl, font, windowWidth, windowHeight);
    }

    public void Render(IHudElement element, int screenW, int screenH)
    {
        if (element is not HealthHudElement el) return;

        float scale    = el.Config.Scale;
        float hs       = HeartSize * scale;
        float gap      = HeartGap  * scale;
        float totalW   = Hearts * hs + (Hearts - 1) * gap;
        float totalH   = hs;

        var (startX, startY) = HudUtils.ResolveAnchor(
            el.Config.Anchor,
            el.Config.OffsetX,
            el.Config.OffsetY,
            screenW, screenH,
            totalW, totalH);

        // HP zu Herzen umrechnen: 0..20 → 0..10 Herzen, mit halben Herzen
        float hp         = el.Health;
        float maxHp      = el.MaxHealth;
        float heartsFloat = hp / (maxHp / Hearts);   // 0..Hearts (float, halbe Herzen)

        // Puls-Effekt bei kritischer HP (langsames Blinken ~1 Hz)
        _pulseTimer += 0.016;   // ca. 60 fps — wird nicht exakt benötigt
        float pulse  = el.IsCritical ? (float)(0.5 + 0.5 * Math.Sin(_pulseTimer * 6.0)) : 1f;

        _text.BeginFrame(screenW, screenH);

        for (int i = 0; i < Hearts; i++)
        {
            float x = startX + i * (hs + gap);
            float y = startY;

            float filled = Math.Clamp(heartsFloat - i, 0f, 1f);   // 0, 0.5 oder 1

            // Herz-Silhouette: grob via zwei überlappende Quads (♥-Annäherung)
            DrawHeart(x, y, hs, filled, pulse, el.IsCritical);
        }

        _text.EndFrame();
    }

    /// <summary>
    /// Zeichnet ein einzelnes Herz-Symbol via Quad-Komposition.
    ///
    /// ♥-Näherung (3 Quads):
    ///   [obere zwei Bumps] + [untere Raute]
    ///   Einfach, kein Sprite-Sheet benötigt.
    /// </summary>
    private void DrawHeart(float x, float y, float size, float filled, float pulse, bool critical)
    {
        float h = size;
        float w = size;

        // Hintergrund (leeres Herz): dunkles Grau
        DrawHeartShape(x, y, w, h, r: 0.22f, g: 0.10f, b: 0.10f, a: 0.8f);

        if (filled <= 0f) return;

        // Füllfarbe
        float rFull = critical ? 0.5f + 0.5f * pulse : 0.85f;
        float gFull = critical ? 0.05f               : 0.10f;
        float bFull = critical ? 0.05f               : 0.10f;

        if (filled >= 1f)
        {
            // Volles Herz
            DrawHeartShape(x, y, w, h, r: rFull, g: gFull, b: bFull, a: 1f);
        }
        else
        {
            // Halbes Herz: nur die linke Hälfte füllen
            float half = w * 0.5f;
            // Obere linke Beule
            _text.DrawQuad(x,                  y + h * 0.10f, half, h * 0.45f, r: rFull, g: gFull, b: bFull, a: 1f);
            // Untere linke Dreieckshälfte (Näherung: Rechteck)
            _text.DrawQuad(x,                  y + h * 0.50f, half, h * 0.42f, r: rFull, g: gFull, b: bFull, a: 1f);
        }
    }

    /// <summary>Zeichnet die Herzform via 3 Quads.</summary>
    private void DrawHeartShape(float x, float y, float w, float h,
                                 float r, float g, float b, float a)
    {
        // Zwei obere Beulen
        _text.DrawQuad(x + w * 0.04f, y + h * 0.10f, w * 0.44f, h * 0.45f, r: r, g: g, b: b, a: a);
        _text.DrawQuad(x + w * 0.52f, y + h * 0.10f, w * 0.44f, h * 0.45f, r: r, g: g, b: b, a: a);
        // Unterer Spitz (Raute als breites Rechteck)
        _text.DrawQuad(x + w * 0.04f, y + h * 0.50f, w * 0.92f, h * 0.42f, r: r, g: g, b: b, a: a);
    }

    public void Dispose()
    {
        _text.Dispose();
        GC.SuppressFinalize(this);
    }
}
