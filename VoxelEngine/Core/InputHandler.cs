using Silk.NET.Input;

namespace VoxelEngine.Core;

public class InputHandler
{
    private readonly IKeyboard _keyboard;
    private readonly IMouse    _mouse;

    private float _lastX;
    private float _lastY;
    private bool  _firstMove = true;

    public IKeyboard Keyboard => _keyboard;

    public InputHandler(IInputContext input)
    {
        _keyboard = input.Keyboards[0];
        _mouse    = input.Mice[0];

        _mouse.Cursor.CursorMode = CursorMode.Raw;
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
}
