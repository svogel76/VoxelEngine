using VoxelEngine.Api;
using VoxelEngine.Api.Entity;
using VoxelEngine.Api.World;

namespace VoxelEngine.Entity;

/// <summary>
/// Leerer IModContext für Engine-interne Komponenten, die keinen Mod-Zugriff benötigen.
/// </summary>
internal sealed class NullModContext : IModContext
{
    public static readonly NullModContext Instance = new();

    private NullModContext() { }

    public string             ModId            => string.Empty;
    public IComponentRegistry ComponentRegistry => throw new NotSupportedException();
    public IBlockRegistry     BlockRegistry    => throw new NotSupportedException();
    public IWorldAccess       World            => throw new NotSupportedException();
    public IInputState        Input            => throw new NotSupportedException();
    public IKeyBindings       KeyBindings      => throw new NotSupportedException();
    public IEntity            Player           => throw new NotSupportedException();
}
