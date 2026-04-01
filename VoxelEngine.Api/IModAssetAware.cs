namespace VoxelEngine.Api;

/// <summary>
/// Optional interface for mods that need their asset base path before lifecycle registration starts.
/// </summary>
public interface IModAssetAware
{
    void SetAssetBasePath(string assetBasePath);
}
