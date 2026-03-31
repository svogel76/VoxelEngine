using Silk.NET.Input;
using VoxelEngine.Core;

namespace VoxelEngine.Core.UI;

public interface IUIPanel
{
    string Id { get; }
    Key? ToggleKey { get; }
    void OnOpen(GameContext ctx);
    void OnClose(GameContext ctx);
    void Update(GameContext ctx);
    void Render(GameContext ctx, double frameTime, int screenW, int screenH);
}
