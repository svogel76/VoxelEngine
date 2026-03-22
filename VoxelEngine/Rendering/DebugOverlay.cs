using Silk.NET.OpenGL;
using VoxelEngine.Core;

namespace VoxelEngine.Rendering;

public class DebugOverlay : IDisposable
{
    private readonly GameContext  _context;
    private readonly BitmapFont   _font;
    private readonly TextRenderer _textRenderer;

    private const float CharW      = 9f;
    private const float CharH      = 16f;
    private const float LineHeight = 18f;

    public DebugOverlay(GL gl, GameContext context, int windowWidth, int windowHeight)
    {
        _context      = context;
        _font         = new BitmapFont(gl, "Assets/Fonts/font.png");
        _textRenderer = new TextRenderer(gl, _font, windowWidth, windowHeight);
    }

    public void Render(int windowWidth, int windowHeight, double fps, string consoleInput)
    {
        _textRenderer.BeginFrame(windowWidth, windowHeight);

        // HUD — immer sichtbar (oben links)
        var pos     = _context.Camera.Position;
        int visible = _context.Renderer.VisibleChunkCount;
        int loaded  = _context.World.LoadedChunkCount;
        string hud  = $"FPS: {fps:F0}  X:{pos.X:F1} Y:{pos.Y:F1} Z:{pos.Z:F1}  Chunks: {visible}/{loaded}";
        _textRenderer.DrawText(hud, 8f, 8f, r: 1f, g: 1f, b: 0f);

        if (_context.Console.IsOpen)
        {
            var output = _context.Console.GetOutput();

            // Hintergrund-Balken
            float bgHeight    = 9 * LineHeight + 12f;
            float bgY         = windowHeight - bgHeight - 4f;
            _textRenderer.DrawQuad(0f, bgY, windowWidth, bgHeight + 4f,
                                   r: 0f, g: 0f, b: 0f, a: 0.72f);

            // Ausgabe-Zeilen — von unten nach oben
            int linesToShow = Math.Min(output.Count, 8);
            for (int i = 0; i < linesToShow; i++)
            {
                string line = output[output.Count - 1 - i];
                float  lineY = windowHeight - 40f - i * LineHeight;
                _textRenderer.DrawText(line, 10f, lineY, r: 0.9f, g: 0.9f, b: 0.9f);
            }

            // Eingabezeile
            _textRenderer.DrawText($"> {consoleInput}_",
                                   10f, windowHeight - 22f,
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
