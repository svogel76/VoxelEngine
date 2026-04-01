using System.Text.Json;
using VoxelEngine.Api.Entity;

namespace VoxelEngine;

public sealed class BehaviourRegistry : IBehaviourRegistry
{
    private readonly Dictionary<string, Func<JsonElement, IBehaviourNode>> _conditionFactories = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Func<JsonElement, IBehaviourNode>> _actionFactories = new(StringComparer.OrdinalIgnoreCase);

    public void RegisterCondition(string name, Func<JsonElement, IBehaviourNode> factory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(factory);
        _conditionFactories[name] = factory;
    }

    public void RegisterAction(string name, Func<JsonElement, IBehaviourNode> factory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(factory);
        _actionFactories[name] = factory;
    }

    public IBehaviourNode Create(string type, string name, JsonElement config)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var factories = type.Equals("condition", StringComparison.OrdinalIgnoreCase)
            ? _conditionFactories
            : type.Equals("action", StringComparison.OrdinalIgnoreCase)
                ? _actionFactories
                : throw new InvalidOperationException($"Unknown behaviour registry type '{type}' for node '{name}'.");

        if (!factories.TryGetValue(name, out var factory))
            throw new InvalidOperationException($"Unknown {type} node '{name}'. Registered {type}s: {string.Join(", ", factories.Keys.OrderBy(key => key, StringComparer.OrdinalIgnoreCase))}");

        return factory(config);
    }
}
