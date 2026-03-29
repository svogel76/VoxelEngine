using VoxelEngine.Core.Debug;
using VoxelEngine.Core.Hud;
using VoxelEngine.Persistence;
using VoxelEngine.Rendering;
using VoxelEngine.World;

namespace VoxelEngine.Core;

public class GameContext : IDisposable
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
    public HudRegistry       HudRegistry   { get; } = new HudRegistry();
    public IWorldPersistence Persistence   { get; }

    public BlockRaycastHit?       TargetedBlock    { get; set; }
    public BlockPlacementPreview? PlacementPreview { get; set; }

    private bool _disposed;

    public GameContext(
        EngineSettings     settings,
        World.World        world,
        Player             player,
        Camera             camera,
        Renderer           renderer,
        InputHandler       inputHandler,
        WorldGenerator     generator,
        IWorldPersistence  persistence)
    {
        Settings     = settings;
        World        = world;
        Player       = player;
        Camera       = camera;
        Renderer     = renderer;
        Input        = inputHandler;
        Generator    = generator;
        Persistence  = persistence;
        Console      = new DebugConsole(this);
        ChunkManager = new ChunkManager(world, generator, settings, persistence);
        Time         = new WorldTime { TimeScale = settings.TimeScale };
        Time.SetTime(settings.InitialTime);
    }

    public bool TryDequeueResult(out ChunkResult result) =>
        ChunkManager.TryDequeueResult(out result);

    /// <summary>
    /// Speichert aktuellen Spielerstand und Welt-Metadaten in der Persistence-Schicht.
    /// </summary>
    public async Task SaveGameStateAsync()
    {
        await WorldStatePersistence.SaveLoadedChunkEditsAsync(World, Persistence).ConfigureAwait(false);

        var hotbar = new ItemStackData?[Inventory.HotbarSize];
        for (int i = 0; i < Inventory.HotbarSize; i++)
        {
            var stack = Player.Inventory.Hotbar[i];
            hotbar[i] = stack is null ? null : new ItemStackData(stack.BlockType, stack.Count);
        }

        var playerState = new PlayerState(
            Player.Position,
            Player.FlyMode,
            Player.Inventory.SelectedSlot,
            hotbar);

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
        Player.SetFlyMode(playerState.FlyMode);
        Player.Inventory.SelectSlot(playerState.SelectedSlot);
        for (int i = 0; i < Inventory.HotbarSize; i++)
        {
            var data = i < playerState.Hotbar.Count ? playerState.Hotbar[i] : null;
            Player.Inventory.SetSlot(i, data is null ? null : new ItemStack(data.BlockType, data.Count));
        }

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
}
