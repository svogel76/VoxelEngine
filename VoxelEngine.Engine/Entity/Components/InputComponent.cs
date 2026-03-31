using System.Numerics;
using VoxelEngine.Api;
using VoxelEngine.Api.Entity;
using VoxelEngine.Rendering;
using VoxelEngine.World;

namespace VoxelEngine.Entity.Components;

/// <summary>
/// Liest Tastatur-Eingaben und überträgt sie auf PhysicsComponent (Bewegung, Sprung, Fly).
/// Benötigt PhysicsComponent am selben Entity.
/// </summary>
public sealed class InputComponent : IComponent
{
    private readonly IInputState   _input;
    private readonly IKeyBindings  _keyBindings;
    private readonly Camera        _camera;
    private readonly float         _moveSpeed;
    private readonly float         _jumpVelocity;

    public string ComponentId => "input";

    public InputComponent(
        IInputState  input,
        IKeyBindings keyBindings,
        Camera       camera,
        float        moveSpeed,
        float        jumpVelocity)
    {
        _input        = input        ?? throw new ArgumentNullException(nameof(input));
        _keyBindings  = keyBindings  ?? throw new ArgumentNullException(nameof(keyBindings));
        _camera       = camera       ?? throw new ArgumentNullException(nameof(camera));
        _moveSpeed    = moveSpeed;
        _jumpVelocity = jumpVelocity;
    }

    public void Update(IEntity iEntity, IModContext context, double deltaTime)
    {
        if (iEntity is not Entity entity) return;

        var phys = entity.GetComponent<PhysicsComponent>();
        if (phys is null) return;

        float forward = 0f;
        float right   = 0f;
        float up      = 0f;
        bool  jump    = false;

        if (_input.IsKeyPressed(_keyBindings.MoveForward))  forward += 1f;
        if (_input.IsKeyPressed(_keyBindings.MoveBackward)) forward -= 1f;
        if (_input.IsKeyPressed(_keyBindings.MoveLeft))     right   -= 1f;
        if (_input.IsKeyPressed(_keyBindings.MoveRight))    right   += 1f;

        if (phys.FlyMode)
        {
            if (_input.IsKeyPressed(_keyBindings.Jump))  up += 1f;
            if (_input.IsKeyPressed(_keyBindings.Sneak)) up -= 1f;
        }
        else
        {
            jump = _input.IsKeyPressed(_keyBindings.Jump);
        }

        var lookForward = new Vector3(_camera.Front.X, _camera.Front.Y, _camera.Front.Z);
        var lookRight   = new Vector3(_camera.Right.X, _camera.Right.Y, _camera.Right.Z);
        var lookUp      = new Vector3(_camera.Up.X,    _camera.Up.Y,    _camera.Up.Z);

        phys.ProcessPlayerInput(
            entity,
            new PlayerInput(forward, right, up, jump),
            lookForward,
            lookRight,
            lookUp,
            _moveSpeed,
            deltaTime);

        if (jump && phys.IsOnGround)
            phys.ApplyJumpVelocity(entity, _jumpVelocity);
    }
}
