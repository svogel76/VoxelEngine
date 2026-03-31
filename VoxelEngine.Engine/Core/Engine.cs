using VoxelEngine.Api.Input;
using VoxelEngine.Core.Debug.Commands;
using VoxelEngine.Core.Hud;
using VoxelEngine.Core.UI;
using VoxelEngine.Core.UI.Panels;
using VoxelEngine.Entity.Models;
using VoxelEngine.Persistence;
using VoxelEngine.Rendering;
using VoxelEngine.Rendering.Hud;
using VoxelEngine.World;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System.Diagnostics;
using System.Numerics;

namespace VoxelEngine.Core;

public class Engine : IDisposable
{
    private readonly IWindow        _window;
    private readonly EngineSettings _settings;
    private readonly double         _fixedDelta;
    private readonly IKeyBindings   _keyBindings;
    private readonly IGameMod       _game;

    private GL                   _gl           = null!;
    private IInputContext        _inputContext = null!;
    private GameContext          _context      = null!;
    private DebugOverlay         _debugOverlay = null!;
    private LocalFilePersistence _persistence  = null!;
    private PauseMenuPanel       _pauseMenu    = null!;
    private InventoryPanel       _inventoryPanel = null!;

    private readonly Stopwatch _frameTimer = new();

    private double _accumulator = 0.0;
    private double _fpsTimer    = 0.0;
    private double _fps         = 0.0;
    private int    _frameCount  = 0;
    private bool   _closed;

    // Spawn-Schutz: FlyMode temporaer aktiv bis der Chunk unter dem Spieler geladen ist,
    // damit der Spieler nicht durch ungeladene Chunks faellt.
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

    // Eingabe-Buffer fuer Debug-Konsole (Silk.NET-spezifisch)
    private string _consoleInput = "";

