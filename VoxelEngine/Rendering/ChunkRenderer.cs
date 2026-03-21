using Silk.NET.Maths;
using Silk.NET.OpenGL;
using VoxelEngine.World;

namespace VoxelEngine.Rendering;

public class ChunkRenderer : IDisposable
{
    private readonly GL      _gl;
    private readonly Shader  _shader;
    private readonly Texture _texture;

    private readonly Dictionary<(int X, int Z), Mesh> _meshes = new();

    public ChunkRenderer(GL gl, Shader shader, Texture texture)
    {
        _gl      = gl;
        _shader  = shader;
        _texture = texture;
    }

    public void BuildMeshes(World.World world)
    {
        foreach (var mesh in _meshes.Values)
            mesh.Dispose();
        _meshes.Clear();

        foreach (var chunk in world.GetAllChunks())
        {
            var (vertices, indices) = ChunkMeshBuilder.Build(chunk, world);
            if (vertices.Length == 0)
                continue;

            _meshes[chunk.ChunkPosition] = new Mesh(_gl, vertices, indices);
        }
    }

    public void Render(Shader shader, Camera camera, Texture texture)
    {
        _gl.Enable(GLEnum.DepthTest);
        _gl.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);
        _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        shader.Use();
        shader.SetMatrix4("model",      Matrix4X4<float>.Identity);
        shader.SetMatrix4("view",       camera.ViewMatrix);
        shader.SetMatrix4("projection", camera.ProjectionMatrix);

        texture.Bind(TextureUnit.Texture0);
        shader.SetInt("uTexture", 0);

        foreach (var mesh in _meshes.Values)
            mesh.Draw();
    }

    public void Dispose()
    {
        foreach (var mesh in _meshes.Values)
            mesh.Dispose();
        _meshes.Clear();
        GC.SuppressFinalize(this);
    }
}
