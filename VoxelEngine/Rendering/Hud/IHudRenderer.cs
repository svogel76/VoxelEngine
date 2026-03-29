using VoxelEngine.Core.Hud;

namespace VoxelEngine.Rendering.Hud;

public interface IHudRenderer : IDisposable
{
    void Render(IHudElement element, int screenW, int screenH);
}
