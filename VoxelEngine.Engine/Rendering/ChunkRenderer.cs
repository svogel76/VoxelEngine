using System.Numerics;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using VoxelEngine.Core;
using VoxelEngine.World;

namespace VoxelEngine.Rendering;

public class ChunkRenderer : IDisposable
{
        private const float GhostScale = 1.002f;

    // Sicherer Fog-Disable-Wert: weit genug von FLT_MAX entfernt,
    // um NaN durch float.MaxValue-Arithmetik in GLSL zu vermeiden.
    private const float FogDisabledStart = 1e30f;
    private const float FogDisabledEnd   = 2e30f;

    private readonly GL _gl;
    private readonly Shader _shader;
    private readonly ArrayTexture _atlas;
    private readonly int _renderDistance;
    private readonly int _maxUploadsPerFrame;

    /// <summary>Die Block-ArrayTexture — zugänglich für HUD-Renderer (Icons).</summary>
    public ArrayTexture Atlas => _atlas;

    private readonly Dictionary<(int X, int Z), Mesh> _opaqueMeshes = new();
    private readonly Dictionary<(int X, int Z), Mesh> _cutoutMeshes = new();
    private readonly Dictionary<(int X, int Z), Mesh> _transparentMeshes = new();
    private readonly FrustumCuller _frustumCuller = new();
    private readonly Dictionary<byte, Mesh> _ghostMeshes = new();

    public bool IsWireframe { get; set; }
    public int VisibleChunkCount => _frustumCuller.LastVisibleCount;
    public int TotalVertexCount { get; private set; }
    public float FogStartFactor { get; set; }
    public float FogEndFactor { get; set; }

    public ChunkRenderer(GL gl, Shader shader, EngineSettings settings)
    {
        _gl = gl;
        _shader = shader;
        _atlas = new ArrayTexture(gl);
        _renderDistance = settings.RenderDistance;
        _maxUploadsPerFrame = Math.Max(1, settings.MaxGlUploadsPerFrame);
        FogStartFactor = settings.FogStartFactor;
        FogEndFactor = settings.FogEndFactor;
        _ghostMeshes[BlockType.Grass] = new Mesh(gl, CreateGhostVertices(BlockType.Grass), CreateGhostIndices());
        _ghostMeshes[BlockType.Dirt] = new Mesh(gl, CreateGhostVertices(BlockType.Dirt), CreateGhostIndices());
        _ghostMeshes[BlockType.Stone] = new Mesh(gl, CreateGhostVertices(BlockType.Stone), CreateGhostIndices());
        _ghostMeshes[BlockType.Sand] = new Mesh(gl, CreateGhostVertices(BlockType.Sand), CreateGhostIndices());
    }

    public void UploadPendingMeshes(ChunkManager chunkManager)
    {
        int uploaded = 0;

        while (uploaded < _maxUploadsPerFrame &&
               chunkManager.TryDequeueResult(out var result))
        {
            var key = (result.ChunkX, result.ChunkZ);

            if (_opaqueMeshes.TryGetValue(key, out var oldOpaque))
            {
                oldOpaque.Dispose();
                _opaqueMeshes.Remove(key);
            }

            if (_cutoutMeshes.TryGetValue(key, out var oldCutout))
            {
                oldCutout.Dispose();
                _cutoutMeshes.Remove(key);
            }

            if (_transparentMeshes.TryGetValue(key, out var oldTransparent))
            {
                oldTransparent.Dispose();
                _transparentMeshes.Remove(key);
            }

            if (result.HasOpaqueMesh)
                _opaqueMeshes[key] = new Mesh(_gl, result.OpaqueVertices, result.OpaqueIndices);
            if (result.HasCutoutMesh)
                _cutoutMeshes[key] = new Mesh(_gl, result.CutoutVertices, result.CutoutIndices);
            if (result.HasTransparentMesh)
                _transparentMeshes[key] = new Mesh(_gl, result.TransparentVertices, result.TransparentIndices);

            uploaded++;
        }
    }

