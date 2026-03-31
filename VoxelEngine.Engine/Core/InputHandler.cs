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

    public bool IsKeyPressed(Key key) => _keyboard.IsKeyPressed(ToSilkKey(key));

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

    public void SetCursorMode(CursorMode mode)
    {
        _mouse.Cursor.CursorMode = mode;
        if (mode == CursorMode.Raw)
            _firstMove = true;
    }

    private void OnMouseDown(IMouse mouse, Silk.NET.Input.MouseButton button)
    {
        var apiButton = ToApiMouseButton(button);
        _pendingClicks.TryGetValue(apiButton, out int count);
        _pendingClicks[apiButton] = count + 1;
    }

    private void OnScroll(IMouse mouse, ScrollWheel wheel)
    {
        _pendingScrollSteps += (int)MathF.Round(wheel.Y);
    }

    private static Silk.NET.Input.Key ToSilkKey(Key key) => key switch
    {
        Key.W => Silk.NET.Input.Key.W,
        Key.A => Silk.NET.Input.Key.A,
        Key.S => Silk.NET.Input.Key.S,
        Key.D => Silk.NET.Input.Key.D,
        Key.I => Silk.NET.Input.Key.I,
        Key.P => Silk.NET.Input.Key.P,
        Key.Space => Silk.NET.Input.Key.Space,
        Key.Tab => Silk.NET.Input.Key.Tab,
        Key.Escape => Silk.NET.Input.Key.Escape,
        Key.Enter => Silk.NET.Input.Key.Enter,
        Key.Backspace => Silk.NET.Input.Key.Backspace,
        Key.Up => Silk.NET.Input.Key.Up,
        Key.Down => Silk.NET.Input.Key.Down,
        Key.Left => Silk.NET.Input.Key.Left,
        Key.Right => Silk.NET.Input.Key.Right,
        Key.ShiftLeft => Silk.NET.Input.Key.ShiftLeft,
        Key.ControlLeft => Silk.NET.Input.Key.ControlLeft,
        Key.GraveAccent => Silk.NET.Input.Key.GraveAccent,
        Key.F1 => Silk.NET.Input.Key.F1,
        Key.F2 => Silk.NET.Input.Key.F2,
        Key.F3 => Silk.NET.Input.Key.F3,
        Key.F4 => Silk.NET.Input.Key.F4,
        Key.F5 => Silk.NET.Input.Key.F5,
        Key.F6 => Silk.NET.Input.Key.F6,
        Key.F7 => Silk.NET.Input.Key.F7,
        Key.F8 => Silk.NET.Input.Key.F8,
        Key.F9 => Silk.NET.Input.Key.F9,
        Key.Number1 => Silk.NET.Input.Key.Number1,
        Key.Number2 => Silk.NET.Input.Key.Number2,
        Key.Number3 => Silk.NET.Input.Key.Number3,
        Key.Number4 => Silk.NET.Input.Key.Number4,
        Key.Number5 => Silk.NET.Input.Key.Number5,
        Key.Number6 => Silk.NET.Input.Key.Number6,
        Key.Number7 => Silk.NET.Input.Key.Number7,
        Key.Number8 => Silk.NET.Input.Key.Number8,
        Key.Number9 => Silk.NET.Input.Key.Number9,
        _ => Silk.NET.Input.Key.Unknown,
    };

    private static MouseButton ToApiMouseButton(Silk.NET.Input.MouseButton button) => button switch
    {
        Silk.NET.Input.MouseButton.Left => MouseButton.Left,
        Silk.NET.Input.MouseButton.Right => MouseButton.Right,
        Silk.NET.Input.MouseButton.Middle => MouseButton.Middle,
        _ => MouseButton.Left,
    };
}
