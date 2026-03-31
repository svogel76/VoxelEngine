using VoxelEngine.World;

namespace VoxelEngine.Core;

public sealed class EngineRunner
{
    private readonly EngineSettings _settings;
    private readonly IKeyBindings _keyBindings;

    public EngineRunner(EngineSettings settings, IKeyBindings keyBindings)
    {
        _settings = settings;
        _keyBindings = keyBindings;
    }

    public void Run(IGame game)
    {
        ArgumentNullException.ThrowIfNull(game);

        BlockRegistry.Clear();
        game.RegisterBlocks(BlockRegistryAdapter.Instance);

        using var engine = new Engine(_settings, _keyBindings, game);
        engine.Run();
    }
}
