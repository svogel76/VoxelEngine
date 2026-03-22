using VoxelEngine.Core.Debug;
using VoxelEngine.Rendering;
using VoxelEngine.World;

namespace VoxelEngine.Core;

public class GameContext : IDisposable
{
    public EngineSettings Settings      { get; }
    public World.World    World         { get; }
    public Camera         Camera        { get; }
    public Renderer       Renderer      { get; }
    public InputHandler   Input         { get; }
    public DebugConsole   Console       { get; }
    public WorldGenerator Generator     { get; }
    public ChunkManager   ChunkManager  { get; }
    public WorldTime      Time          { get; }

    public GameContext(
        EngineSettings  settings,
        World.World     world,
        Camera          camera,
        Renderer        renderer,
        InputHandler    inputHandler,
        WorldGenerator  generator)
    {
        Settings     = settings;
        World        = world;
        Camera       = camera;
        Renderer     = renderer;
        Input        = inputHandler;
        Generator    = generator;
        Console      = new DebugConsole(this);
        ChunkManager = new ChunkManager(world, generator, settings);
        Time         = new WorldTime { TimeScale = settings.TimeScale };
    }

    public bool TryDequeueResult(out ChunkResult result) =>
        ChunkManager.TryDequeueResult(out result);

    public void Dispose()
    {
        ChunkManager.Dispose();
        Renderer.Dispose();
        Console.Dispose();
        GC.SuppressFinalize(this);
    }
}
