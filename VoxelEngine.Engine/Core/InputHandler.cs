using Silk.NET.Input;

namespace VoxelEngine.Core;

public class InputHandler : IInputState
{
    private readonly IKeyboard _keyboard;
    private readonly IMouse    _mouse;

    private float _lastX;
    private float _lastY;
    private bool  _firstMove = true;
    private int _pendingLeftClicks;
    private int _pendingRightClicks;
    private int _pendingScrollSteps;

    public IKeyboard Keyboard => _keyboard;

    /// <summary>Aktuelle Mausposition in Bildschirmkoordinaten (Pixel).</summary>
    public (float X, float Y) MousePosition => (_mouse.Position.X, _mouse.Position.Y);

    public InputHandler(IInputContext input)
    {
        _keyboard = input.Keyboards[0];
        _mouse    = input.Mice[0];

        _mouse.Cursor.CursorMode = CursorMode.Raw;
        _mouse.MouseDown += OnMouseDown;
        _mouse.Scroll += OnScroll;
    }

    public (float deltaX, float deltaY) GetMouseDelta()
    {
        float currentX = _mouse.Position.X;
        float currentY = _mouse.Position.Y;

        if (_firstMove)
        {
            _lastX     = currentX;
            _lastY     = currentY;
            _firstMove = false;
            return (0f, 0f);
        }

        float deltaX = currentX - _lastX;
        float deltaY = currentY - _lastY;

        _lastX = currentX;
        _lastY = currentY;

        return (deltaX, deltaY);
    }

    public bool IsKeyPressed(Key key) => _keyboard.IsKeyPressed(key);

    public int ConsumeLeftClicks()
    {
        int value = _pendingLeftClicks;
        _pendingLeftClicks = 0;
        return value;
    }

    public int ConsumeRightClicks()
    {
        int value = _pendingRightClicks;
        _pendingRightClicks = 0;
        return value;
    }

    public int ConsumeScrollSteps()
    {
        int value = _pendingScrollSteps;
        _pendingScrollSteps = 0;
        return value;
    }

    public void ClearTransientMouseState()
    {
        _pendingLeftClicks = 0;
        _pendingRightClicks = 0;
        _pendingScrollSteps = 0;
    }

    private void OnMouseDown(IMouse mouse, MouseButton button)
    {
        if (button == MouseButton.Left)
            _pendingLeftClicks++;
        else if (button == MouseButton.Right)
            _pendingRightClicks++;
    }

    /// <summary>
    /// Setzt den Cursor-Modus der Maus (Raw = versteckt/gesperrt, Normal = sichtbar).
    /// Wird vom UIStateManager aufgerufen wenn sich der Panel-Stack ändert.
    /// </summary>
    public void SetCursorMode(CursorMode mode)
    {
        _mouse.Cursor.CursorMode = mode;
        // Bei Wechsel zurück zu Raw: erste Bewegung ignorieren (kein Sprung)
        if (mode == CursorMode.Raw)
            _firstMove = true;
    }

    private void OnScroll(IMouse mouse, ScrollWheel wheel)
    {
        _pendingScrollSteps += (int)MathF.Round(wheel.Y);
    }
}
