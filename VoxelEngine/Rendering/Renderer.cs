using Silk.NET.OpenGL;

namespace VoxelEngine.Rendering;

public class Renderer : IDisposable
{
    private readonly GL           _gl;
    private Shader        _shader        = null!;
    private ChunkRenderer _chunkRenderer = null!;

    public Renderer(GL gl)
    {
        _gl = gl;
        Initialize();
    }

    private void Initialize()
    {
        _shader = new Shader(_gl, "Assets/Shaders/basic.vert", "Assets/Shaders/basic.frag");

        // AtlasTexture wird intern von ChunkRenderer verwaltet
        _chunkRenderer = new ChunkRenderer(_gl, _shader);
    }

    public bool IsWireframe
    {
        get => _chunkRenderer.IsWireframe;
        set => _chunkRenderer.IsWireframe = value;
    }

    public int VisibleChunkCount => _chunkRenderer.VisibleChunkCount;

    public void BuildWorldMeshes(World.World world)
        => _chunkRenderer.BuildMeshes(world);

    public void BuildChunkMesh(World.Chunk chunk, World.World world)
        => _chunkRenderer.BuildMesh(chunk, world);

    public void RemoveChunkMesh(int chunkX, int chunkZ)
        => _chunkRenderer.RemoveMesh(chunkX, chunkZ);

    public void Render(Camera camera, double _)
        => _chunkRenderer.Render(_shader, camera);

    public void Dispose()
    {
        _chunkRenderer.Dispose();
        _shader.Dispose();
        GC.SuppressFinalize(this);
    }
}
