using Silk.NET.Maths;
using Silk.NET.OpenGL;
using VoxelEngine.Core;
using VoxelEngine.World;

namespace VoxelEngine.Rendering;

public class ChunkRenderer : IDisposable
{
    private readonly GL            _gl;
    private readonly Shader        _shader;
    private readonly ArrayTexture  _atlas;
    private readonly int           _renderDistance;

    private readonly Dictionary<(int X, int Z), Mesh> _meshes = new();
    private readonly FrustumCuller _frustumCuller = new();

    public bool  IsWireframe       { get; set; } = false;
    public int   VisibleChunkCount => _frustumCuller.LastVisibleCount;
    public int   TotalVertexCount  { get; private set; }
    public float FogStartFactor    { get; set; }
    public float FogEndFactor      { get; set; }

    public ChunkRenderer(GL gl, Shader shader, EngineSettings settings)
    {
        _gl             = gl;
        _shader         = shader;
        _atlas          = new ArrayTexture(gl);
        _renderDistance = settings.RenderDistance;
        FogStartFactor  = settings.FogStartFactor;
        FogEndFactor    = settings.FogEndFactor;
    }

    public void BuildMeshes(World.World world)
    {
        foreach (var mesh in _meshes.Values)
            mesh.Dispose();
        _meshes.Clear();

        foreach (var chunk in world.GetAllChunks())
        {
            var (vertices, indices) = GreedyMeshBuilder.Build(chunk, world);
            if (vertices.Length == 0)
                continue;

            _meshes[chunk.ChunkPosition] = new Mesh(_gl, vertices, indices);
        }
    }

    public void BuildMesh(Chunk chunk, World.World world)
    {
        if (_meshes.TryGetValue(chunk.ChunkPosition, out var old))
        {
            old.Dispose();
            _meshes.Remove(chunk.ChunkPosition);
        }

        var (vertices, indices) = GreedyMeshBuilder.Build(chunk, world);
        if (vertices.Length == 0)
            return;

        _meshes[chunk.ChunkPosition] = new Mesh(_gl, vertices, indices);
    }

    public void RemoveMesh(int chunkX, int chunkZ)
    {
        var key = (chunkX, chunkZ);
        if (_meshes.TryGetValue(key, out var mesh))
        {
            mesh.Dispose();
            _meshes.Remove(key);
        }
    }

    public void Render(Shader shader, Camera camera, Skybox skybox, WorldTime time)
    {
        _gl.Enable(GLEnum.DepthTest);
        _gl.Enable(GLEnum.CullFace);
        _gl.CullFace(GLEnum.Back);
        _gl.FrontFace(GLEnum.Ccw);

        float renderDist = _renderDistance * (float)Chunk.Width;

        // Nachts Fog-Start weiter rausschieben — Bergsilhouetten bleiben sichtbar
        float t = (float)time.Time;
        bool  isNight       = t < 6.0f || t > 20.0f;
        float startFactor   = FogEndFactor <= 0f ? FogStartFactor
                            : isNight ? MathF.Max(FogStartFactor, 0.7f)
                            : FogStartFactor;

        float fogStart = FogEndFactor <= 0f ? float.MaxValue / 2f : renderDist * startFactor;
        float fogEnd   = FogEndFactor <= 0f ? float.MaxValue      : renderDist * FogEndFactor;

        shader.Use();
        shader.SetMatrix4("model",      Matrix4X4<float>.Identity);
        shader.SetMatrix4("view",       camera.ViewMatrix);
        shader.SetMatrix4("projection", camera.ProjectionMatrix);
        shader.SetFloat("uGlobalLight", skybox.CurrentAmbientLight);
        shader.SetVector3("uSunColor",  skybox.CurrentSunColor);
        shader.SetVector3("uFogColor",  skybox.FogColor);
        shader.SetFloat("uFogStart",    fogStart);
        shader.SetFloat("uFogEnd",      fogEnd);

        _atlas.Bind(TextureUnit.Texture0);
        shader.SetInt("uTexture", 0);

        var vp = camera.ViewMatrix * camera.ProjectionMatrix;
        _frustumCuller.Update(vp);
        TotalVertexCount = 0;

        _gl.PolygonMode(GLEnum.FrontAndBack, IsWireframe ? GLEnum.Line : GLEnum.Fill);

        foreach (var (pos, mesh) in _meshes)
        {
            if (!_frustumCuller.IsChunkVisible(pos.X, pos.Z))
                continue;
            TotalVertexCount += mesh.VertexCount;
            mesh.Draw();
        }

        _gl.PolygonMode(GLEnum.FrontAndBack, GLEnum.Fill);
    }

    public void Dispose()
    {
        foreach (var mesh in _meshes.Values)
            mesh.Dispose();
        _meshes.Clear();
        _atlas.Dispose();
        GC.SuppressFinalize(this);
    }
}