    public Engine(EngineSettings settings, IKeyBindings keyBindings, IGameMod game)
    {
        _settings   = settings;
        _keyBindings = keyBindings;
        _game       = game;
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

        // Zeichen-Input fuer Konsole
        input.Keyboard.KeyChar += (_, c) =>
        {
            if (_context?.Console.IsOpen == true)
                _consoleInput += c;
        };

        float aspectRatio      = (float)_settings.WindowWidth / _settings.WindowHeight;
        var (camX, camY, camZ) = _settings.CameraStartPosition;
        var entityModels = FileSystemEntityModelLibrary.LoadFromDirectory("Assets/Entities", _settings.EntityVoxelScale);
        var renderer  = new Renderer(_gl, _settings, entityModels);
        var world     = new World.World();
        var generator = new WorldGenerator(_settings);
        var playerStart = CreatePlayerStartPosition(generator, camX, camY, camZ);
        var player = new Player(playerStart, vitalsConfig: _settings.Vitals);
        player.SetInteractionReach(_settings.InteractionReach);
        var camera = new Camera(ToSilk(player.EyePosition), aspectRatio, _settings);

        _persistence = new LocalFilePersistence(_settings.SaveDirectory);
        _context = new GameContext(_settings, _keyBindings, world, player, camera, renderer, input, generator, entityModels, _persistence);

        // Spielerstand + Welt-Metadaten laden (falls vorhanden)
        var (savedPlayer, savedWorld) = _context.LoadGameStateAsync().GetAwaiter().GetResult();
        if (savedPlayer is not null && savedWorld is not null)
            _context.ApplyLoadedState(savedPlayer, savedWorld);

        // Spawn-Schutz: FlyMode aktivieren bis der Chunk unter dem Spieler geladen ist.
        // Verhindert, dass der Spieler nicht durch ungeladene Chunks faellt (Hintergrund-Generierung
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
        _context.Console.Register(new EntityCommand());

        _debugOverlay = new DebugOverlay(_gl, _context, _settings.WindowWidth, _settings.WindowHeight);

        var hotbarElement  = new HotbarHudElement();
        _context.HudRegistry.Register(hotbarElement);
        var hotbarRenderer = new HotbarHudRenderer(_gl, _settings, _settings.WindowWidth, _settings.WindowHeight);
        hotbarRenderer.Atlas = _context.Renderer.Atlas;
        _debugOverlay.HudManager.RegisterRenderer("hotbar", hotbarRenderer);

        var healthElement  = new HealthHudElement();
        _context.HudRegistry.Register(healthElement);
        var healthRenderer = new HealthHudRenderer(_gl, _settings, _settings.WindowWidth, _settings.WindowHeight);
        _debugOverlay.HudManager.RegisterRenderer("health", healthRenderer);

        var hungerElement  = new HungerHudElement();
        _context.HudRegistry.Register(hungerElement);
        var hungerRenderer = new HungerHudRenderer(_gl, _settings, _settings.WindowWidth, _settings.WindowHeight);
        _debugOverlay.HudManager.RegisterRenderer("hunger", hungerRenderer);

        _context.HudRegistry.LoadConfig("Assets/Hud/hud.json");

        _pauseMenu = new PauseMenuPanel();
        _pauseMenu.InitRenderer(_gl);
        _context.UI.Register(_pauseMenu, isGameMenu: true);

        var invFont     = new BitmapFont(_gl, "Assets/Fonts/font.png");
        var invText     = new TextRenderer(_gl, invFont, _settings.WindowWidth, _settings.WindowHeight);
        var invIcon     = new IconRenderer(_gl);
        _inventoryPanel = new InventoryPanel(invText, invIcon, _keyBindings.ToggleInventory);
        _inventoryPanel.Atlas = _context.Renderer.Atlas;
        _context.UI.Register(_inventoryPanel);

        _context.ChunkManager.PrimeInitialChunks(player.Position.X, player.Position.Z, _settings.InitialChunkLoadRadius);

        _game.Initialize(_context);
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

        bool f1Now = _context.Input.IsKeyPressed(_keyBindings.DebugConsole);
        if (f1Now && !_prevF1)
            _context.Console.Toggle();
        _prevF1 = f1Now;

        if (_context.Console.IsOpen)
        {
            _context.Input.ClearTransientMouseState();

            bool escNow = _context.Input.IsKeyPressed(_keyBindings.Pause);
            if (escNow && !_prevEscape)
            {
                _context.Console.Toggle();
                _consoleInput = "";
            }
            _prevEscape = escNow;

            bool bsNow = _context.Input.IsKeyPressed(Key.Backspace);
            if (bsNow && !_prevBackspace && _consoleInput.Length > 0)
                _consoleInput = _consoleInput[..^1];
            _prevBackspace = bsNow;

            bool enterNow = _context.Input.IsKeyPressed(Key.Enter);
            if (enterNow && !_prevEnter)
            {
                _context.Console.Execute(_consoleInput);
                _consoleInput = "";
            }
            _prevEnter = enterNow;

            bool upNow = _context.Input.IsKeyPressed(Key.Up);
            if (upNow && !_prevUp)
            {
                var result = _context.Console.NavigateHistoryUp(_consoleInput);
                if (result is not null)
                    _consoleInput = result;
            }
            _prevUp = upNow;

            bool downNow = _context.Input.IsKeyPressed(Key.Down);
            if (downNow && !_prevDown)
            {
                var result = _context.Console.NavigateHistoryDown();
                if (result is not null)
                    _consoleInput = result;
            }
            _prevDown = downNow;

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

        bool uiConsuming = _context.UI.Update(_context);

        if (_context.ShutdownRequested)
        {
            _window.Close();
            return;
        }

        if (uiConsuming)
        {
            _context.Input.ClearTransientMouseState();
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
        _context.Player.UpdateVitals(fixedDelta);
        _context.Camera.Position = ToSilk(_context.Player.EyePosition);
        _context.TargetedBlock = BlockRaycaster.Raycast(
            _context.World,
            _context.Player.EyePosition,
            ToNumerics(_context.Camera.Front),
            _context.Player.InteractionReach,
            ShouldIgnoreWaterForRaycast());
        _context.PlacementPreview = null;

        int scrollSteps = _context.Input.ConsumeScrollSteps();
        int hotbarDelta = MapHotbarScroll(scrollSteps);
        if (hotbarDelta != 0)
            _context.Player.CycleSelectedBlock(hotbarDelta);

        if (_settings.EnableHotbarNumberKeys)
        {
            Key[] numKeys = [_keyBindings.Hotbar1, _keyBindings.Hotbar2, _keyBindings.Hotbar3, _keyBindings.Hotbar4, _keyBindings.Hotbar5,
                             _keyBindings.Hotbar6, _keyBindings.Hotbar7, _keyBindings.Hotbar8, _keyBindings.Hotbar9];
            for (int i = 0; i < 9; i++)
            {
                bool numNow = _context.Input.IsKeyPressed(numKeys[i]);
                if (numNow && !_prevNum[i])
                    _context.Player.Inventory.SelectSlot(i);
                _prevNum[i] = numNow;
            }
        }

        if (_context.Input.ConsumeMouseClicks(_keyBindings.BlockBreak) > 0)
            TryBreakTargetedBlock();

        int rightClicks = _context.Input.ConsumeMouseClicks(_keyBindings.BlockPlace);
        if (rightClicks > 0)
            TryPlaceSelectedBlock();
        else
            _context.PlacementPreview = GetPlacementPreview();

        _context.ChunkManager.Update(_context.Player.Position.X, _context.Player.Position.Z);

        foreach (var (x, z) in _context.ChunkManager.UnloadedThisUpdate)
            _context.Renderer.RemoveChunkMesh(x, z);

        _context.EntityManager.Update(fixedDelta);
        _game.Update(fixedDelta);
    }

    private void Render(double frameTime)
    {
        _frameCount++;
        _fpsTimer += frameTime;

        if (_fpsTimer >= 0.5)
        {
            _fps          = _frameCount / _fpsTimer;
            _window.Title = _settings.ShowFps ? $"{_settings.Title} | FPS: {_fps:F0}  Chunks: {_context.World.LoadedChunkCount}" : _settings.Title;
            _fpsTimer     = 0.0;
            _frameCount   = 0;
        }

        _context.Renderer.UploadPendingMeshes(_context.ChunkManager);
        _context.Renderer.Render(_context.Camera, _context.Time, (float)frameTime, _context.EntityManager, _context.TargetedBlock, _context.PlacementPreview);
        _debugOverlay.Render(_settings.WindowWidth, _settings.WindowHeight, _fps, _consoleInput);
        _context.UI.Render(_context, frameTime, _settings.WindowWidth, _settings.WindowHeight);
        _game.Render(frameTime);
    }

    private void Close()
    {
        if (_closed)
            return;

        _closed = true;
        _context?.SaveGameStateAsync().GetAwaiter().GetResult();
        _game.Shutdown();
        _pauseMenu?.Dispose();
        _inventoryPanel?.Dispose();
        _debugOverlay?.Dispose();
        _context?.Dispose();
        _persistence?.Dispose();
        Console.WriteLine("Engine closing.");
    }

    public void Dispose()
    {
        _window.Closing -= Close;
        _window.Dispose();
        GC.SuppressFinalize(this);
    }

    private PlayerInput ReadPlayerInput()
    {
        float forward = 0f;
        float right = 0f;
        float up = 0f;

        if (_context.Input.IsKeyPressed(_keyBindings.MoveForward))
            forward += 1f;
        if (_context.Input.IsKeyPressed(_keyBindings.MoveBackward))
            forward -= 1f;
        if (_context.Input.IsKeyPressed(_keyBindings.MoveRight))
            right += 1f;
        if (_context.Input.IsKeyPressed(_keyBindings.MoveLeft))
            right -= 1f;
        bool jump = _context.Input.IsKeyPressed(_keyBindings.Jump);
        if (_context.Player.FlyMode && jump)
            up += 1f;
        if (_context.Input.IsKeyPressed(_keyBindings.Sneak))
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

    private int MapHotbarScroll(int scrollSteps)
    {
        if (scrollSteps == 0)
            return 0;

        int direction = _keyBindings.HotbarScrollUp == ScrollBinding.Up ? 1 : -1;
        return scrollSteps * direction;
    }

    private bool ShouldIgnoreWaterForRaycast()
    {
        var eyePosition = _context.Player.EyePosition;
        int x = (int)MathF.Floor(eyePosition.X);
        int y = (int)MathF.Floor(eyePosition.Y);
        int z = (int)MathF.Floor(eyePosition.Z);
        return _context.World.GetBlock(x, y, z) == BlockType.Water;
    }
}
