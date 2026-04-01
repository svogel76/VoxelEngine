using System.Text.Json;
using VoxelEngine.Entity.BehaviourTree.Actions;
using VoxelEngine.Entity.BehaviourTree.Conditions;
using VoxelEngine.Entity.Components;
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

    public void Run(IGameMod game)
    {
        ArgumentNullException.ThrowIfNull(game);

        BlockRegistry.Clear();
        RegisterEngineComponents();
        RegisterEngineBehaviours();
        game.RegisterBlocks(BlockRegistryAdapter.Instance);

        using var engine = new Engine(_settings, _keyBindings, game);
        engine.Run();
    }

    private void RegisterEngineComponents()
    {
        var registry = EngineModContext.Registry;

        registry.Register("health", config => HealthComponent.FromJson(config));
        registry.Register("physics", config => PhysicsComponent.FromJson(config, null!, _settings));
        registry.Register("ai", config => AIComponent.FromJson(config, EngineModContext.BehaviourTreeRegistry));
        registry.Register("drops", config => DropComponent.FromJson(config));
        registry.Register("render", config => RenderComponent.FromJson(config));
    }

    private static void RegisterEngineBehaviours()
    {
        var registry = EngineModContext.BehaviourTreeRegistry;

        registry.RegisterCondition("player_near", config => new PlayerNearCondition(ReadRequiredSingle(config, "radius")));
        registry.RegisterCondition("health_low", config => new HealthLowCondition(ReadRequiredSingle(config, "threshold")));
        registry.RegisterCondition("is_night", _ => new IsNightCondition());
        registry.RegisterCondition("is_day", _ => new IsDayCondition());

        registry.RegisterAction("flee", config => new FleeAction(
            ReadRequiredSingle(config, "speed"),
            ReadRequiredSingle(config, "radius")));
        registry.RegisterAction("wander", config => new WanderAction(
            ReadRequiredSingle(config, "speed"),
            ReadRequiredSingle(config, "radius"),
            ReadRequiredSingle(config, "pause_seconds")));
        registry.RegisterAction("idle", config => new IdleAction(ReadRequiredSingle(config, "duration_seconds")));
    }

    private static float ReadRequiredSingle(JsonElement config, string propertyName)
    {
        if (!config.TryGetProperty(propertyName, out var property))
            throw new InvalidOperationException($"Behaviour node '{config.GetProperty("name").GetString()}' is missing required property '{propertyName}'.");

        return property.GetSingle();
    }
}
