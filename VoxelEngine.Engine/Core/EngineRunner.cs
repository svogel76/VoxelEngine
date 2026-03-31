using System.Text.Json;
using VoxelEngine.Entity.Components;
using VoxelEngine.World;

namespace VoxelEngine.Core;

public sealed class EngineRunner
{
    private readonly EngineSettings _settings;
    private readonly IKeyBindings   _keyBindings;

    public EngineRunner(EngineSettings settings, IKeyBindings keyBindings)
    {
        _settings    = settings;
        _keyBindings = keyBindings;
    }

    public void Run(IGameMod game)
    {
        ArgumentNullException.ThrowIfNull(game);

        BlockRegistry.Clear();
        game.RegisterBlocks(BlockRegistryAdapter.Instance);

        RegisterEngineComponents();

        using var engine = new Engine(_settings, _keyBindings, game);
        engine.Run();
    }

    private void RegisterEngineComponents()
    {
        var registry = EngineModContext.Registry;

        registry.Register("health",  config => HealthComponent.FromJson(config));
        registry.Register("physics", config => PhysicsComponent.FromJson(config, null!, _settings));
        registry.Register("ai",      config => AIComponent.FromJson(config, null!));
        registry.Register("drops",   config => DropComponent.FromJson(config));
        registry.Register("render",  config => RenderComponent.FromJson(config));
    }
}
