using Silk.NET.Input;

namespace VoxelEngine.Core.UI;

/// <summary>
/// Stack-basierter UI-Zustandsautomat.
///
/// Regeln:
/// - Panel-Taste gedrückt + Panel liegt oben  → Panel schließen (pop)
/// - Panel-Taste gedrückt + Panel liegt nicht oben → Panel öffnen (push)
/// - Escape + Stack nicht leer                 → alle Panels schließen (clear)
/// - Escape + Stack leer                       → Spielmenü öffnen (falls registriert)
/// - Solange ein Panel offen ist: Maus sichtbar, Bewegungs-/Kamera-Input gesperrt
/// </summary>
public class UIStateManager
{
    private readonly Dictionary<string, IUIPanel> _registered = new();
    private readonly Stack<IUIPanel>              _stack      = new();
    private          IUIPanel?                    _gameMenu;

    // Edge-Detection
    private readonly HashSet<Key> _keysHeldLastFrame = new();

    /// <summary>Gibt an ob mindestens ein Panel geöffnet ist.</summary>
    public bool IsAnyPanelOpen => _stack.Count > 0;

    /// <summary>Registriert ein Panel. Das Spielmenü-Panel wird über <paramref name="isGameMenu"/> markiert.</summary>
    public void Register(IUIPanel panel, bool isGameMenu = false)
    {
        _registered[panel.Id] = panel;
        if (isGameMenu)
            _gameMenu = panel;
    }

    /// <summary>
    /// Schließt das oberste Panel auf dem Stack von innen heraus
    /// (z.B. "Weiterspielen"-Button im PauseMenu).
    /// </summary>
    public void CloseTop(GameContext ctx)
    {
        Pop(ctx);
    }

    /// <summary>
    /// Wird jeden Tick aufgerufen. Verarbeitet Tasteneingaben, ruft Update auf offene
    /// Panels, und gibt true zurück wenn Gameplay-Input pausiert werden soll.
    /// </summary>
    public bool Update(GameContext ctx)
    {
        // --- Taste: gedrückte Panel-Tasten auswerten ---
        foreach (var panel in _registered.Values)
        {
            if (panel.ToggleKey is not { } key)
                continue;

            bool heldNow     = ctx.Input.IsKeyPressed(key);
            bool heldPrev    = _keysHeldLastFrame.Contains(key);
            bool justPressed = heldNow && !heldPrev;

            if (heldNow) _keysHeldLastFrame.Add(key);
            else         _keysHeldLastFrame.Remove(key);

            if (!justPressed)
                continue;

            if (_stack.Count > 0 && _stack.Peek() == panel)
                Pop(ctx);
            else
                Push(ctx, panel);
        }

        // --- Escape ---
        bool escNow  = ctx.Input.IsKeyPressed(Key.Escape);
        bool escPrev = _keysHeldLastFrame.Contains(Key.Escape);
        if (escNow && !escPrev)
        {
            if (_stack.Count > 0)
                ClearAll(ctx);
            else if (_gameMenu is not null)
                Push(ctx, _gameMenu);
        }
        if (escNow) _keysHeldLastFrame.Add(Key.Escape);
        else        _keysHeldLastFrame.Remove(Key.Escape);

        // --- Offene Panels ticken ---
        foreach (var panel in _stack.Reverse())
            panel.Update(ctx);

        return IsAnyPanelOpen;
    }

    /// <summary>
    /// Rendert alle offenen Panels (bottom → top).
    /// screenW/H werden von Engine.Render durchgereicht.
    /// </summary>
    public void Render(GameContext ctx, double frameTime, int screenW, int screenH)
    {
        foreach (var panel in _stack.Reverse())
            panel.Render(ctx, frameTime, screenW, screenH);
    }

    // ── interne Hilfsmethoden ───────────────────────────────────────────────

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
        ctx.Input.SetCursorMode(
            ctx.UI.IsAnyPanelOpen ? CursorMode.Normal : CursorMode.Raw);
    }
}
