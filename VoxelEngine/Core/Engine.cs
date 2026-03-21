using System.Diagnostics;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using VoxelEngine.Rendering;
using VoxelEngine.World;

namespace VoxelEngine.Core;

public class Engine : IDisposable
{
    private readonly IWindow       _window;
    private readonly EngineSettings _settings;
    private readonly double        _fixedDelta;

    private GL            _gl           = null!;
    private IInputContext _inputContext = null!;
    private InputHandler  _input        = null!;
    private Camera        _camera       = null!;
    private Renderer      _renderer     = null!;
    private World.World   _world        = null!;

    private readonly Stopwatch _frameTimer = new();

    private double _accumulator = 0.0;
    private double _fpsTimer   = 0.0;
    private int    _frameCount = 0;

    public Engine(EngineSettings settings)
    {
        _settings   = settings;
        _fixedDelta = 1.0 / settings.TargetUPS;

        var options = WindowOptions.Default with
        {
            Size             = new Vector2D<int>(settings.WindowWidth, settings.WindowHeight),
            Title            = settings.Title,
            VSync            = settings.VSync,
            FramesPerSecond  = 0,
            UpdatesPerSecond = settings.TargetUPS
        };

        _window = Window.Create(options);

        _window.Load    += Load;
        _window.Update  += OnUpdate;
        _window.Render  += OnRender;
        _window.Closing += Close;
    }

    public void Run() => _window.Run();

    private void Load()
    {
        _gl = GL.GetApi(_window);

        _inputContext = _window.CreateInput();
        _input        = new InputHandler(_inputContext);

        float aspectRatio = (float)_settings.WindowWidth / _settings.WindowHeight;
        var (camX, camY, camZ) = _settings.CameraStartPosition;
        _camera = new Camera(new Vector3D<float>(camX, camY, camZ), aspectRatio, _settings);

        _renderer = new Renderer(_gl);

        _world = new World.World();
        var generator = new WorldGenerator(_settings.Terrain);
        generator.GenerateTerrain(_world, -4, 4, -4, 4);
        Console.WriteLine($"Loaded chunks: {_world.LoadedChunkCount}");
        Console.WriteLine($"Block at (0,64,0): {_world.GetBlock(0, 64, 0)}");

        _renderer.BuildWorldMeshes(_world);

        _frameTimer.Start();
    }

    private void OnUpdate(double deltaTime)
    {
        _accumulator += deltaTime;

        while (_accumulator >= _fixedDelta)
        {
            Update(_fixedDelta);
            _accumulator -= _fixedDelta;
        }
    }

    private void OnRender(double deltaTime)
    {
        double interpolation = _accumulator / _fixedDelta;
        Render(interpolation, deltaTime);
    }

    private void Update(double fixedDelta)
    {
        if (_input.IsKeyPressed(Key.Escape))
        {
            _window.Close();
            return;
        }

        _camera.ProcessKeyboard(_input.Keyboard, fixedDelta);

        var (deltaX, deltaY) = _input.GetMouseDelta();
        _camera.ProcessMouseMovement(deltaX, deltaY);
    }

    private void Render(double interpolation, double frameTime)
    {
        _frameCount++;
        _fpsTimer += frameTime;

        if (_fpsTimer >= 0.5)
        {
            double fps = _frameCount / _fpsTimer;
            _window.Title = $"{_settings.Title} | FPS: {fps:F0}";
            _fpsTimer   = 0.0;
            _frameCount = 0;
        }

        _renderer.Render(_camera, interpolation);
    }

    private void Close()
    {
        // GL-Kontext ist hier noch aktiv — alle OpenGL-Ressourcen hier freigeben
        _renderer?.Dispose();
        _inputContext?.Dispose();
        Console.WriteLine("Engine closing.");
    }

    public void Dispose()
    {
        // Closing-Handler abmelden damit _window.Dispose() kein zweites Close() auslöst
        _window.Closing -= Close;
        _window.Dispose();
        GC.SuppressFinalize(this);
    }
}
