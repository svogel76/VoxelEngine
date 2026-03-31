using VoxelEngine.Core;
using VoxelEngine.Core.Hud;

namespace VoxelEngine.Rendering.Hud;

/// <summary>Rendert alle registrierten HUD-Elemente in ZOrder-Reihenfolge.</summary>
public sealed class HudManager : IDisposable
{
    private readonly HudRegistry _registry;
    private readonly Dictionary<string, IHudRenderer> _renderers = new(StringComparer.OrdinalIgnoreCase);
    private bool _disposed;

    public HudManager(HudRegistry registry)
    {
        _registry = registry;
    }

    public void RegisterRenderer(string elementId, IHudRenderer renderer)
    {
        _renderers[elementId] = renderer;
    }

    public void RenderAll(GameContext ctx, int screenW, int screenH)
    {
        foreach (var element in _registry.GetAll())
        {
            if (!element.Visible)
                continue;
            if (!_renderers.TryGetValue(element.Id, out var renderer))
                continue;
            element.Update(ctx);
            renderer.Render(element, screenW, screenH);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        foreach (var renderer in _renderers.Values)
            renderer.Dispose();
        GC.SuppressFinalize(this);
    }
}
