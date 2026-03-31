using Silk.NET.Input;

namespace VoxelEngine.Core;

public class InputHandler : IInputState
{
    private readonly IKeyboard _keyboard;
    private readonly IMouse    _mouse;

    private float _lastX;
    private float _lastY;
    private bool  _firstMove = true;
    private readonly Dictionary<MouseButton, int> _pendingClicks = new();
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

    public int ConsumeMouseClicks(MouseButton button)
    {
        if (!_pendingClicks.TryGetValue(button, out int value))
            return 0;

        _pendingClicks[button] = 0;
        return value;
    }

    public int ConsumeLeftClicks() => ConsumeMouseClicks(MouseButton.Left);

    public int ConsumeRightClicks() => ConsumeMouseClicks(MouseButton.Right);

    public int ConsumeScrollSteps()
    {
        int value = _pendingScrollSteps;
        _pendingScrollSteps = 0;
        return value;
    }

    public void ClearTransientMouseState()
    {
        foreach (var button in _pendingClicks.Keys.ToArray())
            _pendingClicks[button] = 0;

        _pendingScrollSteps = 0;
    }

    private void OnMouseDown(IMouse mouse, MouseButton button)
    {
        _pendingClicks.TryGetValue(button, out int count);
        _pendingClicks[button] = count + 1;
    }

    public void SetCursorMode(CursorMode mode)
    {
        _mouse.Cursor.CursorMode = mode;
        if (mode == CursorMode.Raw)
            _firstMove = true;
    }

    private void OnScroll(IMouse mouse, ScrollWheel wheel)
    {
        _pendingScrollSteps += (int)MathF.Round(wheel.Y);
    }
}
