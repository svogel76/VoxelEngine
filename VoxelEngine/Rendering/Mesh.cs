using Silk.NET.OpenGL;

namespace VoxelEngine.Rendering;

public class Mesh : IDisposable
{
    private readonly GL   _gl;
    private readonly uint _vao;
    private readonly uint _vbo;
    private readonly uint _ebo;
    private readonly uint _indexCount;

    public unsafe Mesh(GL gl, float[] vertices, uint[] indices)
    {
        _gl         = gl;
        _indexCount = (uint)indices.Length;

        _vao = gl.GenVertexArray();
        _vbo = gl.GenBuffer();
        _ebo = gl.GenBuffer();

        gl.BindVertexArray(_vao);

        // Upload vertex data
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        gl.BufferData(BufferTargetARB.ArrayBuffer,
                      (nuint)(vertices.Length * sizeof(float)),
                      vertices.AsSpan(), BufferUsageARB.StaticDraw);

        // Upload index data
        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
        gl.BufferData(BufferTargetARB.ElementArrayBuffer,
                      (nuint)(indices.Length * sizeof(uint)),
                      indices.AsSpan(), BufferUsageARB.StaticDraw);

        uint stride = 5 * sizeof(float);

        // Location 0: Position (3 floats)
        gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, (void*)0);
        gl.EnableVertexAttribArray(0);

        // Location 1: TexCoord (2 floats, offset 3 floats)
        gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, (void*)(3 * sizeof(float)));
        gl.EnableVertexAttribArray(1);

        gl.BindVertexArray(0);
    }

    public unsafe void Draw()
    {
        _gl.BindVertexArray(_vao);
        _gl.DrawElements(PrimitiveType.Triangles, _indexCount, DrawElementsType.UnsignedInt, (void*)0);
    }

    public void Dispose()
    {
        _gl.DeleteVertexArray(_vao);
        _gl.DeleteBuffer(_vbo);
        _gl.DeleteBuffer(_ebo);
    }
}
