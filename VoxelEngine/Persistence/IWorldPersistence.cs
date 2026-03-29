namespace VoxelEngine.Persistence;

/// <summary>
/// Zentrale Abstraktion der Persistence-Schicht.
/// Vollständig async für spätere SpacetimeDB-Kompatibilität.
/// </summary>
public interface IWorldPersistence
{
    // --- Chunk-Edits ---

    /// <summary>
    /// Speichert alle Spieler-Edits eines Chunks. Überschreibt vorhandene Daten.
    /// Wird beim Chunk-Unload aufgerufen wenn IsDirty true ist.
    /// </summary>
    Task SaveChunkEditsAsync(int chunkX, int chunkZ, IReadOnlyDictionary<(byte x, byte y, byte z), byte> edits);

    /// <summary>
    /// Lädt gespeicherte Edits für eine Chunk-Position.
    /// Gibt null zurück wenn keine Daten vorhanden sind.
    /// </summary>
    Task<Dictionary<(byte x, byte y, byte z), byte>?> LoadChunkEditsAsync(int chunkX, int chunkZ);

    // --- Spielerstand ---

    Task SavePlayerStateAsync(PlayerState state);

    /// <summary>Gibt null zurück wenn kein gespeicherter Spielerstand vorhanden.</summary>
    Task<PlayerState?> LoadPlayerStateAsync();

    // --- Welt-Metadaten ---

    Task SaveWorldMetaAsync(WorldMeta meta);

    /// <summary>Gibt null zurück wenn keine gespeicherten Welt-Metadaten vorhanden.</summary>
    Task<WorldMeta?> LoadWorldMetaAsync();
}
