using Silk.NET.Input;
using Silk.NET.OpenGL;
using VoxelEngine.Rendering;

namespace VoxelEngine.Core.UI.Panels;

/// <summary>
/// Pause-Menü — öffnet sich über Escape wenn kein anderes Panel offen ist.
///
/// Buttons:
///   Weiterspielen  → schließt das Panel (Spiel läuft weiter)
///   Speichern      → ruft GameContext.SaveGameStateAsync() auf
///   Beenden        → setzt ShutdownRequested = true
///
/// Während das Panel offen ist, wird WorldTime.Paused = true gesetzt.
/// Beim Schließen wird Paused wieder auf false zurückgesetzt.
/// </summary>
public sealed class PauseMenuPanel : IUIPanel, IDisposable
{
    // ── IUIPanel ──────────────────────────────────────────────────────────

    public string Id        => "pause-menu";
    public Key?   ToggleKey => null;   // nur über Escape erreichbar

    // ── Layout-Konstanten ────────────────────────────────────────────────

    private const float PanelW       = 320f;
    private const float PanelH       = 240f;
    private const float ButtonW      = 240f;
    private const float ButtonH      = 40f;
    private const float ButtonGap    = 12f;
    private const float TitleH       = 32f;
    private const float CharW        = 12f;
    private const float CharH        = 16f;
    private const float TitleCharW   = 18f;
    private const float TitleCharH   = 24f;

    // Buttons: Label + Aktion
    private record ButtonDef(string Label, Action<GameContext> OnClick);
    private readonly ButtonDef[] _buttons;

    // Hover-Tracking (Button-Index oder -1)
    private int _hoveredButton = -1;

    // Zuletzt bekannte Screendims (aus Render — für Hover-Berechnung im Tick)
    private int _lastScreenW;
    private int _lastScreenH;

    // Async-Speicher-Feedback
    private bool   _saving;
    private string _saveStatus = "";
    private double _saveStatusTimer;

    // OpenGL-Ressourcen
    private TextRenderer? _textRenderer;
    private GL?           _gl;

