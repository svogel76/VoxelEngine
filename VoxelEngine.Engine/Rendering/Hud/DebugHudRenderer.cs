using Silk.NET.OpenGL;
using VoxelEngine.Core.Hud;

namespace VoxelEngine.Rendering.Hud;

/// <summary>Rendert das Debug-HUD und die Konsole.</summary>
public sealed class DebugHudRenderer : IHudRenderer
{
    private readonly BitmapFont   _font;
    private readonly TextRenderer _textRenderer;

    private const float LineHeight = 18f;

    public DebugHudRenderer(GL gl, int windowWidth, int windowHeight, string fontPath)
    {
        _font         = new BitmapFont(gl, fontPath);
        _textRenderer = new TextRenderer(gl, _font, windowWidth, windowHeight);
    }

    public void Render(IHudElement element, int screenW, int screenH)
    {
        if (element is not DebugHudElement el)
            return;

        var (x, y) = HudUtils.ResolveAnchor(
            el.Config.Anchor,
            el.Config.OffsetX,
            el.Config.OffsetY,
            screenW, screenH);

        _textRenderer.BeginFrame(screenW, screenH);

        // HUD-Zeile
        string hud = $"FPS: {el.Fps:F0}  {el.Position}  {el.ChunkInfo}  {el.VertInfo}  {el.TimeStr}  {el.SelectedBlock}  {el.ReachStr}";
        _textRenderer.DrawText(hud, x + 8f, y + 8f, r: 1f, g: 1f, b: 0f);

        // Konsole
        if (el.ConsoleOpen)
        {
            var output = el.ConsoleOutput;

            float bgHeight = 9 * LineHeight + 12f;
            float bgY      = screenH - bgHeight - 4f;
            _textRenderer.DrawQuad(0f, bgY, screenW, bgHeight + 4f,
                                   r: 0f, g: 0f, b: 0f, a: 0.72f);

            int linesToShow = Math.Min(output.Count, 8);
            for (int i = 0; i < linesToShow; i++)
            {
                string line  = output[output.Count - 1 - i];
                float  lineY = screenH - 40f - i * LineHeight;
                _textRenderer.DrawText(line, 10f, lineY, r: 0.9f, g: 0.9f, b: 0.9f);
            }

            _textRenderer.DrawText($"> {el.ConsoleInput}_",
                                   10f, screenH - 22f,
                                   r: 0.3f, g: 1f, b: 0.3f);
        }

        _textRenderer.EndFrame();
    }

    public void Dispose()
    {
        _textRenderer.Dispose();
        _font.Dispose();
        GC.SuppressFinalize(this);
    }
}

