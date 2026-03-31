using VoxelEngine.Core.Debug;
using VoxelEngine.Core.Hud;
using VoxelEngine.Core.UI;
using VoxelEngine.Entity;
using VoxelEngine.Entity.Models;
using VoxelEngine.Entity.Spawning;
using VoxelEngine.Persistence;
using VoxelEngine.Rendering;
using VoxelEngine.World;
using VoxelEngine.World.Inventories;

namespace VoxelEngine.Core;

public class GameContext : IDisposable, IGameContext
{
    public EngineSettings    Settings      { get; }
    public World.World       World         { get; }
    public Player            Player        { get; }
    public Camera            Camera        { get; }
    public Renderer          Renderer      { get; }
    public InputHandler      Input         { get; }
    public DebugConsole      Console       { get; }
    public WorldGenerator    Generator     { get; }
    public ChunkManager      ChunkManager  { get; }
    public WorldTime         Time          { get; }
    public EntityManager     EntityManager { get; }
    public SpawnManager      SpawnManager  { get; }
    public IEntityModelLibrary EntityModels { get; }
    public HudRegistry       HudRegistry   { get; } = new HudRegistry();
    public UIStateManager    UI            { get; } = new UIStateManager();
    public IWorldPersistence Persistence   { get; }
    IBlockRegistry IGameContext.BlockRegistry => BlockRegistryAdapter.Instance;
    IWorldAccess IGameContext.World => World;
    IInputState IGameContext.Input => Input;

    /// <summary>
    /// Vollständiges Spieler-Inventar (Hotbar + Grid + Equipment).
    /// Wird nach dem Konstruktor via Init() gesetzt.
    /// </summary>
    public PlayerInventory   Inventory     { get; private set; } = null!;

    public BlockRaycastHit?       TargetedBlock    { get; set; }
    public BlockPlacementPreview? PlacementPreview { get; set; }

    /// <summary>
    /// Wird von UI-Panels gesetzt (z.B. "Beenden"-Button im Pause-Menü).
    /// Engine.Update() prüft dieses Flag und schließt das Fenster sauber.
    /// </summary>
    public bool ShutdownRequested { get; set; }

    private bool _disposed;

    public GameContext(
        EngineSettings     settings,
        World.World        world,
        Player             player,
        Camera             camera,
        Renderer           renderer,
        InputHandler       inputHandler,
        WorldGenerator     generator,
        IEntityModelLibrary entityModels,
        IWorldPersistence  persistence)
    {
        Settings     = settings;
        World        = world;
        Player       = player;
        Camera       = camera;
        Renderer     = renderer;
        Input        = inputHandler;
        Generator    = generator;
        EntityModels = entityModels;
        Persistence  = persistence;
        Console      = new DebugConsole(this);
        ChunkManager = new ChunkManager(world, generator, settings, persistence);
        Time         = new WorldTime { TimeScale = settings.TimeScale };
        EntityManager = new EntityManager(
            world,
            settings,
            generator,
            Time,
            entityModels,
            () => Player.Position,
            () => global::VoxelEngine.Entity.ViewFrustum.FromViewProjection(ToNumerics(Camera.ViewMatrix * Camera.ProjectionMatrix)));
        SpawnManager = new SpawnManager(
            world,
            EntityManager,
            settings,
            generator,
            Time,
            entityModels,
            () => Player.Position,
            () => global::VoxelEngine.Entity.ViewFrustum.FromViewProjection(ToNumerics(Camera.ViewMatrix * Camera.ProjectionMatrix)));
        EntityManager.SpawnManager = SpawnManager;
        Time.SetTime(settings.InitialTime);
        Inventory    = new PlayerInventory(player.Inventory);
    }

    public bool TryDequeueResult(out ChunkResult result) =>
        ChunkManager.TryDequeueResult(out result);

