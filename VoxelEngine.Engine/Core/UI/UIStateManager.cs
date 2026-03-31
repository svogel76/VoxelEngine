namespace VoxelEngine.Core.UI;

public class UIStateManager
{
    private readonly Dictionary<string, IUIPanel> _registered = new();
    private readonly Stack<IUIPanel> _stack = new();
    private IUIPanel? _gameMenu;
    private readonly HashSet<Key> _keysHeldLastFrame = new();

    public bool IsAnyPanelOpen => _stack.Count > 0;

    public void Register(IUIPanel panel, bool isGameMenu = false)
    {
        _registered[panel.Id] = panel;
        if (isGameMenu)
            _gameMenu = panel;
    }

    public void CloseTop(GameContext ctx)
    {
        Pop(ctx);
    }

    public bool Update(GameContext ctx)
    {
        foreach (var panel in _registered.Values)
        {
            if (panel.ToggleKey is not { } key)
                continue;

            bool heldNow = ctx.Input.IsKeyPressed(key);
            bool heldPrev = _keysHeldLastFrame.Contains(key);
            bool justPressed = heldNow && !heldPrev;

            if (heldNow) _keysHeldLastFrame.Add(key);
            else _keysHeldLastFrame.Remove(key);

            if (!justPressed)
                continue;

            if (_stack.Count > 0 && _stack.Peek() == panel)
                Pop(ctx);
            else
                Push(ctx, panel);
        }

        var pauseKey = ctx.KeyBindings.Pause;
        bool pauseNow = ctx.Input.IsKeyPressed(pauseKey);
        bool pausePrev = _keysHeldLastFrame.Contains(pauseKey);
        if (pauseNow && !pausePrev)
        {
            if (_stack.Count > 0)
                ClearAll(ctx);
            else if (_gameMenu is not null)
                Push(ctx, _gameMenu);
        }

        if (pauseNow) _keysHeldLastFrame.Add(pauseKey);
        else _keysHeldLastFrame.Remove(pauseKey);

        foreach (var panel in _stack.Reverse())
            panel.Update(ctx);

        return IsAnyPanelOpen;
    }

    public void Render(GameContext ctx, double frameTime, int screenW, int screenH)
    {
        foreach (var panel in _stack.Reverse())
            panel.Render(ctx, frameTime, screenW, screenH);
    }

    private void Push(GameContext ctx, IUIPanel panel)
    {
        _stack.Push(panel);
        panel.OnOpen(ctx);
        ApplyMouseState(ctx);
    }

    private void Pop(GameContext ctx)
    {
        if (_stack.Count == 0)
            return;

        var panel = _stack.Pop();
        panel.OnClose(ctx);
        ApplyMouseState(ctx);
    }

    private void ClearAll(GameContext ctx)
    {
        while (_stack.Count > 0)
        {
            var panel = _stack.Pop();
            panel.OnClose(ctx);
        }

        ApplyMouseState(ctx);
    }

    private static void ApplyMouseState(GameContext ctx)
    {
        ctx.Input.SetCursorMode(ctx.UI.IsAnyPanelOpen ? Silk.NET.Input.CursorMode.Normal : Silk.NET.Input.CursorMode.Raw);
    }
}
