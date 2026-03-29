using System.Diagnostics;
using System.Numerics;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using VoxelEngine.Core.Debug.Commands;
using VoxelEngine.Core.Hud;
using VoxelEngine.Persistence;
using VoxelEngine.Rendering;
using VoxelEngine.Rendering.Hud;
using VoxelEngine.World;

namespace VoxelEngine.Core;

public class Engine : IDisposable
{
    private readonly IWindow        _window;
    private readonly EngineSettings _settings;
    private readonly double         _fixedDelta;

    private GL                   _gl           = null!;
    private IInputContext        _inputContext = null!;
    private GameContext          _context      = null!;
    private DebugOverlay         _debugOverlay = null!;
    private LocalFilePersistence _persistence  = null!;

    private readonly Stopwatch _frameTimer = new();

    private double _accumulator = 0.0;
    private double _fpsTimer    = 0.0;
    private double _fps         = 0.0;
    private int    _frameCount  = 0;
    private bool   _closed;

    // Spawn-Schutz: FlyMode temporär aktiv bis der Chunk unter dem Spieler geladen ist,
    // damit der Spieler nicht durch ungeladene Chunks fällt.
    private bool _waitingForChunkLoad = false;
    private bool _spawnFlyMode        = false;

    // Edge detection
    private bool _prevF1        = false;
    private bool _prevEnter     = false;
    private bool _prevBackspace = false;
    private bool _prevEscape    = false;
    private readonly bool[] _prevNum = new bool[9];
    private bool _prevUp        = false;
    private bool _prevDown      = false;
    private bool _prevTab       = false;

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
        var renderer  = new Renderer(_gl, _settings);
        var world     = new World.World();
        var generator = new WorldGenerator(_settings);
        var playerStart = CreatePlayerStartPosition(generator, camX, camY, camZ);
        var player = new Player(playerStart);
        player.SetInteractionReach(_settings.InteractionReach);
        var camera = new Camera(ToSilk(player.EyePosition), aspectRatio, _settings);

        _persistence = new LocalFilePersistence(_settings.SaveDirectory);
        _context = new GameContext(_settings, world, player, camera, renderer, input, generator, _persistence);

        // Spielerstand + Welt-Metadaten laden (falls vorhanden)
        var (savedPlayer, savedWorld) = _context.LoadGameStateAsync().GetAwaiter().GetResult();
        if (savedPlayer is not null && savedWorld is not null)
            _context.ApplyLoadedState(savedPlayer, savedWorld);

        // Spawn-Schutz: FlyMode aktivieren bis der Chunk unter dem Spieler geladen ist.
        // Verhindert, dass der Spieler durch ungeladene Chunks fällt (Hintergrund-Generierung
        // ist asynchron und kann beim ersten Update() noch nicht fertig sein).
        _spawnFlyMode = _context.Player.FlyMode;
        _context.Player.SetFlyMode(true);
        _waitingForChunkLoad = true;

        _context.Console.Register(new HelpCommand(_context.Console));
        _context.Console.Register(new PosCommand());
        _context.Console.Register(new TeleportCommand());
        _context.Console.Register(new FlyCommand());
        _context.Console.Register(new ReachCommand());
        _context.Console.Register(new ClimateCommand());
        _context.Console.Register(new WireframeCommand());
        _context.Console.Register(new ChunkInfoCommand());
        _context.Console.Register(new RenderDistanceCommand());
        _context.Console.Register(new SkyboxCommand());
        _context.Console.Register(new TimeCommand());
        _context.Console.Register(new FogCommand());
        _context.Console.Register(new HudCommand(_context.HudRegistry));

        _debugOverlay = new DebugOverlay(_gl, _context, _settings.WindowWidth, _settings.WindowHeight);

        // Hotbar-Element im HudRegistry registrieren + Renderer einbinden
        var hotbarElement  = new HotbarHudElement();
        _context.HudRegistry.Register(hotbarElement);
        var hotbarRenderer = new HotbarHudRenderer(_gl, _settings, _settings.WindowWidth, _settings.WindowHeight);
        _debugOverlay.HudManager.RegisterRenderer("hotbar", hotbarRenderer);

