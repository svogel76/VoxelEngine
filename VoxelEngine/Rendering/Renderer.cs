using Silk.NET.OpenGL;
using VoxelEngine.Core;
using VoxelEngine.World;

namespace VoxelEngine.Rendering;

public class Renderer : IDisposable
{
    private readonly GL             _gl;
    private readonly EngineSettings _settings;
    private Shader        _shader        = null!;
    private ChunkRenderer _chunkRenderer = null!;
    private Skybox        _skybox        = null!;
    private BlockHighlightRenderer _blockHighlight = null!;

    public Skybox       Skybox => _skybox;

    /// <summary>Block-ArrayTexture — wird an HUD-Renderer weitergereicht für Block-Icons.</summary>
    public ArrayTexture Atlas  => _chunkRenderer.Atlas;

    public Renderer(GL gl, EngineSettings settings)
    {
        _gl       = gl;
        _settings = settings;
        Initialize();
    }

    private void Initialize()
    {
        _shader        = new Shader(_gl, "Assets/Shaders/basic.vert", "Assets/Shaders/basic.frag");
        _chunkRenderer = new ChunkRenderer(_gl, _shader, _settings);
        _skybox        = new Skybox(_gl);
        _blockHighlight = new BlockHighlightRenderer(_gl);
    }

    public float FogStartFactor => _chunkRenderer.FogStartFactor;
    public float FogEndFactor   => _chunkRenderer.FogEndFactor;

    public void SetFog(float startFactor, float endFactor)
    {
        _chunkRenderer.FogStartFactor = startFactor;
        _chunkRenderer.FogEndFactor   = endFactor;
    }

    public bool IsWireframe
    {
        get => _chunkRenderer.IsWireframe;
        set => _chunkRenderer.IsWireframe = value;
    }

    public int VisibleChunkCount => _chunkRenderer.VisibleChunkCount;
    public int TotalVertexCount  => _chunkRenderer.TotalVertexCount;

    public void UploadPendingMeshes(ChunkManager chunkManager)
        => _chunkRenderer.UploadPendingMeshes(chunkManager);

    public void RemoveChunkMesh(int chunkX, int chunkZ)
        => _chunkRenderer.RemoveMesh(chunkX, chunkZ);

    public void Render(Camera camera, WorldTime time, float deltaTime, BlockRaycastHit? targetedBlock, BlockPlacementPreview? placementPreview)
    {
        _gl.ClearColor(0f, 0f, 0f, 1f);
        _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        _skybox.Render(camera, time, deltaTime);
        _chunkRenderer.Render(_shader, camera, _skybox, time);
        _chunkRenderer.RenderGhostBlock(_shader, camera, _skybox, time, placementPreview);
        _blockHighlight.Render(camera, targetedBlock);
    }

    public void Dispose()
    {
        _skybox.Dispose();
        _blockHighlight.Dispose();
        _chunkRenderer.Dispose();
        _shader.Dispose();
        GC.SuppressFinalize(this);
    }
}
