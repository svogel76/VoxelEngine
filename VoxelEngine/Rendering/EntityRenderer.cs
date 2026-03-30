using System.Numerics;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using VoxelEngine.Entity;
using VoxelEngine.Entity.Models;
using VoxelEngine.World;

namespace VoxelEngine.Rendering;

public sealed class EntityRenderer : IDisposable
{
    private readonly GL _gl;
    private readonly Shader _shader;
    private readonly Texture _atlas;
    private readonly Dictionary<string, EntityModelMesh> _meshes = new(StringComparer.OrdinalIgnoreCase);

    public EntityRenderer(GL gl, IEntityModelLibrary models)
    {
        ArgumentNullException.ThrowIfNull(models);

        _gl = gl;
        _shader = new Shader(gl, "Assets/Shaders/entity.vert", "Assets/Shaders/entity.frag");
        _atlas = new Texture(gl, models.Atlas.Path);

        foreach (var model in models.GetAllModels())
            _meshes[model.Id] = CreateMesh(gl, model, models.Atlas);
    }

    public void Render(Camera camera, Skybox skybox, WorldTime time, EntityManager entityManager, float fogStartFactor, float fogEndFactor, int renderDistance)
    {
        var frustum = ViewFrustum.FromViewProjection(ToNumerics(camera.ViewMatrix * camera.ProjectionMatrix));
        var visibleEntities = entityManager.GetVisible(frustum);
        if (visibleEntities.Count == 0)
            return;

        float renderDistanceBlocks = renderDistance * Chunk.Width;
        float hours = (float)time.Time;
        bool isNight = hours < 6.0f || hours > 20.0f;
        float startFactor = fogEndFactor <= 0f
            ? fogStartFactor
            : isNight
                ? MathF.Max(fogStartFactor, 0.7f)
                : fogStartFactor;

        float fogStart = fogEndFactor <= 0f ? 1e30f : renderDistanceBlocks * startFactor;
        float fogEnd = fogEndFactor <= 0f ? 2e30f : renderDistanceBlocks * fogEndFactor;

        _gl.Enable(GLEnum.DepthTest);
        _gl.Enable(GLEnum.CullFace);
        _gl.CullFace(GLEnum.Back);
        _gl.FrontFace(GLEnum.Ccw);
        _gl.Enable(GLEnum.Blend);
        _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        _gl.DepthMask(true);

        _shader.Use();
        _shader.SetMatrix4("view", camera.ViewMatrix);
        _shader.SetMatrix4("projection", camera.ProjectionMatrix);
        _shader.SetFloat("uGlobalLight", skybox.CurrentAmbientLight);
        _shader.SetVector3("uSunColor", skybox.CurrentSunColor);
        _shader.SetVector3("uFogColor", skybox.FogColor);
        _shader.SetFloat("uFogStart", fogStart);
        _shader.SetFloat("uFogEnd", fogEnd);

        _atlas.Bind(TextureUnit.Texture0);
        _shader.SetInt("uTexture", 0);

        var batches = new Dictionary<string, List<float>>(StringComparer.OrdinalIgnoreCase);
        foreach (var entity in visibleEntities)
        {
            if (entity is not IEntityRenderDataProvider renderDataProvider)
                continue;

            var renderInstance = renderDataProvider.GetRenderInstance();
            if (!_meshes.ContainsKey(renderInstance.ModelId))
                continue;

            if (!batches.TryGetValue(renderInstance.ModelId, out var transforms))
            {
                transforms = new List<float>();
                batches.Add(renderInstance.ModelId, transforms);
            }

            AddMatrix(
                transforms,
                Matrix4x4.CreateRotationY(renderInstance.YawRadians) * Matrix4x4.CreateTranslation(renderInstance.Position));
        }

        foreach (var (modelId, matrices) in batches)
        {
            if (!_meshes.TryGetValue(modelId, out var mesh))
                continue;

            int instanceCount = matrices.Count / 16;
            if (instanceCount == 0)
                continue;

            mesh.UpdateInstances(matrices.ToArray(), instanceCount);
            mesh.DrawInstanced(instanceCount);
        }

        _gl.Disable(GLEnum.Blend);
    }

