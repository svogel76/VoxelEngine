using Silk.NET.Input;
using Silk.NET.Maths;
using VoxelEngine.Core;

namespace VoxelEngine.Rendering;

public class Camera
{
    public Vector3D<float> Position { get; set; }

    public float Yaw   { get; set; } = -90f;
    public float Pitch { get; set; } = 0f;

    public float MovementSpeed    { get; }
    public float MouseSensitivity { get; }
    public float Fov              { get; }
    public float NearPlane        { get; }
    public float FarPlane         { get; }
    public bool  InvertMouseY     { get; }

    private Vector3D<float> _front;
    private Vector3D<float> _right;
    private Vector3D<float> _up;
    private float _aspectRatio;

    private const float Deg2Rad = MathF.PI / 180f;

    public Camera(Vector3D<float> startPosition, float aspectRatio, EngineSettings settings)
    {
        Position      = startPosition;
        _aspectRatio  = aspectRatio;

        MovementSpeed    = settings.MovementSpeed;
        MouseSensitivity = settings.MouseSensitivity;
        Fov              = settings.Fov;
        NearPlane        = settings.NearPlane;
        FarPlane         = settings.FarPlane;
        InvertMouseY     = settings.InvertMouseY;

        _front = Vector3D<float>.Zero;
        _right = Vector3D<float>.Zero;
        _up    = Vector3D<float>.Zero;

        UpdateVectors();
    }

    public Matrix4X4<float> ViewMatrix
        => Matrix4X4.CreateLookAt(Position, Position + _front, _up);

    public Matrix4X4<float> ProjectionMatrix
        => Matrix4X4.CreatePerspectiveFieldOfView(Fov * Deg2Rad, _aspectRatio, NearPlane, FarPlane);

    public void UpdateAspectRatio(float ratio) => _aspectRatio = ratio;

    public void ProcessKeyboard(IKeyboard keyboard, double deltaTime)
    {
        float velocity = MovementSpeed * (float)deltaTime;

        if (keyboard.IsKeyPressed(Key.W))          Position += _front * velocity;
        if (keyboard.IsKeyPressed(Key.S))          Position -= _front * velocity;
        if (keyboard.IsKeyPressed(Key.A))          Position -= _right * velocity;
        if (keyboard.IsKeyPressed(Key.D))          Position += _right * velocity;
        if (keyboard.IsKeyPressed(Key.Space))      Position += _up    * velocity;
        if (keyboard.IsKeyPressed(Key.ShiftLeft))  Position -= _up    * velocity;
    }

    public void ProcessMouseMovement(float deltaX, float deltaY)
    {
        float effectiveDeltaY = InvertMouseY ? -deltaY : deltaY;

        Yaw   += deltaX        * MouseSensitivity;
        Pitch -= effectiveDeltaY * MouseSensitivity;

        Pitch = Math.Clamp(Pitch, -89f, 89f);

        UpdateVectors();
    }

    private void UpdateVectors()
    {
        float yawRad   = Yaw   * Deg2Rad;
        float pitchRad = Pitch * Deg2Rad;

        var front = new Vector3D<float>(
            MathF.Cos(yawRad) * MathF.Cos(pitchRad),
            MathF.Sin(pitchRad),
            MathF.Sin(yawRad) * MathF.Cos(pitchRad)
        );

        _front = Vector3D.Normalize(front);
        _right = Vector3D.Normalize(Vector3D.Cross(_front, new Vector3D<float>(0f, 1f, 0f)));
        _up    = Vector3D.Normalize(Vector3D.Cross(_right, _front));
    }
}