    public PauseMenuPanel()
    {
        _buttons =
        [
            new("Weiterspielen",  ctx => ctx.UI.CloseTop(ctx)),
            new("Speichern",      ctx => StartSave(ctx)),
            new("Beenden",        ctx => ctx.ShutdownRequested = true),
        ];
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────

    public void OnOpen(GameContext ctx)
    {
        ctx.Time.Paused = true;
    }

    public void OnClose(GameContext ctx)
    {
        ctx.Time.Paused = false;
        _saveStatus      = "";
        _saving          = false;
    }

    // ── Tick ──────────────────────────────────────────────────────────────

    public void Update(GameContext ctx)
    {
        // Speicher-Status-Nachricht ausblenden nach 3 Sekunden
        if (_saveStatusTimer > 0)
        {
            _saveStatusTimer -= 1.0 / 20.0;   // FixedDelta ~50ms @ 20 UPS
            if (_saveStatusTimer <= 0)
                _saveStatus = "";
        }

        // Mausposition → Hover ermitteln (nutzt zuletzt bekannte Screendims aus Render)
        var (mx, my) = ctx.Input.MousePosition;
        _hoveredButton = HitTest(mx, my,
            GetPanelX(_lastScreenW),
            GetPanelY(_lastScreenH));

        // Linksklick → Button auslösen
        if (ctx.Input.ConsumeLeftClicks() > 0 && _hoveredButton >= 0)
            _buttons[_hoveredButton].OnClick(ctx);
    }

    // ── Render ────────────────────────────────────────────────────────────

    public void Render(GameContext ctx, double frameTime, int screenW, int screenH)
    {
        // TextRenderer lazy-init (GL-Kontext erst beim ersten Render verfügbar)
        if (_textRenderer is null)
            return;

        _lastScreenW = screenW;
        _lastScreenH = screenH;

        float panelX = GetPanelX(screenW);
        float panelY = GetPanelY(screenH);

        // Hover nochmals aus Render-Perspektive berechnen (präzise Screendims)
        var (mx, my) = ctx.Input.MousePosition;
        _hoveredButton = HitTest(mx, my, panelX, panelY);

        _textRenderer.BeginFrame(screenW, screenH);

        // ── Hintergrund-Overlay (abdunkeln) ──────────────────────────────
        _textRenderer.DrawQuad(0, 0, screenW, screenH,
            r: 0f, g: 0f, b: 0f, a: 0.55f);

        // ── Panel-Box ────────────────────────────────────────────────────
        _textRenderer.DrawQuad(panelX, panelY, PanelW, PanelH,
            r: 0.08f, g: 0.08f, b: 0.08f, a: 0.92f);

        // ── Titel "Pause" ─────────────────────────────────────────────────
        const string title = "Pause";
        float titleW  = title.Length * TitleCharW;
        float titleX  = panelX + (PanelW - titleW) / 2f;
        float titleY  = panelY + 20f;
        _textRenderer.DrawText(title, titleX, titleY,
            r: 1f, g: 1f, b: 1f,
            charWidth: TitleCharW, charHeight: TitleCharH);

        // ── Buttons ───────────────────────────────────────────────────────
        float firstButtonY = panelY + TitleH + TitleCharH + 24f;

        for (int i = 0; i < _buttons.Length; i++)
        {
            float bx = panelX + (PanelW - ButtonW) / 2f;
            float by = firstButtonY + i * (ButtonH + ButtonGap);

            bool hovered = i == _hoveredButton;

            // Button-Hintergrund
            if (hovered)
                _textRenderer.DrawQuad(bx, by, ButtonW, ButtonH,
                    r: 0.25f, g: 0.55f, b: 0.25f, a: 0.90f);
            else
                _textRenderer.DrawQuad(bx, by, ButtonW, ButtonH,
                    r: 0.18f, g: 0.18f, b: 0.18f, a: 0.88f);

            // Button-Rahmen (4 dünne Streifen)
            float br = hovered ? 0.6f : 0.35f;
            float bg = hovered ? 0.9f : 0.35f;
            float bb = hovered ? 0.6f : 0.35f;
            _textRenderer.DrawQuad(bx,               by,              ButtonW, 1f, r: br, g: bg, b: bb, a: 1f);
            _textRenderer.DrawQuad(bx,               by + ButtonH - 1f, ButtonW, 1f, r: br, g: bg, b: bb, a: 1f);
            _textRenderer.DrawQuad(bx,               by,              1f, ButtonH, r: br, g: bg, b: bb, a: 1f);
            _textRenderer.DrawQuad(bx + ButtonW - 1f, by,             1f, ButtonH, r: br, g: bg, b: bb, a: 1f);

            // Button-Text zentriert
            string label  = _buttons[i].Label;
            float  textW  = label.Length * CharW;
            float  textX  = bx + (ButtonW - textW) / 2f;
            float  textY  = by + (ButtonH - CharH) / 2f;
            _textRenderer.DrawText(label, textX, textY,
                r: 1f, g: 1f, b: 1f);
        }

        // ── Speicher-Status-Meldung ───────────────────────────────────────
        if (_saveStatus.Length > 0)
        {
            float stW = _saveStatus.Length * CharW;
            float stX = panelX + (PanelW - stW) / 2f;
            float stY = panelY + PanelH - CharH - 12f;
            bool  ok  = _saveStatus.StartsWith("Gespeichert");
            _textRenderer.DrawText(_saveStatus, stX, stY,
                r: ok ? 0.4f : 1f,
                g: ok ? 1.0f : 0.4f,
                b: ok ? 0.4f : 0.4f);
        }

        _textRenderer.EndFrame();
    }

    // ── OpenGL-Init ───────────────────────────────────────────────────────

    /// <summary>
    /// Initialisiert den TextRenderer. Muss nach GL-Kontext-Init aufgerufen werden,
    /// d.h. aus Engine.Load() oder beim ersten OnOpen().
    /// </summary>
    public void InitRenderer(GL gl, string fontPath)
    {
        _gl = gl;
        var font      = new BitmapFont(gl, fontPath);
        _textRenderer = new TextRenderer(gl, font, 1920, 1080); // Screendims werden in BeginFrame() überschrieben
    }

    // ── IDisposable ───────────────────────────────────────────────────────

    public void Dispose()
    {
        _textRenderer?.Dispose();
        GC.SuppressFinalize(this);
    }

    // ── Hilfsmethoden ────────────────────────────────────────────────────

    private static float GetPanelX(int screenW) => (screenW - PanelW) / 2f;
    private static float GetPanelY(int screenH) => (screenH - PanelH) / 2f;

    private int HitTest(float mx, float my, float panelX, float panelY)
    {
        float bx           = panelX + (PanelW - ButtonW) / 2f;
        float firstButtonY = panelY + TitleH + TitleCharH + 24f;

        for (int i = 0; i < _buttons.Length; i++)
        {
            float by = firstButtonY + i * (ButtonH + ButtonGap);
            if (mx >= bx && mx <= bx + ButtonW && my >= by && my <= by + ButtonH)
                return i;
        }
        return -1;
    }

    private void StartSave(GameContext ctx)
    {
        if (_saving)
            return;

        _saving     = true;
        _saveStatus = "Speichern...";

        ctx.SaveGameStateAsync().ContinueWith(t =>
        {
            _saving          = false;
            _saveStatus      = t.IsFaulted
                ? "Fehler beim Speichern!"
                : "Gespeichert!";
            _saveStatusTimer = 3.0;
        });
    }
}