    private static EntityModelMesh CreateMesh(GL gl, IVoxelModelDefinition model, EntityAtlasDefinition atlas)
    {
        var vertices = new List<float>();
        var indices = new List<uint>();
        var occupied = new HashSet<(int X, int Y, int Z)>(model.Voxels.Select(static voxel => (voxel.X, voxel.Y, voxel.Z)));
        var pivot = CreatePivot(model);

        foreach (var voxel in model.Voxels)
        {
            AddFaceIfVisible(vertices, indices, occupied, voxel, model.VoxelSize, pivot, atlas, (voxel.X, voxel.Y + 1, voxel.Z), Face.Top);
            AddFaceIfVisible(vertices, indices, occupied, voxel, model.VoxelSize, pivot, atlas, (voxel.X, voxel.Y - 1, voxel.Z), Face.Bottom);
            AddFaceIfVisible(vertices, indices, occupied, voxel, model.VoxelSize, pivot, atlas, (voxel.X, voxel.Y, voxel.Z + 1), Face.Front);
            AddFaceIfVisible(vertices, indices, occupied, voxel, model.VoxelSize, pivot, atlas, (voxel.X, voxel.Y, voxel.Z - 1), Face.Back);
            AddFaceIfVisible(vertices, indices, occupied, voxel, model.VoxelSize, pivot, atlas, (voxel.X - 1, voxel.Y, voxel.Z), Face.Left);
            AddFaceIfVisible(vertices, indices, occupied, voxel, model.VoxelSize, pivot, atlas, (voxel.X + 1, voxel.Y, voxel.Z), Face.Right);
        }

        return new EntityModelMesh(gl, vertices.ToArray(), indices.ToArray());
    }

    private static void AddFaceIfVisible(
        List<float> vertices,
        List<uint> indices,
        HashSet<(int X, int Y, int Z)> occupied,
        VoxelModelVoxel voxel,
        float voxelSize,
        Vector3 pivot,
        EntityAtlasDefinition atlas,
        (int X, int Y, int Z) neighbor,
        Face face)
    {
        if (occupied.Contains(neighbor))
            return;

        uint baseIndex = (uint)(vertices.Count / 10);
        var uv = GetAtlasUv(voxel.TileX, voxel.TileY, atlas);
        var color = voxel.Tint;
        float shade = GetFaceShade(face);
        float x = voxel.X * voxelSize - pivot.X;
        float y = voxel.Y * voxelSize - pivot.Y;
        float z = voxel.Z * voxelSize - pivot.Z;
        float s = voxelSize;

        switch (face)
        {
            case Face.Top:
                AddVertex(vertices, x, y + s, z, uv.U0, uv.V0, color, shade);
                AddVertex(vertices, x, y + s, z + s, uv.U0, uv.V1, color, shade);
                AddVertex(vertices, x + s, y + s, z + s, uv.U1, uv.V1, color, shade);
                AddVertex(vertices, x + s, y + s, z, uv.U1, uv.V0, color, shade);
                break;
            case Face.Bottom:
                AddVertex(vertices, x, y, z, uv.U0, uv.V0, color, shade);
                AddVertex(vertices, x + s, y, z, uv.U1, uv.V0, color, shade);
                AddVertex(vertices, x + s, y, z + s, uv.U1, uv.V1, color, shade);
                AddVertex(vertices, x, y, z + s, uv.U0, uv.V1, color, shade);
                break;
            case Face.Front:
                AddVertex(vertices, x, y, z + s, uv.U0, uv.V0, color, shade);
                AddVertex(vertices, x + s, y, z + s, uv.U1, uv.V0, color, shade);
                AddVertex(vertices, x + s, y + s, z + s, uv.U1, uv.V1, color, shade);
                AddVertex(vertices, x, y + s, z + s, uv.U0, uv.V1, color, shade);
                break;
            case Face.Back:
                AddVertex(vertices, x + s, y, z, uv.U0, uv.V0, color, shade);
                AddVertex(vertices, x, y, z, uv.U1, uv.V0, color, shade);
                AddVertex(vertices, x, y + s, z, uv.U1, uv.V1, color, shade);
                AddVertex(vertices, x + s, y + s, z, uv.U0, uv.V1, color, shade);
                break;
            case Face.Left:
                AddVertex(vertices, x, y, z, uv.U0, uv.V0, color, shade);
                AddVertex(vertices, x, y, z + s, uv.U1, uv.V0, color, shade);
                AddVertex(vertices, x, y + s, z + s, uv.U1, uv.V1, color, shade);
                AddVertex(vertices, x, y + s, z, uv.U0, uv.V1, color, shade);
                break;
            case Face.Right:
                AddVertex(vertices, x + s, y, z + s, uv.U0, uv.V0, color, shade);
                AddVertex(vertices, x + s, y, z, uv.U1, uv.V0, color, shade);
                AddVertex(vertices, x + s, y + s, z, uv.U1, uv.V1, color, shade);
                AddVertex(vertices, x + s, y + s, z + s, uv.U0, uv.V1, color, shade);
                break;
        }

        indices.Add(baseIndex + 0);
        indices.Add(baseIndex + 1);
        indices.Add(baseIndex + 2);
        indices.Add(baseIndex + 0);
        indices.Add(baseIndex + 2);
        indices.Add(baseIndex + 3);
    }

