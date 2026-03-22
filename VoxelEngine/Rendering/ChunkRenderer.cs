using System.Numerics;
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

    private readonly Dictionary<(int X, int Z), Mesh> _opaqueMeshes      = new();
    private readonly Dictionary<(int X, int Z), Mesh> _transparentMeshes = new();
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
        foreach (var mesh in _opaqueMeshes.Values)      mesh.Dispose();
        foreach (var mesh in _transparentMeshes.Values) mesh.Dispose();
        _opaqueMeshes.Clear();
        _transparentMeshes.Clear();

        foreach (var chunk in world.GetAllChunks())
        {
            var (oVerts, oIdx, tVerts, tIdx) = GreedyMeshBuilder.Build(chunk, world);
            if (oVerts.Length > 0)
                _opaqueMeshes[chunk.ChunkPosition] = new Mesh(_gl, oVerts, oIdx);
            if (tVerts.Length > 0)
                _transparentMeshes[chunk.ChunkPosition] = new Mesh(_gl, tVerts, tIdx);
        }
    }

    public void BuildMesh(Chunk chunk, World.World world)
    {
        var key = chunk.ChunkPosition;

        if (_opaqueMeshes.TryGetValue(key, out var oldOpaque))
        {
            oldOpaque.Dispose();
            _opaqueMeshes.Remove(key);
        }
        if (_transparentMeshes.TryGetValue(key, out var oldTransparent))
        {
            oldTransparent.Dispose();
            _transparentMeshes.Remove(key);
        }

        var (oVerts, oIdx, tVerts, tIdx) = GreedyMeshBuilder.Build(chunk, world);
        if (oVerts.Length > 0)
            _opaqueMeshes[key] = new Mesh(_gl, oVerts, oIdx);
        if (tVerts.Length > 0)
            _transparentMeshes[key] = new Mesh(_gl, tVerts, tIdx);
    }

    public void RemoveMesh(int chunkX, int chunkZ)
    {
        var key = (chunkX, chunkZ);
        if (_opaqueMeshes.TryGetValue(key, out var opaque))
        {
            opaque.Dispose();
            _opaqueMeshes.Remove(key);
        }
        if (_transparentMeshes.TryGetValue(key, out var transparent))
        {
            transparent.Dispose();
            _transparentMeshes.Remove(key);
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
        bool  isNight     = t < 6.0f || t > 20.0f;
        float startFactor = FogEndFactor <= 0f ? FogStartFactor
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

        // ── Pass 1: Opaque ────────────────────────────────────────────────────
        _gl.DepthMask(true);
        _gl.Disable(GLEnum.Blend);

        foreach (var (pos, mesh) in _opaqueMeshes)
        {
            if (!_frustumCuller.IsChunkVisible(pos.X, pos.Z)) continue;
            TotalVertexCount += mesh.VertexCount;
            mesh.Draw();
        }

        // ── Pass 2: Transparent — von hinten nach vorne sortiert ──────────────
        if (_transparentMeshes.Count > 0)
        {
            _gl.Enable(GLEnum.CullFace);  // Rückseiten verwerfen — verhindert Wasser-Innenseiten
            _gl.CullFace(GLEnum.Back);
            _gl.Enable(GLEnum.Blend);
            _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            _gl.DepthMask(false);  // Depth-Write AUS — kritisch!

            var camPos = new Vector3(camera.Position.X, camera.Position.Y, camera.Position.Z);

            var sortedTransparent = _transparentMeshes
                .Where(kv => _frustumCuller.IsChunkVisible(kv.Key.X, kv.Key.Z))
                .OrderByDescending(kv =>
                    Vector3.DistanceSquared(
                        new Vector3(kv.Key.X * Chunk.Width, 0, kv.Key.Z * Chunk.Depth),
                        new Vector3(camPos.X, 0, camPos.Z)))
                .ToList();

            foreach (var (pos, mesh) in sortedTransparent)
            {
                TotalVertexCount += mesh.VertexCount;
                mesh.Draw();
            }

            _gl.DepthMask(true);   // Depth-Write wieder AN
            _gl.Disable(GLEnum.Blend);
        }

        _gl.PolygonMode(GLEnum.FrontAndBack, GLEnum.Fill);
    }

    public void Dispose()
    {
        foreach (var mesh in _opaqueMeshes.Values)      mesh.Dispose();
        foreach (var mesh in _transparentMeshes.Values) mesh.Dispose();
        _opaqueMeshes.Clear();
        _transparentMeshes.Clear();
        _atlas.Dispose();
        GC.SuppressFinalize(this);
    }
}
