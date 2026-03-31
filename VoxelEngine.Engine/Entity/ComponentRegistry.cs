using System.Text.Json;
using VoxelEngine.Api.Entity;

namespace VoxelEngine.Entity;

public sealed class ComponentRegistry : IComponentRegistry
{
    private readonly Dictionary<string, Func<JsonElement, IComponent>> _factories =
        new(StringComparer.OrdinalIgnoreCase);

    public void Register(string name, Func<JsonElement, IComponent> factory)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(factory);
        _factories[name] = factory;
    }

    public IComponent Create(string name, JsonElement config)
    {
        if (!_factories.TryGetValue(name, out var factory))
            throw new KeyNotFoundException($"Unbekannte Komponente: '{name}'. Registrierte Komponenten: {string.Join(", ", _factories.Keys)}");

        return factory(config);
    }
}