    public void RemoveMesh(int chunkX, int chunkZ)
    {
        var key = (chunkX, chunkZ);

        if (_opaqueMeshes.TryGetValue(key, out var opaque))
        {
            opaque.Dispose();
            _opaqueMeshes.Remove(key);
        }

        if (_cutoutMeshes.TryGetValue(key, out var cutout))
        {
            cutout.Dispose();
            _cutoutMeshes.Remove(key);
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
        float t = (float)time.Time;
        bool isNight = t < 6.0f || t > 20.0f;
        float startFactor = FogEndFactor <= 0f
            ? FogStartFactor
            : isNight
                ? MathF.Max(FogStartFactor, 0.7f)
                : FogStartFactor;

        float fogStart = FogEndFactor <= 0f ? FogDisabledStart : renderDist * startFactor;
        float fogEnd   = FogEndFactor <= 0f ? FogDisabledEnd   : renderDist * FogEndFactor;

        shader.Use();
        shader.SetMatrix4("model", Matrix4X4<float>.Identity);
        shader.SetMatrix4("view", camera.ViewMatrix);
        shader.SetMatrix4("projection", camera.ProjectionMatrix);
        shader.SetFloat("uGlobalLight", skybox.CurrentAmbientLight);
        shader.SetVector3("uSunColor", skybox.CurrentSunColor);
        shader.SetVector3("uFogColor", skybox.FogColor);
        shader.SetFloat("uFogStart", fogStart);
        shader.SetFloat("uFogEnd", fogEnd);
        shader.SetFloat("uAlphaMultiplier", 1f);

        _atlas.Bind(TextureUnit.Texture0);
        shader.SetInt("uTexture", 0);

        var vp = camera.ViewMatrix * camera.ProjectionMatrix;
        _frustumCuller.Update(vp);
        TotalVertexCount = 0;

        _gl.PolygonMode(GLEnum.FrontAndBack, IsWireframe ? GLEnum.Line : GLEnum.Fill);

        _gl.DepthMask(true);
        _gl.Disable(GLEnum.Blend);
        _gl.Enable(GLEnum.CullFace);
        _gl.CullFace(GLEnum.Back);

        foreach (var (pos, mesh) in _opaqueMeshes)
        {
            if (!_frustumCuller.IsChunkVisible(pos.X, pos.Z))
                continue;

            TotalVertexCount += mesh.VertexCount;
            mesh.Draw();
        }

        if (_cutoutMeshes.Count > 0)
        {
            _gl.Disable(GLEnum.CullFace);
            _gl.DepthMask(true);
            _gl.Disable(GLEnum.Blend);

            foreach (var (pos, mesh) in _cutoutMeshes)
            {
                if (!_frustumCuller.IsChunkVisible(pos.X, pos.Z))
                    continue;

                TotalVertexCount += mesh.VertexCount;
                mesh.Draw();
            }
        }

        if (_transparentMeshes.Count > 0)
        {
            _gl.Disable(GLEnum.CullFace);
            _gl.Enable(GLEnum.Blend);
            _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            _gl.DepthMask(false);

            var camPos = new Vector3(camera.Position.X, camera.Position.Y, camera.Position.Z);
            var sortedTransparent = _transparentMeshes
                .Where(kv => _frustumCuller.IsChunkVisible(kv.Key.X, kv.Key.Z))
                .OrderByDescending(kv =>
                    Vector3.DistanceSquared(
                        new Vector3(kv.Key.X * Chunk.Width, 0f, kv.Key.Z * Chunk.Depth),
                        new Vector3(camPos.X, 0f, camPos.Z)))
                .ToList();

            foreach (var (_, mesh) in sortedTransparent)
            {
                TotalVertexCount += mesh.VertexCount;
                mesh.Draw();
            }

            _gl.DepthMask(true);
            _gl.Disable(GLEnum.Blend);
        }

        _gl.Enable(GLEnum.CullFace);
        _gl.CullFace(GLEnum.Back);
        _gl.PolygonMode(GLEnum.FrontAndBack, GLEnum.Fill);
    }

    public void RenderGhostBlock(Shader shader, Camera camera, Skybox skybox, WorldTime time, BlockPlacementPreview? preview)
    {
        if (preview is null)
            return;

        float renderDist = _renderDistance * (float)Chunk.Width;
        float t = (float)time.Time;
        bool isNight = t < 6.0f || t > 20.0f;
        float startFactor = FogEndFactor <= 0f
            ? FogStartFactor
            : isNight
                ? MathF.Max(FogStartFactor, 0.7f)
                : FogStartFactor;

        float fogStart = FogEndFactor <= 0f ? FogDisabledStart : renderDist * startFactor;
        float fogEnd   = FogEndFactor <= 0f ? FogDisabledEnd   : renderDist * FogEndFactor;

        var ghost = preview.Value;
        var pos = ghost.Position;

        if (!_ghostMeshes.TryGetValue(ghost.BlockType, out var ghostMesh))
            return;

        var model = Matrix4X4.CreateScale(GhostScale)
                  * Matrix4X4.CreateTranslation(new Vector3D<float>(
                        pos.X - (GhostScale - 1f) * 0.5f,
                        pos.Y - (GhostScale - 1f) * 0.5f,
                        pos.Z - (GhostScale - 1f) * 0.5f));

        _gl.Enable(GLEnum.DepthTest);
        _gl.Enable(GLEnum.Blend);
        _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        _gl.DepthMask(false);
        _gl.Enable(GLEnum.CullFace);
        _gl.CullFace(GLEnum.Back);

        shader.Use();
        shader.SetMatrix4("model", model);
        shader.SetMatrix4("view", camera.ViewMatrix);
        shader.SetMatrix4("projection", camera.ProjectionMatrix);
        shader.SetFloat("uGlobalLight", skybox.CurrentAmbientLight);
        shader.SetVector3("uSunColor", skybox.CurrentSunColor);
        shader.SetVector3("uFogColor", skybox.FogColor);
        shader.SetFloat("uFogStart", fogStart);
        shader.SetFloat("uFogEnd", fogEnd);
        shader.SetFloat("uAlphaMultiplier", 0.4f);

        _atlas.Bind(TextureUnit.Texture0);
        shader.SetInt("uTexture", 0);

        ghostMesh.Draw();

        shader.SetFloat("uAlphaMultiplier", 1f);
        _gl.DepthMask(true);
        _gl.Disable(GLEnum.Blend);
    }

    private static float[] CreateGhostVertices(byte blockType)
    {
        var vertices = new List<float>(6 * 4 * 9);
        bool isCutout = BlockRegistry.IsCutout(blockType);

        AddFace(vertices,
            (0f, 1f, 0f), (0f, 1f, 1f), (1f, 1f, 1f), (1f, 1f, 0f),
            BlockRegistry.Get(blockType).GetTile(FaceDirection.Top),
            3f, 1f, isCutout);

        AddFace(vertices,
            (0f, 0f, 0f), (1f, 0f, 0f), (1f, 0f, 1f), (0f, 0f, 1f),
            BlockRegistry.Get(blockType).GetTile(FaceDirection.Bottom),
            3f, 0.4f, isCutout);

        AddFace(vertices,
            (0f, 0f, 1f), (1f, 0f, 1f), (1f, 1f, 1f), (0f, 1f, 1f),
            BlockRegistry.Get(blockType).GetTile(FaceDirection.Front),
            3f, 0.8f, isCutout);

        AddFace(vertices,
            (1f, 0f, 0f), (0f, 0f, 0f), (0f, 1f, 0f), (1f, 1f, 0f),
            BlockRegistry.Get(blockType).GetTile(FaceDirection.Back),
            3f, 0.6f, isCutout);

        AddFace(vertices,
            (0f, 0f, 0f), (0f, 0f, 1f), (0f, 1f, 1f), (0f, 1f, 0f),
            BlockRegistry.Get(blockType).GetTile(FaceDirection.Left),
            3f, 0.7f, isCutout);

        AddFace(vertices,
            (1f, 0f, 1f), (1f, 0f, 0f), (1f, 1f, 0f), (1f, 1f, 1f),
            BlockRegistry.Get(blockType).GetTile(FaceDirection.Right),
            3f, 0.7f, isCutout);

        return vertices.ToArray();
    }

    private static uint[] CreateGhostIndices()
    {
        var indices = new List<uint>(6 * 6);

        for (uint face = 0; face < 6; face++)
        {
            uint baseIndex = face * 4;
            indices.Add(baseIndex + 0);
            indices.Add(baseIndex + 1);
            indices.Add(baseIndex + 2);
            indices.Add(baseIndex + 0);
            indices.Add(baseIndex + 2);
            indices.Add(baseIndex + 3);
        }

        return indices.ToArray();
    }

    private static void AddFace(
        List<float> vertices,
        (float X, float Y, float Z) v0,
        (float X, float Y, float Z) v1,
        (float X, float Y, float Z) v2,
        (float X, float Y, float Z) v3,
        int tileLayer,
        float ao,
        float faceLight,
        bool isCutout)
    {
        AddVertex(vertices, v0, 0f, 0f, tileLayer, ao, faceLight, isCutout);
        AddVertex(vertices, v1, 1f, 0f, tileLayer, ao, faceLight, isCutout);
        AddVertex(vertices, v2, 1f, 1f, tileLayer, ao, faceLight, isCutout);
        AddVertex(vertices, v3, 0f, 1f, tileLayer, ao, faceLight, isCutout);
    }

    private static void AddVertex(
        List<float> vertices,
        (float X, float Y, float Z) position,
        float u,
        float v,
        int tileLayer,
        float ao,
        float faceLight,
        bool isCutout)
    {
        vertices.Add(position.X);
        vertices.Add(position.Y);
        vertices.Add(position.Z);
        vertices.Add(u);
        vertices.Add(v);
        vertices.Add(tileLayer);
        vertices.Add(ao);
        vertices.Add(faceLight);
        vertices.Add(isCutout ? 1f : 0f);
    }

    public void Dispose()
    {
        foreach (var mesh in _opaqueMeshes.Values)
            mesh.Dispose();
        foreach (var mesh in _cutoutMeshes.Values)
            mesh.Dispose();
        foreach (var mesh in _transparentMeshes.Values)
            mesh.Dispose();
        foreach (var mesh in _ghostMeshes.Values)
            mesh.Dispose();

        _opaqueMeshes.Clear();
        _cutoutMeshes.Clear();
        _transparentMeshes.Clear();
        _ghostMeshes.Clear();
        _atlas.Dispose();
        GC.SuppressFinalize(this);
    }
}

