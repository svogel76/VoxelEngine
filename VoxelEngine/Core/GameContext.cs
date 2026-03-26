using VoxelEngine.Core.Debug;
using VoxelEngine.Rendering;
using VoxelEngine.World;

namespace VoxelEngine.Core;

public class GameContext : IDisposable
{
    public EngineSettings Settings      { get; }
    public World.World    World         { get; }
    public Player         Player        { get; }
    public Camera         Camera        { get; }
    public Renderer       Renderer      { get; }
    public InputHandler   Input         { get; }
    public DebugConsole   Console       { get; }
    public WorldGenerator Generator     { get; }
    public ChunkManager   ChunkManager  { get; }
    public WorldTime      Time          { get; }
    public BlockRaycastHit? TargetedBlock { get; set; }
    public BlockPlacementPreview? PlacementPreview { get; set; }
    private bool _disposed;

    public GameContext(
        EngineSettings  settings,
        World.World     world,
        Player          player,
        Camera          camera,
        Renderer        renderer,
        InputHandler    inputHandler,
        WorldGenerator  generator)
    {
        Settings     = settings;
        World        = world;
        Player       = player;
        Camera       = camera;
        Renderer     = renderer;
        Input        = inputHandler;
        Generator    = generator;
        Console      = new DebugConsole(this);
        ChunkManager = new ChunkManager(world, generator, settings);
        Time         = new WorldTime { TimeScale = settings.TimeScale };
        Time.SetTime(settings.InitialTime);
    }

    public bool TryDequeueResult(out ChunkResult result) =>
        ChunkManager.TryDequeueResult(out result);

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
