using System.Numerics;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using VoxelEngine.World;

namespace VoxelEngine.Rendering;

public sealed class BlockHighlightRenderer : IDisposable
{
    private const float OutlineScale = 1.002f;

    private readonly GL _gl;
    private readonly Shader _shader;
    private readonly uint _vao;
    private readonly uint _vbo;
    private readonly uint _ebo;

    public unsafe BlockHighlightRenderer(GL gl)
    {
        _gl = gl;
        _shader = new Shader(gl, "Assets/Shaders/highlight.vert", "Assets/Shaders/highlight.frag");

        float[] vertices =
        {
            0f, 0f, 0f,
            1f, 0f, 0f,
            1f, 1f, 0f,
            0f, 1f, 0f,
            0f, 0f, 1f,
            1f, 0f, 1f,
            1f, 1f, 1f,
            0f, 1f, 1f
        };

        uint[] indices =
        {
            0, 1, 1, 2, 2, 3, 3, 0,
            4, 5, 5, 6, 6, 7, 7, 4,
            0, 4, 1, 5, 2, 6, 3, 7
        };

        _vao = gl.GenVertexArray();
        _vbo = gl.GenBuffer();
        _ebo = gl.GenBuffer();

        gl.BindVertexArray(_vao);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(vertices.Length * sizeof(float)), vertices.AsSpan(), BufferUsageARB.StaticDraw);
        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
        gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(indices.Length * sizeof(uint)), indices.AsSpan(), BufferUsageARB.StaticDraw);
        gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), (void*)0);
        gl.EnableVertexAttribArray(0);
        gl.BindVertexArray(0);
    }

    public unsafe void Render(Camera camera, BlockRaycastHit? hit)
    {
        if (hit is null)
            return;

        var block = hit.Value.BlockPosition;
        var model = Matrix4X4.CreateScale(OutlineScale)
                  * Matrix4X4.CreateTranslation(
                        block.X - (OutlineScale - 1f) * 0.5f,
                        block.Y - (OutlineScale - 1f) * 0.5f,
                        block.Z - (OutlineScale - 1f) * 0.5f);

        _gl.Enable(GLEnum.DepthTest);
        _gl.DepthMask(false);
        _gl.Enable(GLEnum.Blend);
        _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        _shader.Use();
        _shader.SetMatrix4("model", model);
        _shader.SetMatrix4("view", camera.ViewMatrix);
        _shader.SetMatrix4("projection", camera.ProjectionMatrix);
        _shader.SetVector4("uColor", 0.92f, 0.94f, 0.97f, 0.9f);

        _gl.BindVertexArray(_vao);
        _gl.DrawElements(PrimitiveType.Lines, 24, DrawElementsType.UnsignedInt, null);
        _gl.BindVertexArray(0);

        _gl.Disable(GLEnum.Blend);
        _gl.DepthMask(true);
    }

    public void Dispose()
    {
        _gl.DeleteVertexArray(_vao);
        _gl.DeleteBuffer(_vbo);
        _gl.DeleteBuffer(_ebo);
        _shader.Dispose();
        GC.SuppressFinalize(this);
    }
}