        // hud.json laden (nach allen Registrierungen)
        _context.HudRegistry.LoadConfig("Assets/Hud/hud.json");

        _context.ChunkManager.PrimeInitialChunks(player.Position.X, player.Position.Z, _settings.InitialChunkLoadRadius);

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
        Render(deltaTime);
    }

    private void Update(double fixedDelta)
    {
        // Spawn-Schutz: warten bis der Chunk unter dem Spieler tatsächlich geladen ist.
        if (_waitingForChunkLoad)
        {
            int cx = (int)Math.Floor(_context.Player.Position.X / Chunk.Width);
            int cz = (int)Math.Floor(_context.Player.Position.Z / Chunk.Depth);
            if (_context.World.GetChunk(cx, cz) is not null)
            {
                _context.Player.SetFlyMode(_spawnFlyMode);
                if (!_spawnFlyMode)
                    _context.Player.SyncPhysics(_context.World);
                _waitingForChunkLoad = false;
            }
        }

        // F1 — Konsole öffnen/schließen
        bool f1Now = _context.Input.IsKeyPressed(Key.F1);
        if (f1Now && !_prevF1)
            _context.Console.Toggle();
        _prevF1 = f1Now;

        if (_context.Console.IsOpen)
        {
            _context.Input.ClearTransientMouseState();

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

            // Pfeil hoch — History rückwärts
            bool upNow = _context.Input.IsKeyPressed(Key.Up);
            if (upNow && !_prevUp)
            {
                var result = _context.Console.NavigateHistoryUp(_consoleInput);
                if (result is not null)
                    _consoleInput = result;
            }
            _prevUp = upNow;

            // Pfeil runter — History vorwärts
            bool downNow = _context.Input.IsKeyPressed(Key.Down);
            if (downNow && !_prevDown)
            {
                var result = _context.Console.NavigateHistoryDown();
                if (result is not null)
                    _consoleInput = result;
            }
            _prevDown = downNow;

            // Tab — Autocomplete
            bool tabNow = _context.Input.IsKeyPressed(Key.Tab);
            if (tabNow && !_prevTab)
            {
                var result = _context.Console.Autocomplete(_consoleInput);
                if (result is not null)
                    _consoleInput = result;
            }
            _prevTab = tabNow;

            return;
        }

        _prevEscape    = false;
        _prevBackspace = false;
        _prevEnter     = false;
        _prevUp        = false;
        _prevDown      = false;
        _prevTab       = false;

        if (_context.Input.IsKeyPressed(Key.Escape))
        {
            _window.Close();
            return;
        }

        _context.Time.Update(fixedDelta);

        var (deltaX, deltaY) = _context.Input.GetMouseDelta();
        _context.Camera.ProcessMouseMovement(deltaX, deltaY);
        _context.Player.ProcessInput(
            ReadPlayerInput(),
            ToNumerics(_context.Camera.Front),
            ToNumerics(_context.Camera.Right),
            ToNumerics(_context.Camera.Up),
            _context.Camera.MovementSpeed,
            BuildPhysicsSettings(),
            _context.World,
            fixedDelta);
        _context.Camera.Position = ToSilk(_context.Player.EyePosition);
        _context.TargetedBlock = BlockRaycaster.Raycast(
            _context.World,
            _context.Player.EyePosition,
            ToNumerics(_context.Camera.Front),
            _context.Player.InteractionReach,
            ShouldIgnoreWaterForRaycast());
        _context.PlacementPreview = null;

        int scrollSteps = _context.Input.ConsumeScrollSteps();
        if (scrollSteps != 0)
            _context.Player.CycleSelectedBlock(scrollSteps);

        // Zifferntasten 1-9 für direkten Hotbar-Slot-Auswahl
        if (_settings.EnableHotbarNumberKeys)
        {
            Key[] numKeys = [Key.Number1, Key.Number2, Key.Number3, Key.Number4, Key.Number5,
                             Key.Number6, Key.Number7, Key.Number8, Key.Number9];
            for (int i = 0; i < 9; i++)
            {
                bool numNow = _context.Input.IsKeyPressed(numKeys[i]);
                if (numNow && !_prevNum[i])
                    _context.Player.Inventory.SelectSlot(i);
                _prevNum[i] = numNow;
            }
        }

        if (_context.Input.ConsumeLeftClicks() > 0)
            TryBreakTargetedBlock();

        int rightClicks = _context.Input.ConsumeRightClicks();
        if (rightClicks > 0)
            TryPlaceSelectedBlock();
        else
            _context.PlacementPreview = GetPlacementPreview();

        // Chunk-Manager: Laden/Entladen
        _context.ChunkManager.Update(_context.Player.Position.X, _context.Player.Position.Z);

        // Meshes für entladene Chunks entfernen
        foreach (var (x, z) in _context.ChunkManager.UnloadedThisUpdate)
            _context.Renderer.RemoveChunkMesh(x, z);

    }

    private void Render(double frameTime)
    {
        _frameCount++;
        _fpsTimer += frameTime;

        if (_fpsTimer >= 0.5)
        {
            _fps          = _frameCount / _fpsTimer;
            _window.Title = $"{_settings.Title} | FPS: {_fps:F0}  Chunks: {_context.World.LoadedChunkCount}";
            _fpsTimer     = 0.0;
            _frameCount   = 0;
        }

        _context.Renderer.UploadPendingMeshes(_context.ChunkManager);
        _context.Renderer.Render(_context.Camera, _context.Time, (float)frameTime, _context.TargetedBlock, _context.PlacementPreview);
        _debugOverlay.Render(_settings.WindowWidth, _settings.WindowHeight, _fps, _consoleInput);
    }

    private void Close()
    {
        if (_closed)
            return;

        _closed = true;
        // Spielerstand + Welt-Metadaten speichern
        _context?.SaveGameStateAsync().GetAwaiter().GetResult();
        // GL-Kontext ist hier noch aktiv — alle OpenGL-Ressourcen hier freigeben
        // _inputContext wird von Silk.NET/GLFW intern disposed wenn das Fenster schließt —
        // manuelles Dispose hier würde eine ObjectDisposedException auslösen.
        _debugOverlay?.Dispose();
        _context?.Dispose();
        _persistence?.Dispose();
        Console.WriteLine("Engine closing.");
    }

    public void Dispose()
    {
        // Closing-Handler abmelden damit _window.Dispose() kein zweites Close() auslöst
        _window.Closing -= Close;
        _window.Dispose();
        GC.SuppressFinalize(this);
    }

    private PlayerInput ReadPlayerInput()
    {
        float forward = 0f;
        float right = 0f;
        float up = 0f;

        if (_context.Input.IsKeyPressed(Key.W))
            forward += 1f;
        if (_context.Input.IsKeyPressed(Key.S))
            forward -= 1f;
        if (_context.Input.IsKeyPressed(Key.D))
            right += 1f;
        if (_context.Input.IsKeyPressed(Key.A))
            right -= 1f;
        bool jump = _context.Input.IsKeyPressed(Key.Space);
        if (_context.Player.FlyMode && jump)
            up += 1f;
        if (_context.Input.IsKeyPressed(Key.ShiftLeft))
            up -= 1f;

        return new PlayerInput(forward, right, up, jump);
    }

    private static Vector3 CreatePlayerStartPosition(WorldGenerator generator, float startX, float startEyeY, float startZ)
    {
        int surfaceHeight = generator.GetSurfaceHeight((int)MathF.Floor(startX), (int)MathF.Floor(startZ));
        float minFeetY = surfaceHeight + Player.SpawnClearance;
        float configuredFeetY = startEyeY - Player.EyeHeight;
        return new Vector3(startX, MathF.Max(configuredFeetY, minFeetY), startZ);
    }

    private static Vector3 ToNumerics(Vector3D<float> vector)
        => new(vector.X, vector.Y, vector.Z);

    private static Vector3D<float> ToSilk(Vector3 vector)
        => new(vector.X, vector.Y, vector.Z);

    private PlayerPhysicsSettings BuildPhysicsSettings()
        => new(
            _settings.Gravity,
            _settings.MaxFallSpeed,
            _settings.JumpVelocity,
            _settings.StepHeight,
            _settings.StepUpMaxVisualDrop,
            _settings.StepUpSmoothingSpeed,
            _settings.EnableStepUp);

    private void TryBreakTargetedBlock()
    {
        if (_context.TargetedBlock is not { } hit)
            return;

        if (hit.BlockType == BlockType.Water)
            return;

        var removedBlock = _context.World.GetBlock(hit.BlockPosition.X, hit.BlockPosition.Y, hit.BlockPosition.Z);
        _context.World.SetBlock(hit.BlockPosition.X, hit.BlockPosition.Y, hit.BlockPosition.Z, BlockType.Air);
        _context.ChunkManager.EnqueueBlockUpdate(hit.BlockPosition.X, hit.BlockPosition.Z);
        _context.Player.Inventory.TryAdd(removedBlock);
        _context.TargetedBlock = BlockRaycaster.Raycast(
            _context.World,
            _context.Player.EyePosition,
            ToNumerics(_context.Camera.Front),
            _context.Player.InteractionReach,
            ShouldIgnoreWaterForRaycast());
    }

    private void TryPlaceSelectedBlock()
    {
        if (_context.TargetedBlock is not { } hit)
            return;

        int slot = _context.Player.Inventory.SelectedSlot;
        var stack = _context.Player.Inventory.Hotbar[slot];
        if (stack is null)
            return;

        BlockPosition placement = GetPlacementTarget(hit);
        if (placement.Y < 0 || placement.Y >= Chunk.Height)
            return;

        if (_context.Player.WouldIntersectBlock(placement))
            return;

        if (!BlockRegistry.IsReplaceable(_context.World.GetBlock(placement.X, placement.Y, placement.Z)))
            return;

        _context.World.SetBlock(placement.X, placement.Y, placement.Z, stack.BlockType);
        _context.ChunkManager.EnqueueBlockUpdate(placement.X, placement.Z);
        _context.Player.Inventory.TryRemove(slot, 1);
        _context.TargetedBlock = BlockRaycaster.Raycast(
            _context.World,
            _context.Player.EyePosition,
            ToNumerics(_context.Camera.Front),
            _context.Player.InteractionReach,
            ShouldIgnoreWaterForRaycast());
        _context.PlacementPreview = null;
    }

    private BlockPlacementPreview? GetPlacementPreview()
    {
        if (_context.TargetedBlock is not { } hit)
            return null;

        var stack = _context.Player.Inventory.Hotbar[_context.Player.Inventory.SelectedSlot];
        if (stack is null)
            return null;

        BlockPosition placement = GetPlacementTarget(hit);
        if (placement.Y < 0 || placement.Y >= Chunk.Height)
            return null;

        if (_context.Player.WouldIntersectBlock(placement))
            return null;

        if (!BlockRegistry.IsReplaceable(_context.World.GetBlock(placement.X, placement.Y, placement.Z)))
            return null;

        return new BlockPlacementPreview(placement, stack.BlockType);
    }

    private static BlockPosition GetPlacementTarget(BlockRaycastHit hit) =>
        BlockRegistry.IsReplaceable(hit.BlockType)
            ? hit.BlockPosition
            : hit.PlacementPosition;

    private bool ShouldIgnoreWaterForRaycast()
    {
        var eyePosition = _context.Player.EyePosition;
        int x = (int)MathF.Floor(eyePosition.X);
        int y = (int)MathF.Floor(eyePosition.Y);
        int z = (int)MathF.Floor(eyePosition.Z);
        return _context.World.GetBlock(x, y, z) == BlockType.Water;
    }
}