    private static Vector3 CreatePivot(IVoxelModelDefinition model)
    {
        float minX = float.MaxValue;
        float minY = float.MaxValue;
        float minZ = float.MaxValue;
        float maxX = float.MinValue;
        float maxZ = float.MinValue;

        foreach (var voxel in model.Voxels)
        {
            minX = MathF.Min(minX, voxel.X * model.VoxelSize);
            minY = MathF.Min(minY, voxel.Y * model.VoxelSize);
            minZ = MathF.Min(minZ, voxel.Z * model.VoxelSize);
            maxX = MathF.Max(maxX, (voxel.X + 1) * model.VoxelSize);
            maxZ = MathF.Max(maxZ, (voxel.Z + 1) * model.VoxelSize);
        }

        return new Vector3((minX + maxX) * 0.5f, minY, (minZ + maxZ) * 0.5f);
    }

    private static void AddVertex(List<float> vertices, float x, float y, float z, float u, float v, VoxelTint color, float shade)
    {
        vertices.Add(x);
        vertices.Add(y);
        vertices.Add(z);
        vertices.Add(u);
        vertices.Add(v);
        vertices.Add(color.R / 255f);
        vertices.Add(color.G / 255f);
        vertices.Add(color.B / 255f);
        vertices.Add(color.A / 255f);
        vertices.Add(shade);
    }

    private static (float U0, float V0, float U1, float V1) GetAtlasUv(int tileX, int tileY, EntityAtlasDefinition atlas)
    {
        float u0 = tileX / (float)atlas.TileColumns;
        float v0 = tileY / (float)atlas.TileRows;
        float u1 = (tileX + 1) / (float)atlas.TileColumns;
        float v1 = (tileY + 1) / (float)atlas.TileRows;
        return (u0, v0, u1, v1);
    }

    private static float GetFaceShade(Face face) => face switch
    {
        Face.Top => 1.0f,
        Face.Bottom => 0.55f,
        Face.Front => 0.88f,
        Face.Back => 0.68f,
        Face.Left => 0.76f,
        Face.Right => 0.76f,
        _ => 1.0f
    };

    private static void AddMatrix(List<float> target, Matrix4x4 matrix)
    {
        // System.Numerics stores matrices row-major. The instanced mat4 attribute in GLSL
        // expects column-major columns, so we upload the transposed row sequence here.
        target.Add(matrix.M11);
        target.Add(matrix.M12);
        target.Add(matrix.M13);
        target.Add(matrix.M14);
        target.Add(matrix.M21);
        target.Add(matrix.M22);
        target.Add(matrix.M23);
        target.Add(matrix.M24);
        target.Add(matrix.M31);
        target.Add(matrix.M32);
        target.Add(matrix.M33);
        target.Add(matrix.M34);
        target.Add(matrix.M41);
        target.Add(matrix.M42);
        target.Add(matrix.M43);
        target.Add(matrix.M44);
    }

    private static Matrix4x4 ToNumerics(Matrix4X4<float> matrix)
        => new(
            matrix.M11, matrix.M12, matrix.M13, matrix.M14,
            matrix.M21, matrix.M22, matrix.M23, matrix.M24,
            matrix.M31, matrix.M32, matrix.M33, matrix.M34,
            matrix.M41, matrix.M42, matrix.M43, matrix.M44);

    public void Dispose()
    {
        foreach (var mesh in _meshes.Values)
            mesh.Dispose();

        _meshes.Clear();
        _atlas.Dispose();
        _shader.Dispose();
        GC.SuppressFinalize(this);
    }

    private enum Face
    {
        Top,
        Bottom,
        Front,
        Back,
        Left,
        Right
    }
}
