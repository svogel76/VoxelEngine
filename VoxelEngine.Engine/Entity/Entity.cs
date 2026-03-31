using System.Numerics;
using VoxelEngine.Api;
using VoxelEngine.Api.Entity;
using VoxelEngine.Api.Math;

namespace VoxelEngine.Entity;

/// <summary>
/// Konkrete, versiegelte Entity-Klasse. Alle Spielwelt-Objekte sind Instanzen dieser Klasse,
/// zusammengesetzt aus IComponent-Implementierungen. Keine Unterklassen.
/// </summary>
public sealed class Entity : IEntity
{
    private Vector3 _position;
    private Vector3 _velocity;
    private readonly List<IComponent> _components = new();

    public Entity(string id, Vector3 position)
    {
        Id       = id ?? throw new ArgumentNullException(nameof(id));
        _position = position;
        IsActive  = true;
    }

    public string Id { get; }

    public Vector3D<float> Position
    {
        get => new(_position.X, _position.Y, _position.Z);
        set => _position = new Vector3(value.X, value.Y, value.Z);
    }

    public Vector3D<float> Velocity
    {
        get => new(_velocity.X, _velocity.Y, _velocity.Z);
        set => _velocity = new Vector3(value.X, value.Y, value.Z);
    }

    public bool IsActive { get; set; }

    /// <summary>Interne Vector3-Position für Engine-Systeme (kein Alloc).</summary>
    internal ref Vector3 InternalPosition => ref _position;

    /// <summary>Interne Vector3-Velocity für Engine-Systeme (kein Alloc).</summary>
    internal ref Vector3 InternalVelocity => ref _velocity;

    public IReadOnlyList<IComponent> Components => _components;

    public void AddComponent(IComponent component)
    {
        ArgumentNullException.ThrowIfNull(component);
        _components.Add(component);
    }

    public T? GetComponent<T>() where T : class, IComponent
    {
        foreach (var comp in _components)
            if (comp is T t) return t;
        return null;
    }

    public bool HasComponent<T>() where T : class, IComponent
        => GetComponent<T>() is not null;

    public void Update(IModContext context, double deltaTime)
    {
        foreach (var comp in _components)
            comp.Update(this, context, deltaTime);
    }
}
