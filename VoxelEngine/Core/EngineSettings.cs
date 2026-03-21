using VoxelEngine.World;

namespace VoxelEngine.Core;

public class EngineSettings
{
    // Window
    public string Title          { get; init; } = "VoxelEngine";
    public int    WindowWidth    { get; init; } = 1280;
    public int    WindowHeight   { get; init; } = 720;
    public bool   VSync          { get; init; } = true;

    // Loop
    public double TargetUPS      { get; init; } = 60.0;

    /// <summary>0 = unbegrenzt, VSync bestimmt Obergrenze</summary>
    public double TargetFPS      { get; init; } = 0.0;

    // Camera
    public float MovementSpeed    { get; init; } = 5.0f;
    public float MouseSensitivity { get; init; } = 0.1f;
    public float Fov              { get; init; } = 75.0f;
    public float NearPlane        { get; init; } = 0.1f;
    public float FarPlane         { get; init; } = 500.0f;
    public bool  InvertMouseY     { get; init; } = false;

    // Camera start position
    public (float X, float Y, float Z) CameraStartPosition { get; init; } = (0f, 80f, 30f);

    // Terrain
    /// <summary>Wird später durch BiomeDefinitions[] ersetzt</summary>
    public NoiseSettings Terrain { get; init; } = new NoiseSettings();
}
