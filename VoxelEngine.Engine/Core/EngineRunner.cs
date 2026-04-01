using System.Text.Json;
using VoxelEngine.Entity.BehaviourTree.Actions;
using VoxelEngine.Entity.BehaviourTree.Conditions;
using VoxelEngine.Entity.Components;
using VoxelEngine.World;

namespace VoxelEngine.Core;

public sealed class EngineRunner
{
    private readonly EngineSettings? _settingsOverride;
    private readonly IKeyBindings? _keyBindingsOverride;

    public EngineRunner()
    {
    }

    public EngineRunner(EngineSettings settings, IKeyBindings keyBindings)
    {
        _settingsOverride = settings;
        _keyBindingsOverride = keyBindings;
    }

    public void Run(IReadOnlyList<IGameMod> mods)
    {
        ArgumentNullException.ThrowIfNull(mods);
        if (mods.Count == 0)
            throw new InvalidOperationException("At least one mod must be loaded before running the engine.");

        IGameMod primaryMod = mods[0];
        string primaryAssetBasePath = ResolveAssetBasePath(primaryMod);
        EngineSettings settings = _settingsOverride ?? EngineSettings.LoadFrom(primaryAssetBasePath);
        IKeyBindings keyBindings = _keyBindingsOverride ?? KeyBindingLoader.LoadFrom(primaryAssetBasePath);

        BlockRegistry.Clear();
        RegisterEngineComponents(settings);
        RegisterEngineBehaviours();

        foreach (IGameMod mod in mods)
            mod.RegisterComponents(EngineModContext.Registry);

        foreach (IGameMod mod in mods)
            mod.RegisterBehaviours(EngineModContext.BehaviourTreeRegistry);

        foreach (IGameMod mod in mods)
            mod.RegisterBlocks(BlockRegistryAdapter.Instance);

        using var engine = new Engine(settings, keyBindings, mods, primaryAssetBasePath);
        engine.Run();
    }

    private static string ResolveAssetBasePath(IGameMod mod)
    {
        if (mod is ModLoader.IModAssetProvider assetProvider)
            return assetProvider.AssetBasePath;

        return Path.GetFullPath(Path.Combine("Mods", mod.Id, "Assets"));
    }

    private static void RegisterEngineComponents(EngineSettings settings)
    {
        var registry = EngineModContext.Registry;

        registry.Register("health", config => HealthComponent.FromJson(config));
        registry.Register("physics", config => PhysicsComponent.FromJson(config, null!, settings));
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
