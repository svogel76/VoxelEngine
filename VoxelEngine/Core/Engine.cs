using System.Diagnostics;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using VoxelEngine.Core.Debug.Commands;
using VoxelEngine.Rendering;
using VoxelEngine.World;

namespace VoxelEngine.Core;

public class Engine : IDisposable
{
    private readonly IWindow        _window;
    private readonly EngineSettings _settings;
    private readonly double         _fixedDelta;

    private GL            _gl           = null!;
    private IInputContext _inputContext = null!;
    private GameContext   _context      = null!;
    private DebugOverlay  _debugOverlay = null!;

    private readonly Stopwatch _frameTimer = new();

    private double _accumulator = 0.0;
    private double _fpsTimer    = 0.0;
    private double _fps         = 0.0;
    private int    _frameCount  = 0;

    // Edge detection
    private bool _prevF1        = false;
    private bool _prevEnter     = false;
    private bool _prevBackspace = false;
    private bool _prevEscape    = false;

    // Eingabe-Buffer für Debug-Konsole (Silk.NET-spezifisch)
    private string _consoleInput = "";

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
        var input     = new InputHandler(_inputContext);

        // Zeichen-Input für Konsole
        input.Keyboard.KeyChar += (_, c) =>
        {
            if (_context?.Console.IsOpen == true)
                _consoleInput += c;
        };

        float aspectRatio      = (float)_settings.WindowWidth / _settings.WindowHeight;
        var (camX, camY, camZ) = _settings.CameraStartPosition;
        var camera   = new Camera(new Vector3D<float>(camX, camY, camZ), aspectRatio, _settings);
        var renderer = new Renderer(_gl);
        var world    = new World.World();

        var generator = new WorldGenerator(_settings.Terrain);
        generator.GenerateTerrain(world, -4, 4, -4, 4);
        Console.WriteLine($"Loaded chunks: {world.LoadedChunkCount}");
        Console.WriteLine($"Block at (0,64,0): {world.GetBlock(0, 64, 0)}");

        renderer.BuildWorldMeshes(world);

        _context = new GameContext(_settings, world, camera, renderer, input);

        _context.Console.Register(new HelpCommand(_context.Console));
        _context.Console.Register(new PosCommand());
        _context.Console.Register(new TeleportCommand());
        _context.Console.Register(new WireframeCommand());
        _context.Console.Register(new ChunkInfoCommand());

        _debugOverlay = new DebugOverlay(_gl, _context, _settings.WindowWidth, _settings.WindowHeight);

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
        // F1 — Konsole öffnen/schließen
        bool f1Now = _context.Input.IsKeyPressed(Key.F1);
        if (f1Now && !_prevF1)
            _context.Console.Toggle();
        _prevF1 = f1Now;

        if (_context.Console.IsOpen)
        {
            // Escape — Konsole schließen
            bool escNow = _context.Input.IsKeyPressed(Key.Escape);
            if (escNow && !_prevEscape)
            {
                _context.Console.Toggle();
                _consoleInput = "";
            }
            _prevEscape = escNow;

            // Backspace — letztes Zeichen entfernen
            bool bsNow = _context.Input.IsKeyPressed(Key.Backspace);
            if (bsNow && !_prevBackspace && _consoleInput.Length > 0)
                _consoleInput = _consoleInput[..^1];
            _prevBackspace = bsNow;

            // Enter — Kommando ausführen
            bool enterNow = _context.Input.IsKeyPressed(Key.Enter);
            if (enterNow && !_prevEnter)
            {
                _context.Console.Execute(_consoleInput);
                _consoleInput = "";
            }
            _prevEnter = enterNow;

            return;
        }

        _prevEscape    = false;
        _prevBackspace = false;
        _prevEnter     = false;

        if (_context.Input.IsKeyPressed(Key.Escape))
        {
            _window.Close();
            return;
        }

        _context.Camera.ProcessKeyboard(_context.Input.Keyboard, fixedDelta);

        var (deltaX, deltaY) = _context.Input.GetMouseDelta();
        _context.Camera.ProcessMouseMovement(deltaX, deltaY);
    }

    private void Render(double interpolation, double frameTime)
    {
        _frameCount++;
        _fpsTimer += frameTime;

        if (_fpsTimer >= 0.5)
        {
            _fps        = _frameCount / _fpsTimer;
            _window.Title = $"{_settings.Title} | FPS: {_fps:F0}";
            _fpsTimer   = 0.0;
            _frameCount = 0;
        }

        _context.Renderer.Render(_context.Camera, interpolation);
        _debugOverlay.Render(_settings.WindowWidth, _settings.WindowHeight, _fps, _consoleInput);
    }

    private void Close()
    {
        // GL-Kontext ist hier noch aktiv — alle OpenGL-Ressourcen hier freigeben
        _debugOverlay?.Dispose();
        _context?.Dispose();
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
