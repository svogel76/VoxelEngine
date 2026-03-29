using System.Collections.Concurrent;

namespace VoxelEngine.Persistence;

/// <summary>
/// Triviale In-Memory-Implementierung von IWorldPersistence.
/// Geeignet für Tests und als Fallback wenn keine Datei-Persistence benötigt wird.
/// </summary>
public sealed class InMemoryPersistence : IWorldPersistence
{
    private readonly ConcurrentDictionary<(int X, int Z), Dictionary<(byte x, byte y, byte z), byte>> _chunkEdits = new();
    private PlayerState? _playerState;
    private WorldMeta?   _worldMeta;

    public Task SaveChunkEditsAsync(int chunkX, int chunkZ, IReadOnlyDictionary<(byte x, byte y, byte z), byte> edits)
    {
        _chunkEdits[(chunkX, chunkZ)] = new Dictionary<(byte x, byte y, byte z), byte>(edits);
        return Task.CompletedTask;
    }

    public Task<Dictionary<(byte x, byte y, byte z), byte>?> LoadChunkEditsAsync(int chunkX, int chunkZ)
    {
        Dictionary<(byte x, byte y, byte z), byte>? result = null;
        if (_chunkEdits.TryGetValue((chunkX, chunkZ), out var stored))
            result = new Dictionary<(byte x, byte y, byte z), byte>(stored);
        return Task.FromResult(result);
    }

    public Task SavePlayerStateAsync(PlayerState state)
    {
        _playerState = state;
        return Task.CompletedTask;
    }

    public Task<PlayerState?> LoadPlayerStateAsync()
        => Task.FromResult(_playerState);

    public Task SaveWorldMetaAsync(WorldMeta meta)
    {
        _worldMeta = meta;
        return Task.CompletedTask;
    }

    public Task<WorldMeta?> LoadWorldMetaAsync()
        => Task.FromResult(_worldMeta);
}
