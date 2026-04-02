namespace VoxelEngine.World;

public class Chunk
{
    public const int Width  = 16;
    public const int Height = 256;
    public const int Depth  = 16;

    private readonly byte[,,] _blocks = new byte[Width, Height, Depth];
    private readonly byte[,,] _skyLight = new byte[Width, Height, Depth];
    private readonly Dictionary<(byte x, byte y, byte z), byte> _playerEdits = new();

    public (int X, int Z) ChunkPosition { get; }
    public bool IsDirty { get; private set; }
    public IReadOnlyDictionary<(byte x, byte y, byte z), byte> PlayerEdits => _playerEdits;

    public Chunk(int x, int z)
    {
        ChunkPosition = (x, z);
    }

    public byte GetBlock(int x, int y, int z)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height || z < 0 || z >= Depth)
            return BlockType.Air;
        return _blocks[x, y, z];
    }

    public void SetBlock(int x, int y, int z, byte type)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height || z < 0 || z >= Depth)
            return;
        _blocks[x, y, z] = type;
    }

    public byte GetSkyLight(int x, int y, int z)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height || z < 0 || z >= Depth)
            return 0;

        return _skyLight[x, y, z];
    }

    public void SetSkyLight(int x, int y, int z, byte light)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height || z < 0 || z >= Depth)
            return;

        _skyLight[x, y, z] = light;
    }

    public void ClearSkyLight() => Array.Clear(_skyLight);

    /// <summary>
    /// Zeichnet eine Spieler-Aenderung auf und setzt IsDirty. Nur ueber World.SetBlock() aufrufen.
    /// </summary>
    public void RecordEdit(int x, int y, int z, byte blockType)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height || z < 0 || z >= Depth)
            return;
        _playerEdits[((byte)x, (byte)y, (byte)z)] = blockType;
        IsDirty = true;
    }

    /// <summary>
    /// Uebertraegt gespeicherte Edits eines entladenen Chunks in diesen neu generierten Chunk.
    /// Wird von ChunkWorker aufgerufen, bevor der Chunk der Welt hinzugefuegt wird.
    /// </summary>
    public void LoadEdits(Dictionary<(byte x, byte y, byte z), byte> edits)
    {
        foreach (var (key, value) in edits)
            _playerEdits[key] = value;
        if (_playerEdits.Count > 0)
            IsDirty = true;
    }

    /// <summary>
    /// Wendet alle gespeicherten Spieler-Edits auf das Blocks-Array an.
    /// Nach prozeduraler Terrain-Generierung aufrufen, damit Aenderungen Reload ueberleben.
    /// </summary>
    public void ApplyPlayerEdits()
    {
        foreach (var (pos, type) in _playerEdits)
            _blocks[pos.x, pos.y, pos.z] = type;
    }

    public bool IsEmpty()
    {
        for (int x = 0; x < Width;  x++)
        for (int y = 0; y < Height; y++)
        for (int z = 0; z < Depth;  z++)
        {
            if (_blocks[x, y, z] != BlockType.Air)
                return false;
        }
        return true;
    }
}
