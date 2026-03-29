using System.Collections.Concurrent;
using System.Numerics;
using VoxelEngine.World;

namespace VoxelEngine.Persistence;

/// <summary>
/// Dateibasierte Persistence mit binärem Format und Region-Dateien (16×16 Chunks pro Datei).
///
/// Dateistruktur:
///   {baseDir}/regions/r.{regionX}.{regionZ}.vxr  — Chunk-Edits
///   {baseDir}/player.vxp                          — Spielerstand
///   {baseDir}/world.vxm                           — Welt-Metadaten
///
/// Region-Datei-Format (.vxr):
///   [4]    Magic: 'V','X','R','1'
///   [2048] Index: 256 Slots × (dataOffset: uint32, dataLength: uint32)
///            offset=0 und length=0 bedeutet: kein Eintrag für diesen Slot
///   [...]  Datenlöcke: pro Chunk — uint16 editCount + editCount × (x,y,z,blockType: je byte)
/// </summary>
public sealed class LocalFilePersistence : IWorldPersistence, IDisposable
{
    private const int RegionDim      = 16;          // Chunks pro Region-Achse
    private const int RegionSlots    = RegionDim * RegionDim; // 256
    private const int IndexEntrySize = 8;           // uint32 offset + uint32 length
    private const int HeaderSize     = 4 + RegionSlots * IndexEntrySize; // 2052

    private readonly string _regionsDir;
    private readonly string _playerFile;
    private readonly string _worldMetaFile;

    private readonly ConcurrentDictionary<(int, int), SemaphoreSlim> _regionLocks = new();
    private readonly SemaphoreSlim _playerLock    = new(1, 1);
    private readonly SemaphoreSlim _worldMetaLock = new(1, 1);
    private bool _disposed;

