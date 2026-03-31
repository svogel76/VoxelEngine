using VoxelEngine.Api;
using VoxelEngine.Api.Entity;
using VoxelEngine.Api.World;
using VoxelEngine.Entity;
using VoxelEngine.World;

namespace VoxelEngine.Core;

/// <summary>
/// IModContext-Implementierung für den Engine-Kern.
/// Wird an Entity.Update() übergeben, damit Komponenten auf Eingabe, Welt und Registry zugreifen können.
/// </summary>
public sealed class EngineModContext : IModContext
{
    private readonly GameContext _context;

    public EngineModContext(GameContext context, string modId = "engine")
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        ModId    = modId;
    }

    public string             ModId            { get; }
    public IComponentRegistry ComponentRegistry => _componentRegistry;
    public IBlockRegistry     BlockRegistry    => BlockRegistryAdapter.Instance;
    public IWorldAccess       World            => _context.World;
    public IInputState        Input            => _context.Input;
    public IKeyBindings       KeyBindings      => _context.KeyBindings;
    public IEntity            Player           => _context.Player;

    private static readonly ComponentRegistry _componentRegistry = new();
    internal static ComponentRegistry Registry => _componentRegistry;
}
