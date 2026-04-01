using VoxelEngine.Api.Input;
using VoxelEngine.Core.Debug.Commands;
using VoxelEngine.Core.Hud;
using VoxelEngine.Core.UI;
using VoxelEngine.Core.UI.Panels;
using VoxelEngine.Entity;
using VoxelEngine.Entity.Components;
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
    private const float SpawnClearance = 2f;

    private readonly IWindow        _window;
    private readonly EngineSettings _settings;
    private readonly double         _fixedDelta;
    private readonly IKeyBindings   _keyBindings;
    private readonly IReadOnlyList<IGameMod> _mods;
    private readonly string _primaryAssetBasePath;

    private GL                   _gl             = null!;
    private IInputContext        _inputContext   = null!;
    private GameContext          _context        = null!;
    private readonly List<EngineModContext> _modContexts = new();
    private DebugOverlay         _debugOverlay   = null!;
    private LocalFilePersistence _persistence    = null!;
    private PauseMenuPanel       _pauseMenu      = null!;
    private InventoryPanel       _inventoryPanel = null!;

    private readonly Stopwatch _frameTimer = new();

    private float  _interactionReach = 5f;
    private double _accumulator      = 0.0;
    private double _fpsTimer         = 0.0;
    private double _fps              = 0.0;
    private int    _frameCount       = 0;
    private bool   _closed;

    // Spawn-Schutz
    private bool _waitingForChunkLoad = false;
    private bool _spawnFlyMode        = false;
    private Vector3 _playerSpawnPoint;

    // Edge detection
    private bool _prevF1        = false;
    private bool _prevEnter     = false;
    private bool _prevBackspace = false;
    private bool _prevEscape    = false;
    private readonly bool[] _prevNum = new bool[9];
    private bool _prevUp        = false;
    private bool _prevDown      = false;
    private bool _prevTab       = false;

    private string _consoleInput = "";

    public Engine(EngineSettings settings, IKeyBindings keyBindings, IReadOnlyList<IGameMod> mods, string primaryAssetBasePath)
    {
        ArgumentNullException.ThrowIfNull(mods);
        if (mods.Count == 0)
            throw new InvalidOperationException("At least one mod is required.");

        ArgumentException.ThrowIfNullOrWhiteSpace(primaryAssetBasePath);

        _settings    = settings;
        _keyBindings = keyBindings;
        _mods        = mods;
        _primaryAssetBasePath = Path.GetFullPath(primaryAssetBasePath);
        _fixedDelta  = 1.0 / settings.TargetUPS;

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

        input.Keyboard.KeyChar += (_, c) =>
        {
            if (_context?.Console.IsOpen == true)
                _consoleInput += c;
        };

        float aspectRatio      = (float)_settings.WindowWidth / _settings.WindowHeight;
        var (camX, camY, camZ) = _settings.CameraStartPosition;
        var entityModels = FileSystemEntityModelLibrary.LoadFromDirectory(Path.Combine(_primaryAssetBasePath, "Entities"), _settings.EntityVoxelScale);
        var renderer  = new Renderer(_gl, _settings, entityModels);
        var world     = new World.World();
        var generator = new WorldGenerator(_settings, Path.Combine(_primaryAssetBasePath, "Climate"));
        var playerStart = CreatePlayerStartPosition(generator, camX, camY, camZ);
        _playerSpawnPoint = playerStart;

        _interactionReach = _settings.InteractionReach;

        // Spieler als plain Entity — Komponenten werden in VoxelGame.Initialize() hinzugefügt
        var playerEntity = new Entity.Entity("player", playerStart);

        var camera = new Camera(
            new Vector3D<float>(playerStart.X, playerStart.Y + _settings.EyeHeight, playerStart.Z),
            aspectRatio,
            _settings);

        _persistence = new LocalFilePersistence(_settings.SaveDirectory);
        _context     = new GameContext(_settings, _keyBindings, world, playerEntity, camera, renderer, input, generator, entityModels, _persistence, _playerSpawnPoint);
        _modContexts.Clear();
        foreach (IGameMod mod in _mods)
        {
            string assetBasePath = ResolveAssetBasePath(mod);
            _modContexts.Add(new EngineModContext(_context, mod.Id, assetBasePath));
        }

        _context.EntityManager.UpdateContext = _modContexts[0];

        // Spieler-Komponenten hinzufügen
        var playerPhys = new Entity.Components.PhysicsComponent(
            world,
            _settings.PlayerWidth, _settings.PlayerHeight,
            _settings.Gravity, _settings.MaxFallSpeed,
            _settings.FallDamageThreshold, _settings.FallDamageMultiplier,
            _settings.StepHeight, _settings.EnableStepUp,
            _settings.StepUpMaxVisualDrop, _settings.StepUpSmoothingSpeed);
        playerPhys.EyeOffset = _settings.EyeHeight;
        playerEntity.AddComponent(playerPhys);
        playerEntity.AddComponent(new Entity.Components.HealthComponent(20f));
        var inputComponent = new Entity.Components.InputComponent(
            input, _keyBindings, camera, _settings.MovementSpeed, _settings.JumpVelocity, _settings.FlySpeed);
        playerEntity.AddComponent(inputComponent);
        playerEntity.AddComponent(new Entity.Components.CameraComponent(camera));

        // Spielerstand + Welt-Metadaten laden (falls vorhanden)
        var (savedPlayer, savedWorld) = _context.LoadGameStateAsync().GetAwaiter().GetResult();

        // Mods initialisieren
        for (int i = 0; i < _mods.Count; i++)
            _mods[i].Initialize(_modContexts[i]);

        if (savedPlayer is not null && savedWorld is not null)
            _context.ApplyLoadedState(savedPlayer, savedWorld);

        // Spawn-Schutz
        var phys = _context.Player.GetComponent<PhysicsComponent>();
        _spawnFlyMode = phys?.FlyMode ?? false;
        phys?.SetFlyMode(true);
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
        _context.Console.Register(new DamageCommand());
        _context.Console.Register(new SpeedCommand(inputComponent));
        _context.Console.Register(new FlySpeedCommand(inputComponent));

        _debugOverlay = new DebugOverlay(_gl, _context, _settings.WindowWidth, _settings.WindowHeight, Path.Combine(_primaryAssetBasePath, "Fonts", "font.png"));

        var hotbarElement  = new HotbarHudElement();
        _context.HudRegistry.Register(hotbarElement);
        var hotbarRenderer = new HotbarHudRenderer(_gl, _settings, _settings.WindowWidth, _settings.WindowHeight, Path.Combine(_primaryAssetBasePath, "Fonts", "font.png"));
        hotbarRenderer.Atlas = _context.Renderer.Atlas;
        _debugOverlay.HudManager.RegisterRenderer("hotbar", hotbarRenderer);

        var healthElement  = new HealthHudElement();
        _context.HudRegistry.Register(healthElement);
        var healthRenderer = new HealthHudRenderer(_gl, _settings, _settings.WindowWidth, _settings.WindowHeight, Path.Combine(_primaryAssetBasePath, "Fonts", "font.png"));
        _debugOverlay.HudManager.RegisterRenderer("health", healthRenderer);

        var hungerElement  = new HungerHudElement();
        _context.HudRegistry.Register(hungerElement);
        var hungerRenderer = new HungerHudRenderer(_gl, _settings, _settings.WindowWidth, _settings.WindowHeight, Path.Combine(_primaryAssetBasePath, "Fonts", "font.png"));
        _debugOverlay.HudManager.RegisterRenderer("hunger", hungerRenderer);

        _context.HudRegistry.LoadConfig(Path.Combine(_primaryAssetBasePath, "Hud", "hud.json"));

        _pauseMenu = new PauseMenuPanel();
        _pauseMenu.InitRenderer(_gl, Path.Combine(_primaryAssetBasePath, "Fonts", "font.png"));
        _context.UI.Register(_pauseMenu, isGameMenu: true);

        var invFont     = new BitmapFont(_gl, Path.Combine(_primaryAssetBasePath, "Fonts", "font.png"));
        var invText     = new TextRenderer(_gl, invFont, _settings.WindowWidth, _settings.WindowHeight);
        var invIcon     = new IconRenderer(_gl);
        _inventoryPanel = new InventoryPanel(invText, invIcon, _keyBindings.ToggleInventory);
        _inventoryPanel.Atlas = _context.Renderer.Atlas;
        _context.UI.Register(_inventoryPanel);

        _context.ChunkManager.PrimeInitialChunks(
            _context.Player.InternalPosition.X,
            _context.Player.InternalPosition.Z,
            _settings.InitialChunkLoadRadius);

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

    private void OnRender(double deltaTime) => Render(deltaTime);

    private void Update(double fixedDelta)
    {
        var phys = _context.Player.GetComponent<PhysicsComponent>();

        if (_waitingForChunkLoad)
        {
            var pos = _context.Player.InternalPosition;
            int cx  = (int)Math.Floor(pos.X / Chunk.Width);
            int cz  = (int)Math.Floor(pos.Z / Chunk.Depth);
            if (_context.World.GetChunk(cx, cz) is not null)
            {
                phys?.SetFlyMode(_spawnFlyMode);
                if (!_spawnFlyMode) phys?.SyncPhysics(_context.Player);
                _waitingForChunkLoad = false;
            }
        }

        bool f1Now = _context.Input.IsKeyPressed(_keyBindings.DebugConsole);
        if (f1Now && !_prevF1) _context.Console.Toggle();
        _prevF1 = f1Now;

        if (_context.Console.IsOpen)
        {
            _context.Input.ClearTransientMouseState();

            bool escNow = _context.Input.IsKeyPressed(_keyBindings.Pause);
            if (escNow && !_prevEscape) { _context.Console.Toggle(); _consoleInput = ""; }
            _prevEscape = escNow;

            bool bsNow = _context.Input.IsKeyPressed(Key.Backspace);
            if (bsNow && !_prevBackspace && _consoleInput.Length > 0)
                _consoleInput = _consoleInput[..^1];
            _prevBackspace = bsNow;

            bool enterNow = _context.Input.IsKeyPressed(Key.Enter);
            if (enterNow && !_prevEnter) { _context.Console.Execute(_consoleInput); _consoleInput = ""; }
            _prevEnter = enterNow;

            bool upNow = _context.Input.IsKeyPressed(Key.Up);
            if (upNow && !_prevUp) { var result = _context.Console.NavigateHistoryUp(_consoleInput); if (result is not null) _consoleInput = result; }
            _prevUp = upNow;

            bool downNow = _context.Input.IsKeyPressed(Key.Down);
            if (downNow && !_prevDown) { var result = _context.Console.NavigateHistoryDown(); if (result is not null) _consoleInput = result; }
            _prevDown = downNow;

            bool tabNow = _context.Input.IsKeyPressed(Key.Tab);
            if (tabNow && !_prevTab) { var result = _context.Console.Autocomplete(_consoleInput); if (result is not null) _consoleInput = result; }
            _prevTab = tabNow;

            return;
        }

        _prevEscape = _prevBackspace = _prevEnter = _prevUp = _prevDown = _prevTab = false;

        bool uiConsuming = _context.UI.Update(_context);

        if (_context.ShutdownRequested) { _window.Close(); return; }
        if (uiConsuming) { _context.Input.ClearTransientMouseState(); return; }

        _context.Time.Update(fixedDelta);

        // Maus-Eingabe → Camera
        var (deltaX, deltaY) = _context.Input.GetMouseDelta();
        _context.Camera.ProcessMouseMovement(deltaX, deltaY);

        // Spieler-Entity Update (InputComponent, CameraComponent, PhysicsComponent)
        _context.Player.Update(_modContexts[0], fixedDelta);

        // Eye-Position für Raycast
        var eyePos = phys is not null
            ? phys.GetEyePosition(_context.Player.InternalPosition)
            : _context.Player.InternalPosition;

        _context.TargetedBlock = BlockRaycaster.Raycast(
            _context.World,
            eyePos,
            ToNumerics(_context.Camera.Front),
            _interactionReach,
            ShouldIgnoreWaterForRaycast(eyePos));
        _context.PlacementPreview = null;

        // Hotbar-Scroll
        int scrollSteps   = _context.Input.ConsumeScrollSteps();
        int hotbarDelta   = MapHotbarScroll(scrollSteps);
        if (hotbarDelta > 0)
            for (int i = 0; i < hotbarDelta; i++) _context.Inventory.Hotbar.SelectNext();
        else if (hotbarDelta < 0)
            for (int i = 0; i < -hotbarDelta; i++) _context.Inventory.Hotbar.SelectPrevious();

        // Nummernrasten-Hotbar
        if (_settings.EnableHotbarNumberKeys)
        {
            Key[] numKeys = [_keyBindings.Hotbar1, _keyBindings.Hotbar2, _keyBindings.Hotbar3, _keyBindings.Hotbar4, _keyBindings.Hotbar5,
                             _keyBindings.Hotbar6, _keyBindings.Hotbar7, _keyBindings.Hotbar8, _keyBindings.Hotbar9];
            for (int i = 0; i < 9; i++)
            {
                bool numNow = _context.Input.IsKeyPressed(numKeys[i]);
                if (numNow && !_prevNum[i]) _context.Inventory.Hotbar.SelectSlot(i);
                _prevNum[i] = numNow;
            }
        }

        if (_context.Input.ConsumeMouseClicks(_keyBindings.BlockBreak) > 0)
            TryBreakTargetedBlock(eyePos);

        int rightClicks = _context.Input.ConsumeMouseClicks(_keyBindings.BlockPlace);
        if (rightClicks > 0)
            TryPlaceSelectedBlock(phys, eyePos);
        else
            _context.PlacementPreview = GetPlacementPreview(phys, eyePos);

        _context.ChunkManager.Update(
            _context.Player.InternalPosition.X,
            _context.Player.InternalPosition.Z);

        foreach (var (x, z) in _context.ChunkManager.UnloadedThisUpdate)
            _context.Renderer.RemoveChunkMesh(x, z);

        _context.EntityManager.Update(fixedDelta);
        foreach (IGameMod mod in _mods)
            mod.Update(fixedDelta);
        _context.RespawnPlayerIfDead();
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
        foreach (IGameMod mod in _mods)
            mod.Render(frameTime);
    }

    private void Close()
    {
        if (_closed) return;
        _closed = true;
        _context?.SaveGameStateAsync().GetAwaiter().GetResult();
        for (int i = _mods.Count - 1; i >= 0; i--)
            _mods[i].Shutdown();
        _pauseMenu?.Dispose();
        _inventoryPanel?.Dispose();
        _debugOverlay?.Dispose();
        _context?.Dispose();
        _persistence?.Dispose();
        System.Console.WriteLine("Engine closing.");
    }

    public void Dispose()
    {
        _window.Closing -= Close;
        _window.Dispose();
        GC.SuppressFinalize(this);
    }

    private void TryBreakTargetedBlock(Vector3 eyePos)
    {
        if (_context.TargetedBlock is not { } hit) return;
        if (hit.BlockType == BlockType.Water) return;

        var removedBlock = _context.World.GetBlock(hit.BlockPosition.X, hit.BlockPosition.Y, hit.BlockPosition.Z);
        _context.World.SetBlock(hit.BlockPosition.X, hit.BlockPosition.Y, hit.BlockPosition.Z, BlockType.Air);
        _context.ChunkManager.EnqueueBlockUpdate(hit.BlockPosition.X, hit.BlockPosition.Z);
        _context.Inventory.Hotbar.TryAdd(removedBlock);
        _context.TargetedBlock = BlockRaycaster.Raycast(
            _context.World, eyePos, ToNumerics(_context.Camera.Front),
            _interactionReach, ShouldIgnoreWaterForRaycast(eyePos));
    }

    private void TryPlaceSelectedBlock(PhysicsComponent? phys, Vector3 eyePos)
    {
        if (_context.TargetedBlock is not { } hit) return;

        int    slot  = _context.Inventory.Hotbar.SelectedSlot;
        var    stack = _context.Inventory.Hotbar.Hotbar[slot];
        if (stack is null) return;

        BlockPosition placement = GetPlacementTarget(hit);
        if (placement.Y < 0 || placement.Y >= Chunk.Height) return;

        bool playerIntersects = phys?.WouldIntersectBlock(_context.Player, placement.X, placement.Y, placement.Z) ?? false;
        if (playerIntersects) return;

        if (!BlockRegistry.IsReplaceable(_context.World.GetBlock(placement.X, placement.Y, placement.Z))) return;

        _context.World.SetBlock(placement.X, placement.Y, placement.Z, stack.BlockType);
        _context.ChunkManager.EnqueueBlockUpdate(placement.X, placement.Z);
        _context.Inventory.Hotbar.TryRemove(slot, 1);
        _context.TargetedBlock = BlockRaycaster.Raycast(
            _context.World, eyePos, ToNumerics(_context.Camera.Front),
            _interactionReach, ShouldIgnoreWaterForRaycast(eyePos));
        _context.PlacementPreview = null;
    }

    private BlockPlacementPreview? GetPlacementPreview(PhysicsComponent? phys, Vector3 eyePos)
    {
        if (_context.TargetedBlock is not { } hit) return null;

        var stack = _context.Inventory.Hotbar.Hotbar[_context.Inventory.Hotbar.SelectedSlot];
        if (stack is null) return null;

        BlockPosition placement = GetPlacementTarget(hit);
        if (placement.Y < 0 || placement.Y >= Chunk.Height) return null;

        bool playerIntersects = phys?.WouldIntersectBlock(_context.Player, placement.X, placement.Y, placement.Z) ?? false;
        if (playerIntersects) return null;

        if (!BlockRegistry.IsReplaceable(_context.World.GetBlock(placement.X, placement.Y, placement.Z))) return null;

        return new BlockPlacementPreview(placement, stack.BlockType);
    }

    private static BlockPosition GetPlacementTarget(BlockRaycastHit hit) =>
        BlockRegistry.IsReplaceable(hit.BlockType) ? hit.BlockPosition : hit.PlacementPosition;

    private int MapHotbarScroll(int scrollSteps)
    {
        if (scrollSteps == 0) return 0;
        int direction = _keyBindings.HotbarScrollUp == ScrollBinding.Up ? 1 : -1;
        return scrollSteps * direction;
    }

    private bool ShouldIgnoreWaterForRaycast(Vector3 eyePos)
    {
        int x = (int)MathF.Floor(eyePos.X);
        int y = (int)MathF.Floor(eyePos.Y);
        int z = (int)MathF.Floor(eyePos.Z);
        return _context.World.GetBlock(x, y, z) == BlockType.Water;
    }

    private Vector3 CreatePlayerStartPosition(WorldGenerator generator, float startX, float startEyeY, float startZ)
    {
        int   surfaceHeight   = generator.GetSurfaceHeight((int)MathF.Floor(startX), (int)MathF.Floor(startZ));
        float minFeetY        = surfaceHeight + SpawnClearance;
        float configuredFeetY = startEyeY - _settings.EyeHeight;
        return new Vector3(startX, MathF.Max(configuredFeetY, minFeetY), startZ);
    }

    private static string ResolveAssetBasePath(IGameMod mod)
    {
        if (mod is ModLoader.IModAssetProvider assetProvider)
            return assetProvider.AssetBasePath;

        return Path.GetFullPath(Path.Combine("Mods", mod.Id, "Assets"));
    }
    private static Vector3 ToNumerics(Vector3D<float> vector) => new(vector.X, vector.Y, vector.Z);
    private static Vector3D<float> ToSilk(Vector3 vector) => new(vector.X, vector.Y, vector.Z);
}










