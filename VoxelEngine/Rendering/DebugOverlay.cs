using Silk.NET.OpenGL;
using VoxelEngine.Core;
using VoxelEngine.Rendering.Hud;

namespace VoxelEngine.Rendering;

/// <summary>
/// Dünner Wrapper um HudManager + DebugHudElement.
/// Stellt die bisherige Render()-Signatur weiterhin bereit.
/// </summary>
public class DebugOverlay : IDisposable
{
    private readonly GameContext     _context;
    private readonly HudManager      _hudManager;
    public  readonly DebugHudElement DebugElement;

    public HudManager HudManager => _hudManager;

    public DebugOverlay(GL gl, GameContext context, int windowWidth, int windowHeight)
    {
        _context     = context;
        DebugElement = new DebugHudElement();
        context.HudRegistry.Register(DebugElement);

        _hudManager = new HudManager(context.HudRegistry);
        _hudManager.RegisterRenderer("debug", new DebugHudRenderer(gl, windowWidth, windowHeight));
    }

    public void Render(int windowWidth, int windowHeight, double fps, string consoleInput)
    {
        DebugElement.Fps          = fps;
        DebugElement.ConsoleInput = consoleInput;
        _hudManager.RenderAll(_context, windowWidth, windowHeight);
    }

    public void Dispose()
    {
        _hudManager.Dispose();
        GC.SuppressFinalize(this);
    }
}
