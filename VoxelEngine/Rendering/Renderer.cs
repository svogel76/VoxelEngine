using Silk.NET.OpenGL;
using VoxelEngine.World;

namespace VoxelEngine.Rendering;

public class Renderer : IDisposable
{
    private readonly GL           _gl;
    private Shader        _shader        = null!;
    private ChunkRenderer _chunkRenderer = null!;
    private Skybox        _skybox        = null!;

    public Skybox Skybox => _skybox;

    public Renderer(GL gl)
    {
        _gl = gl;
        Initialize();
    }

    private void Initialize()
    {
        _shader        = new Shader(_gl, "Assets/Shaders/basic.vert", "Assets/Shaders/basic.frag");
        _chunkRenderer = new ChunkRenderer(_gl, _shader);
        _skybox        = new Skybox(_gl);
    }

    public bool IsWireframe
    {
        get => _chunkRenderer.IsWireframe;
        set => _chunkRenderer.IsWireframe = value;
    }

    public int VisibleChunkCount => _chunkRenderer.VisibleChunkCount;
    public int TotalVertexCount  => _chunkRenderer.TotalVertexCount;

    public void BuildWorldMeshes(World.World world)
        => _chunkRenderer.BuildMeshes(world);

    public void BuildChunkMesh(World.Chunk chunk, World.World world)
        => _chunkRenderer.BuildMesh(chunk, world);

    public void RemoveChunkMesh(int chunkX, int chunkZ)
        => _chunkRenderer.RemoveMesh(chunkX, chunkZ);

    public void Render(Camera camera, WorldTime time)
    {
        _gl.ClearColor(0f, 0f, 0f, 1f);
        _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        _skybox.Render(camera, time);
        _chunkRenderer.Render(_shader, camera);
    }

    public void Dispose()
    {
        _skybox.Dispose();
        _chunkRenderer.Dispose();
        _shader.Dispose();
        GC.SuppressFinalize(this);
    }
}
