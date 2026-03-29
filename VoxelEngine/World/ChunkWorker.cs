using System.Collections.Concurrent;
using VoxelEngine.Rendering;

namespace VoxelEngine.World;

public sealed class ChunkWorker : IDisposable
{
    private readonly World _world;
    private readonly WorldGenerator _generator;
    private readonly ConcurrentQueue<ChunkJob> _jobQueue = new();
    private readonly ConcurrentQueue<ChunkResult> _resultQueue = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly Task[] _workers;

    private int _activeJobs;
    private bool _disposed;

    public ChunkWorker(World world, WorldGenerator generator, int? workerCount = null)
    {
        _world = world;
        _generator = generator;

        int count = workerCount ?? Math.Max(2, Environment.ProcessorCount - 1);
        _workers = Enumerable.Range(0, count)
            .Select(_ => Task.Run(() => WorkerLoop(_cts.Token)))
            .ToArray();
    }

    public int PendingJobCount => _jobQueue.Count + Volatile.Read(ref _activeJobs);
    public int PendingResultCount => _resultQueue.Count;

    public void EnqueueJob(int chunkX, int chunkZ)
        => _jobQueue.Enqueue(new ChunkJob(chunkX, chunkZ, ChunkJobKind.Generate));

    public void EnqueueRebuild(int chunkX, int chunkZ)
        => _jobQueue.Enqueue(new ChunkJob(chunkX, chunkZ, ChunkJobKind.Rebuild));

    public bool TryDequeueResult(out ChunkResult result)
        => _resultQueue.TryDequeue(out result!);

    private void WorkerLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            if (_jobQueue.TryDequeue(out var job))
            {
                if (ct.IsCancellationRequested)
                    break;

                Interlocked.Increment(ref _activeJobs);
                try
                {
                    Chunk? chunk = job.Kind switch
                    {
                        ChunkJobKind.Generate => GenerateChunk(job.ChunkX, job.ChunkZ),
                        ChunkJobKind.Rebuild => _world.GetChunk(job.ChunkX, job.ChunkZ),
                        _ => null
                    };

                    if (chunk is null)
                        continue;
                    if (ct.IsCancellationRequested)
                        continue;

                    var (ov, oi, cv, ci, tv, ti) = GreedyMeshBuilder.Build(chunk, _world, _generator);
                    if (ct.IsCancellationRequested)
                        continue;
                    _resultQueue.Enqueue(new ChunkResult(job.ChunkX, job.ChunkZ, chunk, ov, oi, cv, ci, tv, ti));
                }
                finally
                {
                    Interlocked.Decrement(ref _activeJobs);
                }
            }
            else
            {
                ct.WaitHandle.WaitOne(1);
            }
        }
    }

    private Chunk GenerateChunk(int chunkX, int chunkZ)
    {
        var chunk = _generator.GenerateChunk(chunkX, chunkZ);

        var edits = _world.TakePersistedEdits(chunkX, chunkZ);
        if (edits is not null)
        {
            chunk.LoadEdits(edits);
            chunk.ApplyPlayerEdits();
        }

        _world.AddChunk(chunk);
        return chunk;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _cts.Cancel();
        try
        {
            Task.WaitAll(_workers, TimeSpan.FromSeconds(1));
        }
        catch (AggregateException)
        {
        }
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
