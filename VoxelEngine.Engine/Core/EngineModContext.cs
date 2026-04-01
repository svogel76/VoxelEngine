using VoxelEngine.Api;
using VoxelEngine.Api.Entity;
using VoxelEngine.Api.World;
using VoxelEngine.Entity;
using VoxelEngine.World;

namespace VoxelEngine.Core;

/// <summary>
/// IModContext implementation used by the engine runtime.
/// </summary>
public sealed class EngineModContext : IModContext
{
    private readonly GameContext _context;

    public EngineModContext(GameContext context, string modId, string assetBasePath)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        ArgumentException.ThrowIfNullOrWhiteSpace(modId);
        ArgumentException.ThrowIfNullOrWhiteSpace(assetBasePath);

        ModId = modId;
        AssetBasePath = Path.GetFullPath(assetBasePath);
    }

    public string ModId { get; }
    public string AssetBasePath { get; }
    public IComponentRegistry ComponentRegistry => _componentRegistry;
    public IBehaviourRegistry BehaviourRegistry => _behaviourRegistry;
    public IBlockRegistry BlockRegistry => BlockRegistryAdapter.Instance;
    public IWorldAccess World => _context.World;
    public IInputState Input => _context.Input;
    public IKeyBindings KeyBindings => _context.KeyBindings;
    public IEntity Player => _context.Player;
    public double WorldTimeHours => _context.Time.Time;
    public bool IsDay => _context.Time.IsDay;
    public bool IsNight => _context.Time.IsNight;

    private static readonly ComponentRegistry _componentRegistry = new();
    private static readonly BehaviourRegistry _behaviourRegistry = new();

    internal static ComponentRegistry Registry => _componentRegistry;
    public static BehaviourRegistry BehaviourTreeRegistry => _behaviourRegistry;
}
