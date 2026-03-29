using System.Numerics;
using FluentAssertions;
using VoxelEngine.Persistence;
using VoxelEngine.World;

namespace VoxelEngine.Tests.Persistence;

/// <summary>
/// Gemeinsame Tests gegen IWorldPersistence — werden für InMemory und LocalFile ausgeführt.
/// </summary>
public abstract class PersistenceTestBase
{
    protected abstract IWorldPersistence CreatePersistence();

    // ──────────────────────────────────────────────────────────────────────────
    // Chunk-Edits
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SaveAndLoad_ChunkEdits_RoundTrips()
    {
        var p = CreatePersistence();
        var edits = new Dictionary<(byte x, byte y, byte z), byte>
        {
            { (1, 64, 2), BlockType.Stone },
            { (15, 100, 15), BlockType.Sand }
        };

        await p.SaveChunkEditsAsync(5, -3, edits);
        var loaded = await p.LoadChunkEditsAsync(5, -3);

        loaded.Should().NotBeNull();
        loaded.Should().BeEquivalentTo(edits);
    }

    [Fact]
    public async Task LoadChunkEdits_NoData_ReturnsNull()
    {
        var p = CreatePersistence();
        (await p.LoadChunkEditsAsync(99, 99)).Should().BeNull();
    }

    [Fact]
    public async Task SaveAndLoad_MultipleChunks_StoreIndependently()
    {
        var p = CreatePersistence();
        var edits1 = new Dictionary<(byte x, byte y, byte z), byte> { { (0, 64, 0), BlockType.Grass } };
        var edits2 = new Dictionary<(byte x, byte y, byte z), byte> { { (5, 70, 5), BlockType.Dirt } };

        await p.SaveChunkEditsAsync(0, 0, edits1);
        await p.SaveChunkEditsAsync(1, 0, edits2);

        (await p.LoadChunkEditsAsync(0, 0)).Should().BeEquivalentTo(edits1);
        (await p.LoadChunkEditsAsync(1, 0)).Should().BeEquivalentTo(edits2);
    }

    [Fact]
    public async Task SaveChunkEdits_Overwrite_ReplacesOldData()
    {
        var p = CreatePersistence();
        var original = new Dictionary<(byte x, byte y, byte z), byte> { { (1, 64, 1), BlockType.Stone } };
        var updated = new Dictionary<(byte x, byte y, byte z), byte>
        {
            { (1, 64, 1), BlockType.Stone },
            { (2, 65, 2), BlockType.Grass }
        };

        await p.SaveChunkEditsAsync(0, 0, original);
        await p.SaveChunkEditsAsync(0, 0, updated);

        (await p.LoadChunkEditsAsync(0, 0)).Should().BeEquivalentTo(updated);
    }

    [Fact]
    public async Task SaveChunkEdits_NegativeChunkCoords_Work()
    {
        var p = CreatePersistence();
        var edits = new Dictionary<(byte x, byte y, byte z), byte> { { (8, 64, 8), BlockType.Dirt } };

        await p.SaveChunkEditsAsync(-5, -10, edits);

        (await p.LoadChunkEditsAsync(-5, -10)).Should().BeEquivalentTo(edits);
    }

    [Fact]
    public async Task SaveChunkEdits_ChunksInSameRegion_StoredIndependently()
    {
        var p = CreatePersistence();
        // Beide Chunk-Koordinaten liegen in derselben Region (regionX=0, regionZ=0)
        var edits1 = new Dictionary<(byte x, byte y, byte z), byte> { { (0, 64, 0), BlockType.Stone } };
        var edits2 = new Dictionary<(byte x, byte y, byte z), byte> { { (0, 64, 0), BlockType.Grass } };

        await p.SaveChunkEditsAsync(0,  0,  edits1);
        await p.SaveChunkEditsAsync(15, 15, edits2);

        (await p.LoadChunkEditsAsync(0,  0 )).Should().BeEquivalentTo(edits1);
        (await p.LoadChunkEditsAsync(15, 15)).Should().BeEquivalentTo(edits2);
    }

