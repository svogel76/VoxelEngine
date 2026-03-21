using VoxelEngine.Core;

namespace VoxelEngine.World;

public class ChunkManager
{
    private readonly World          _world;
    private readonly WorldGenerator _generator;
    private readonly EngineSettings _settings;

    private int _lastPlayerChunkX = int.MinValue;
    private int _lastPlayerChunkZ = int.MinValue;

    private readonly HashSet<(int, int)> _chunksToLoad    = new();
    private readonly List<(int, int)>    _unloadedThisUpdate = new();

    /// <summary>Überschreibt EngineSettings.RenderDistance zur Laufzeit</summary>
    public int RenderDistance { get; set; }

    public int UnloadDistance        => _settings.UnloadDistance;
    public int MaxChunksLoadedPerFrame => _settings.MaxChunksLoadedPerFrame;

    public int PendingChunks => _chunksToLoad.Count;

    /// <summary>Chunks die im letzten Update()-Aufruf entladen wurden</summary>
    public IReadOnlyList<(int chunkX, int chunkZ)> UnloadedThisUpdate => _unloadedThisUpdate;

    public ChunkManager(World world, WorldGenerator generator, EngineSettings settings)
    {
        _world     = world;
        _generator = generator;
        _settings  = settings;

        RenderDistance = settings.RenderDistance;
    }

    public void Update(float playerWorldX, float playerWorldZ)
    {
        int playerChunkX = (int)Math.Floor(playerWorldX / Chunk.Width);
        int playerChunkZ = (int)Math.Floor(playerWorldZ / Chunk.Depth);

        _unloadedThisUpdate.Clear();

        // Entlade-Phase
        foreach (var chunk in _world.GetAllChunks().ToList())
        {
            int dx   = chunk.ChunkPosition.X - playerChunkX;
            int dz   = chunk.ChunkPosition.Z - playerChunkZ;
            int dist = Math.Max(Math.Abs(dx), Math.Abs(dz));

            if (dist > UnloadDistance)
            {
                _world.RemoveChunk(chunk.ChunkPosition.X, chunk.ChunkPosition.Z);
                _chunksToLoad.Remove((chunk.ChunkPosition.X, chunk.ChunkPosition.Z));
                _unloadedThisUpdate.Add((chunk.ChunkPosition.X, chunk.ChunkPosition.Z));
            }
        }

        // Position unverändert — keine neue Lade-Phase nötig
        if (playerChunkX == _lastPlayerChunkX && playerChunkZ == _lastPlayerChunkZ)
            return;

        _lastPlayerChunkX = playerChunkX;
        _lastPlayerChunkZ = playerChunkZ;

        // Lade-Phase: alle Chunks im Quadrat ±RenderDistance einplanen
        for (int dx = -RenderDistance; dx <= RenderDistance; dx++)
        for (int dz = -RenderDistance; dz <= RenderDistance; dz++)
        {
            int dist = Math.Max(Math.Abs(dx), Math.Abs(dz));
            if (dist > RenderDistance)
                continue;

            int cx = playerChunkX + dx;
            int cz = playerChunkZ + dz;

            if (_world.GetChunk(cx, cz) is null)
                _chunksToLoad.Add((cx, cz));
        }
    }

    public void ProcessLoadQueue(Action<int, int> onChunkLoaded)
    {
        int loaded = 0;

        foreach (var (cx, cz) in _chunksToLoad.ToList())
        {
            if (loaded >= MaxChunksLoadedPerFrame)
                break;

            _generator.GenerateChunk(cx, cz, _world);
            _chunksToLoad.Remove((cx, cz));
            onChunkLoaded(cx, cz);
            loaded++;
        }
    }
}