    /// <summary>
    /// Speichert aktuellen Spielerstand und Welt-Metadaten in der Persistence-Schicht.
    /// </summary>
    public async Task SaveGameStateAsync()
    {
        await WorldStatePersistence.SaveLoadedChunkEditsAsync(World, Persistence).ConfigureAwait(false);

        var hotbar = new ItemStackData?[global::VoxelEngine.World.Inventory.HotbarSize];
        for (int i = 0; i < global::VoxelEngine.World.Inventory.HotbarSize; i++)
        {
            var stack = Player.Inventory.Hotbar[i];
            hotbar[i] = stack is null ? null : new ItemStackData(stack.BlockType, stack.Count);
        }

        var inventoryGrid = new ItemStackData?[InventoryGrid.TotalSlots];
        for (int i = 0; i < InventoryGrid.TotalSlots; i++)
        {
            var stack = Inventory.Grid.Get(i);
            inventoryGrid[i] = stack is null ? null : new ItemStackData(stack.BlockType, stack.Count);
        }

        var equipmentSlots = new ItemStackData?[EquipmentSlots.Count];
        for (int i = 0; i < EquipmentSlots.Count; i++)
        {
            var stack = Inventory.Equipment.Get((EquipmentSlotType)i);
            equipmentSlots[i] = stack is null ? null : new ItemStackData(stack.BlockType, stack.Count);
        }

        var playerState = new PlayerState(
            Player.Position,
            Camera.Yaw,
            Camera.Pitch,
            Player.FlyMode,
            Player.Inventory.SelectedSlot,
            hotbar,
            inventoryGrid,
            equipmentSlots,
            Player.Vitals.Health,
            Player.Vitals.Hunger);

        var worldMeta = new WorldMeta(
            Time.Time,
            Time.DayCount,
            Settings.Terrain.Seed,
            Time.TimeScale);

        await Persistence.SavePlayerStateAsync(playerState).ConfigureAwait(false);
        await Persistence.SaveWorldMetaAsync(worldMeta).ConfigureAwait(false);
    }

    /// <summary>
    /// Lädt gespeicherten Spielerstand und Welt-Metadaten.
    /// Gibt (null, null) zurück wenn kein Speicherstand vorhanden.
    /// </summary>
    public async Task<(PlayerState? Player, WorldMeta? World)> LoadGameStateAsync()
    {
        var playerState = await Persistence.LoadPlayerStateAsync().ConfigureAwait(false);
        var worldMeta   = await Persistence.LoadWorldMetaAsync().ConfigureAwait(false);
        return (playerState, worldMeta);
    }

    /// <summary>
    /// Wendet geladenen Spielerstand und Welt-Metadaten auf die aktiven Systeme an.
    /// </summary>
    public void ApplyLoadedState(PlayerState playerState, WorldMeta worldMeta)
    {
        Player.Teleport(playerState.Position);
        Camera.Position = new Silk.NET.Maths.Vector3D<float>(Player.EyePosition.X, Player.EyePosition.Y, Player.EyePosition.Z);
        Camera.SetRotation(playerState.Yaw, playerState.Pitch);
        Player.SetFlyMode(playerState.FlyMode);
        Player.Inventory.SelectSlot(playerState.SelectedSlot);
        for (int i = 0; i < global::VoxelEngine.World.Inventory.HotbarSize; i++)
        {
            var data = i < playerState.Hotbar.Count ? playerState.Hotbar[i] : null;
            Player.Inventory.SetSlot(i, data is null ? null : new ItemStack(data.BlockType, data.Count));
        }

        for (int i = 0; i < InventoryGrid.TotalSlots; i++)
        {
            var data = i < playerState.InventoryGrid.Count ? playerState.InventoryGrid[i] : null;
            Inventory.Grid.Set(i, data is null ? null : new ItemStack(data.BlockType, data.Count));
        }

        for (int i = 0; i < EquipmentSlots.Count; i++)
        {
            var data = i < playerState.EquipmentSlots.Count ? playerState.EquipmentSlots[i] : null;
            Inventory.Equipment.Set((EquipmentSlotType)i, data is null ? null : new ItemStack(data.BlockType, data.Count));
        }

        Player.Vitals.RestoreHealth(playerState.Health);
        Player.Vitals.RestoreHunger(playerState.Hunger);

        Time.Restore(worldMeta.Time, worldMeta.DayCount);
        Time.TimeScale = worldMeta.TimeScale;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        ChunkManager.Dispose();
        Renderer.Dispose();
        Console.Dispose();
        GC.SuppressFinalize(this);
    }

    private static System.Numerics.Matrix4x4 ToNumerics(Silk.NET.Maths.Matrix4X4<float> matrix)
        => new(
            matrix.M11, matrix.M12, matrix.M13, matrix.M14,
            matrix.M21, matrix.M22, matrix.M23, matrix.M24,
            matrix.M31, matrix.M32, matrix.M33, matrix.M34,
            matrix.M41, matrix.M42, matrix.M43, matrix.M44);
}