    [Fact]
    public async Task SaveChunkEdits_ChunksInDifferentRegions_StoredIndependently()
    {
        var p = CreatePersistence();
        var edits1 = new Dictionary<(byte x, byte y, byte z), byte> { { (0, 64, 0), BlockType.Stone } };
        var edits2 = new Dictionary<(byte x, byte y, byte z), byte> { { (0, 64, 0), BlockType.Sand } };

        // Chunk (0,0) → Region (0,0) ; Chunk (16,16) → Region (1,1)
        await p.SaveChunkEditsAsync(0,  0,  edits1);
        await p.SaveChunkEditsAsync(16, 16, edits2);

        (await p.LoadChunkEditsAsync(0,  0 )).Should().BeEquivalentTo(edits1);
        (await p.LoadChunkEditsAsync(16, 16)).Should().BeEquivalentTo(edits2);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Spielerstand
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SaveAndLoad_PlayerState_RoundTrips()
    {
        var p = CreatePersistence();
        var hotbar = new ItemStackData?[9];
        hotbar[0] = new ItemStackData(BlockType.Grass, 10);
        hotbar[2] = new ItemStackData(BlockType.Stone, 5);

        var state = new PlayerState(
            new Vector3(10f, 65.5f, -5f),
            FlyMode: true,
            SelectedSlot: 2,
            hotbar);

        await p.SavePlayerStateAsync(state);
        var loaded = await p.LoadPlayerStateAsync();

        loaded.Should().NotBeNull();
        loaded!.Position.X.Should().BeApproximately(10f, 0.001f);
        loaded.Position.Y.Should().BeApproximately(65.5f, 0.001f);
        loaded.Position.Z.Should().BeApproximately(-5f, 0.001f);
        loaded.FlyMode.Should().BeTrue();
        loaded.SelectedSlot.Should().Be(2);
        loaded.Hotbar[0].Should().Be(new ItemStackData(BlockType.Grass, 10));
        loaded.Hotbar[1].Should().BeNull();
        loaded.Hotbar[2].Should().Be(new ItemStackData(BlockType.Stone, 5));
        for (int i = 3; i < 9; i++)
            loaded.Hotbar[i].Should().BeNull();
    }

    [Fact]
    public async Task LoadPlayerState_NoData_ReturnsNull()
    {
        var p = CreatePersistence();
        (await p.LoadPlayerStateAsync()).Should().BeNull();
    }

    [Fact]
    public async Task SavePlayerState_EmptyHotbar_RoundTrips()
    {
        var p = CreatePersistence();
        var state = new PlayerState(
            Vector3.Zero,
            FlyMode: false,
            SelectedSlot: 0,
            new ItemStackData?[9]);

        await p.SavePlayerStateAsync(state);
        var loaded = await p.LoadPlayerStateAsync();

        loaded.Should().NotBeNull();
        for (int i = 0; i < 9; i++)
            loaded!.Hotbar[i].Should().BeNull();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Welt-Metadaten
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SaveAndLoad_WorldMeta_RoundTrips()
    {
        var p = CreatePersistence();
        var meta = new WorldMeta(Time: 14.5, DayCount: 7, Seed: 12345, TimeScale: 72.0);

        await p.SaveWorldMetaAsync(meta);
        var loaded = await p.LoadWorldMetaAsync();

        loaded.Should().Be(meta);
    }

    [Fact]
    public async Task LoadWorldMeta_NoData_ReturnsNull()
    {
        var p = CreatePersistence();
        (await p.LoadWorldMetaAsync()).Should().BeNull();
    }

    [Fact]
    public async Task SaveWorldMeta_Overwrite_ReplacesOldData()
    {
        var p = CreatePersistence();
        var first  = new WorldMeta(Time: 8.0,  DayCount: 1, Seed: 42,    TimeScale: 72.0);
        var second = new WorldMeta(Time: 20.0, DayCount: 5, Seed: 42,    TimeScale: 36.0);

        await p.SaveWorldMetaAsync(first);
        await p.SaveWorldMetaAsync(second);

        (await p.LoadWorldMetaAsync()).Should().Be(second);
    }
}

// ──────────────────────────────────────────────────────────────────────────
// Konkrete Test-Klassen für beide Implementierungen
// ──────────────────────────────────────────────────────────────────────────

public class InMemoryPersistenceTests : PersistenceTestBase
{
    protected override IWorldPersistence CreatePersistence() => new InMemoryPersistence();
}

public class LocalFilePersistenceTests : PersistenceTestBase, IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), $"VoxelEngineTests_{Path.GetRandomFileName()}");

    protected override IWorldPersistence CreatePersistence()
        => new LocalFilePersistence(_tempDir);

    [Fact]
    public async Task SaveAndLoad_AcrossSessions_DataPersists()
    {
        var edits = new Dictionary<(byte x, byte y, byte z), byte> { { (3, 64, 7), BlockType.Stone } };
        var meta  = new WorldMeta(Time: 12.0, DayCount: 3, Seed: 999, TimeScale: 72.0);

        using (var p1 = new LocalFilePersistence(_tempDir))
        {
            await p1.SaveChunkEditsAsync(2, 2, edits);
            await p1.SaveWorldMetaAsync(meta);
        }

        using var p2 = new LocalFilePersistence(_tempDir);
        (await p2.LoadChunkEditsAsync(2, 2)).Should().BeEquivalentTo(edits);
        (await p2.LoadWorldMetaAsync()).Should().Be(meta);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }
}
