using VoxelEngine.World;

namespace VoxelEngine.Persistence;

/// <summary>
/// Koordiniert das Persistieren des aktuellen Weltzustands, der nicht direkt an einzelne Chunk-Unloads gebunden ist.
/// </summary>
public static class WorldStatePersistence
{
    /// <summary>
    /// Speichert alle aktuell geladenen Chunks mit Spieler-Edits.
    /// Wichtig für reguläres Beenden, wenn geänderte Chunks noch im Speicher liegen.
    /// </summary>
    public static async Task SaveLoadedChunkEditsAsync(World.World world, IWorldPersistence persistence)
    {
        foreach (var chunk in world.GetAllChunks().Where(static chunk => chunk.IsDirty).ToArray())
        {
            await persistence.SaveChunkEditsAsync(
                chunk.ChunkPosition.X,
                chunk.ChunkPosition.Z,
                chunk.PlayerEdits).ConfigureAwait(false);
        }
    }
}
