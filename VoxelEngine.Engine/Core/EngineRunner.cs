using VoxelEngine.World;

namespace VoxelEngine.Core;

public sealed class EngineRunner
{
    private readonly EngineSettings _settings;

    public EngineRunner()
        : this(new EngineSettings { TargetFPS = 60 })
    {
    }

    public EngineRunner(EngineSettings settings)
    {
        _settings = settings;
    }

    public void Run(IGame game)
    {
        ArgumentNullException.ThrowIfNull(game);

        BlockRegistry.Clear();
        game.RegisterBlocks(BlockRegistryAdapter.Instance);

        using var engine = new Engine(_settings, game);
        engine.Run();
    }
}