    public LocalFilePersistence(string baseDir)
    {
        _regionsDir    = Path.Combine(baseDir, "regions");
        _playerFile    = Path.Combine(baseDir, "player.vxp");
        _worldMetaFile = Path.Combine(baseDir, "world.vxm");
        Directory.CreateDirectory(_regionsDir);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Chunk-Edits
    // ──────────────────────────────────────────────────────────────────────────

    public async Task SaveChunkEditsAsync(int chunkX, int chunkZ, IReadOnlyDictionary<(byte x, byte y, byte z), byte> edits)
    {
        (int regionX, int regionZ, int slot) = GetRegionInfo(chunkX, chunkZ);
        byte[] slotData = SerializeEdits(edits);

        var sem = GetRegionLock(regionX, regionZ);
        await sem.WaitAsync().ConfigureAwait(false);
        try
        {
            string path = GetRegionPath(regionX, regionZ);
            var allSlots = await ReadAllSlotsAsync(path).ConfigureAwait(false);
            allSlots[slot] = slotData;
            byte[] file = BuildRegionFile(allSlots);
            await File.WriteAllBytesAsync(path, file).ConfigureAwait(false);
        }
        finally
        {
            sem.Release();
        }
    }

    public async Task<Dictionary<(byte x, byte y, byte z), byte>?> LoadChunkEditsAsync(int chunkX, int chunkZ)
    {
        (int regionX, int regionZ, int slot) = GetRegionInfo(chunkX, chunkZ);
        string path = GetRegionPath(regionX, regionZ);

        if (!File.Exists(path))
            return null;

        var sem = GetRegionLock(regionX, regionZ);
        await sem.WaitAsync().ConfigureAwait(false);
        try
        {
            byte[]? slotData = await ReadSlotAsync(path, slot).ConfigureAwait(false);
            return slotData is null ? null : DeserializeEdits(slotData);
        }
        finally
        {
            sem.Release();
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Spielerstand
    // ──────────────────────────────────────────────────────────────────────────

    public async Task SavePlayerStateAsync(PlayerState state)
    {
        await _playerLock.WaitAsync().ConfigureAwait(false);
        try
        {
            byte[] data = SerializePlayer(state);
            await File.WriteAllBytesAsync(_playerFile, data).ConfigureAwait(false);
        }
        finally
        {
            _playerLock.Release();
        }
    }

    public async Task<PlayerState?> LoadPlayerStateAsync()
    {
        if (!File.Exists(_playerFile))
            return null;

        await _playerLock.WaitAsync().ConfigureAwait(false);
        try
        {
            byte[] data = await File.ReadAllBytesAsync(_playerFile).ConfigureAwait(false);
            return DeserializePlayer(data);
        }
        finally
        {
            _playerLock.Release();
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Welt-Metadaten
    // ──────────────────────────────────────────────────────────────────────────

    public async Task SaveWorldMetaAsync(WorldMeta meta)
    {
        await _worldMetaLock.WaitAsync().ConfigureAwait(false);
        try
        {
            byte[] data = SerializeWorldMeta(meta);
            await File.WriteAllBytesAsync(_worldMetaFile, data).ConfigureAwait(false);
        }
        finally
        {
            _worldMetaLock.Release();
        }
    }

    public async Task<WorldMeta?> LoadWorldMetaAsync()
    {
        if (!File.Exists(_worldMetaFile))
            return null;

        await _worldMetaLock.WaitAsync().ConfigureAwait(false);
        try
        {
            byte[] data = await File.ReadAllBytesAsync(_worldMetaFile).ConfigureAwait(false);
            return DeserializeWorldMeta(data);
        }
        finally
        {
            _worldMetaLock.Release();
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Region-Datei-Hilfsmethoden
    // ──────────────────────────────────────────────────────────────────────────

    private string GetRegionPath(int regionX, int regionZ)
        => Path.Combine(_regionsDir, $"r.{regionX}.{regionZ}.vxr");

    private static (int regionX, int regionZ, int slot) GetRegionInfo(int chunkX, int chunkZ)
    {
        int regionX = (int)Math.Floor(chunkX / (double)RegionDim);
        int regionZ = (int)Math.Floor(chunkZ / (double)RegionDim);
        int localX  = ((chunkX % RegionDim) + RegionDim) % RegionDim;
        int localZ  = ((chunkZ % RegionDim) + RegionDim) % RegionDim;
        return (regionX, regionZ, localX * RegionDim + localZ);
    }

    private SemaphoreSlim GetRegionLock(int regionX, int regionZ)
        => _regionLocks.GetOrAdd((regionX, regionZ), _ => new SemaphoreSlim(1, 1));

    /// <summary>Liest alle Slot-Daten aus einer Region-Datei (slot-Index → rohe Bytes).</summary>
    private static async Task<Dictionary<int, byte[]>> ReadAllSlotsAsync(string path)
    {
        var slots = new Dictionary<int, byte[]>();
        if (!File.Exists(path))
            return slots;

        byte[] file = await File.ReadAllBytesAsync(path).ConfigureAwait(false);
        if (file.Length < HeaderSize)
            return slots;
        if (file[0] != 'V' || file[1] != 'X' || file[2] != 'R' || file[3] != '1')
            return slots;

        for (int i = 0; i < RegionSlots; i++)
        {
            int idxPos = 4 + i * IndexEntrySize;
            uint offset = BitConverter.ToUInt32(file, idxPos);
            uint length = BitConverter.ToUInt32(file, idxPos + 4);

            if (offset == 0 || length == 0)
                continue;
            if ((long)offset + length > file.Length)
                continue; // Korrupter Eintrag — überspringen

            slots[i] = file[(int)offset..(int)(offset + length)];
        }
        return slots;
    }

    /// <summary>Liest nur den Datenpuffer eines einzelnen Slots (effizienter für Load-Pfad).</summary>
    private static async Task<byte[]?> ReadSlotAsync(string path, int slot)
    {
        byte[] file = await File.ReadAllBytesAsync(path).ConfigureAwait(false);
        if (file.Length < HeaderSize)
            return null;
        if (file[0] != 'V' || file[1] != 'X' || file[2] != 'R' || file[3] != '1')
            return null;

        int idxPos = 4 + slot * IndexEntrySize;
        uint offset = BitConverter.ToUInt32(file, idxPos);
        uint length = BitConverter.ToUInt32(file, idxPos + 4);

        if (offset == 0 || length == 0 || (long)offset + length > file.Length)
            return null;

        return file[(int)offset..(int)(offset + length)];
    }

    /// <summary>Baut eine Region-Datei aus einem Slot-Dictionary neu auf.</summary>
    private static byte[] BuildRegionFile(Dictionary<int, byte[]> slots)
    {
        int dataSize = 0;
        foreach (var d in slots.Values)
            dataSize += d.Length;

        byte[] result = new byte[HeaderSize + dataSize];

        result[0] = (byte)'V';
        result[1] = (byte)'X';
        result[2] = (byte)'R';
        result[3] = (byte)'1';

        int writePos = HeaderSize;
        for (int i = 0; i < RegionSlots; i++)
        {
            if (!slots.TryGetValue(i, out var data))
                continue;

            int idxPos = 4 + i * IndexEntrySize;
            BitConverter.TryWriteBytes(result.AsSpan(idxPos,     4), (uint)writePos);
            BitConverter.TryWriteBytes(result.AsSpan(idxPos + 4, 4), (uint)data.Length);

            data.CopyTo(result, writePos);
            writePos += data.Length;
        }

        return result;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Serialisierung — Chunk-Edits
    // ──────────────────────────────────────────────────────────────────────────

    private static byte[] SerializeEdits(IReadOnlyDictionary<(byte x, byte y, byte z), byte> edits)
    {
        // uint16 editCount + editCount × 4 bytes
        byte[] data = new byte[2 + edits.Count * 4];
        BitConverter.TryWriteBytes(data.AsSpan(0, 2), (ushort)edits.Count);
        int pos = 2;
        foreach (var ((x, y, z), blockType) in edits)
        {
            data[pos++] = x;
            data[pos++] = y;
            data[pos++] = z;
            data[pos++] = blockType;
        }
        return data;
    }

    private static Dictionary<(byte x, byte y, byte z), byte> DeserializeEdits(byte[] data)
    {
        using var ms = new MemoryStream(data);
        using var br = new BinaryReader(ms);
        int count  = br.ReadUInt16();
        var result = new Dictionary<(byte x, byte y, byte z), byte>(count);
        for (int i = 0; i < count; i++)
        {
            byte x         = br.ReadByte();
            byte y         = br.ReadByte();
            byte z         = br.ReadByte();
            byte blockType = br.ReadByte();
            result[(x, y, z)] = blockType;
        }
        return result;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Serialisierung — Spielerstand
    // Binärformat (.vxp):
    //   [4]  Magic 'VXP1'
    //   [12] Position: float × 3
    //   [1]  FlyMode: byte (0/1)
    //   [1]  SelectedSlot: byte
    //   [9×] Hotbar-Slots: [hasItem: byte] [blockType: byte] [count: int32]
    // ──────────────────────────────────────────────────────────────────────────

    private static byte[] SerializePlayer(PlayerState state)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms, System.Text.Encoding.UTF8, leaveOpen: true);

        bw.Write('V'); bw.Write('X'); bw.Write('P'); bw.Write('1');
        bw.Write(state.Position.X);
        bw.Write(state.Position.Y);
        bw.Write(state.Position.Z);
        bw.Write(state.FlyMode ? (byte)1 : (byte)0);
        bw.Write((byte)state.SelectedSlot);

        for (int i = 0; i < Inventory.HotbarSize; i++)
        {
            var item = i < state.Hotbar.Count ? state.Hotbar[i] : null;
            if (item is null)
            {
                bw.Write((byte)0);
            }
            else
            {
                bw.Write((byte)1);
                bw.Write(item.BlockType);
                bw.Write(item.Count);
            }
        }

        bw.Flush();
        return ms.ToArray();
    }

    private static PlayerState? DeserializePlayer(byte[] data)
    {
        using var ms = new MemoryStream(data);
        using var br = new BinaryReader(ms);

        if (br.ReadByte() != 'V' || br.ReadByte() != 'X' ||
            br.ReadByte() != 'P' || br.ReadByte() != '1')
            return null;

        float posX       = br.ReadSingle();
        float posY       = br.ReadSingle();
        float posZ       = br.ReadSingle();
        bool  flyMode    = br.ReadByte() != 0;
        int   selSlot    = br.ReadByte();

        var hotbar = new ItemStackData?[Inventory.HotbarSize];
        for (int i = 0; i < Inventory.HotbarSize; i++)
        {
            if (br.ReadByte() != 0)
            {
                byte blockType = br.ReadByte();
                int  count     = br.ReadInt32();
                hotbar[i] = new ItemStackData(blockType, count);
            }
        }

        return new PlayerState(new Vector3(posX, posY, posZ), flyMode, selSlot, hotbar);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Serialisierung — Welt-Metadaten
    // Binärformat (.vxm):
    //   [4]  Magic 'VXM1'
    //   [8]  Time: double
    //   [4]  DayCount: int32
    //   [4]  Seed: int32
    //   [8]  TimeScale: double
    // ──────────────────────────────────────────────────────────────────────────

    private static byte[] SerializeWorldMeta(WorldMeta meta)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms, System.Text.Encoding.UTF8, leaveOpen: true);

        bw.Write('V'); bw.Write('X'); bw.Write('M'); bw.Write('1');
        bw.Write(meta.Time);
        bw.Write(meta.DayCount);
        bw.Write(meta.Seed);
        bw.Write(meta.TimeScale);

        bw.Flush();
        return ms.ToArray();
    }

    private static WorldMeta? DeserializeWorldMeta(byte[] data)
    {
        using var ms = new MemoryStream(data);
        using var br = new BinaryReader(ms);

        if (br.ReadByte() != 'V' || br.ReadByte() != 'X' ||
            br.ReadByte() != 'M' || br.ReadByte() != '1')
            return null;

        double time      = br.ReadDouble();
        int    dayCount  = br.ReadInt32();
        int    seed      = br.ReadInt32();
        double timeScale = br.ReadDouble();

        return new WorldMeta(time, dayCount, seed, timeScale);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IDisposable
    // ──────────────────────────────────────────────────────────────────────────

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var sem in _regionLocks.Values)
            sem.Dispose();
        _playerLock.Dispose();
        _worldMetaLock.Dispose();
    }
}
