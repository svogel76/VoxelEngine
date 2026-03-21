using VoxelEngine.Core.Debug;
using VoxelEngine.Rendering;
using VoxelEngine.World;

namespace VoxelEngine.Core;

public class GameContext : IDisposable
{
    public EngineSettings Settings    { get; }
    public World.World    World       { get; }
    public Camera         Camera      { get; }
    public Renderer       Renderer    { get; }
    public InputHandler   Input       { get; }
    public DebugConsole   Console     { get; }

    public GameContext(
        EngineSettings settings,
        World.World    world,
        Camera         camera,
        Renderer       renderer,
        InputHandler   inputHandler)
    {
        Settings = settings;
        World    = world;
        Camera   = camera;
        Renderer = renderer;
        Input    = inputHandler;
        Console  = new DebugConsole(this);
    }

    public void Dispose()
    {
        Renderer.Dispose();
        Console.Dispose();
        GC.SuppressFinalize(this);
    }
}
