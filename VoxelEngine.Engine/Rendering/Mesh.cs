using Silk.NET.OpenGL;

namespace VoxelEngine.Rendering;

public class Mesh : IDisposable
{
    private readonly GL   _gl;
    private readonly uint _vao;
    private readonly uint _vbo;
    private readonly uint _ebo;
    private readonly uint _indexCount;

    /// <summary>Anzahl Vertices (floats / stride 9)</summary>
    public int VertexCount { get; }

    public unsafe Mesh(GL gl, float[] vertices, uint[] indices)
    {
        _gl         = gl;
        _indexCount = (uint)indices.Length;
        VertexCount = vertices.Length / 9;

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

        uint stride = 9 * sizeof(float);

        // Location 0: Position (3 floats)
        gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, (void*)0);
        gl.EnableVertexAttribArray(0);

        // Location 1: TexCoord (2 floats, offset 3 floats)
        gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, (void*)(3 * sizeof(float)));
        gl.EnableVertexAttribArray(1);

        // Location 2: TileLayer (1 float, offset 5 floats)
        gl.VertexAttribPointer(2, 1, VertexAttribPointerType.Float, false, stride, (void*)(5 * sizeof(float)));
        gl.EnableVertexAttribArray(2);

        // Location 3: AO (1 float, offset 6 floats)
        gl.VertexAttribPointer(3, 1, VertexAttribPointerType.Float, false, stride, (void*)(6 * sizeof(float)));
        gl.EnableVertexAttribArray(3);

        // Location 4: FaceLight (1 float, offset 7 floats)
        gl.VertexAttribPointer(4, 1, VertexAttribPointerType.Float, false, stride, (void*)(7 * sizeof(float)));
        gl.EnableVertexAttribArray(4);

        // Location 5: Cutout flag (1 float, offset 8 floats)
        gl.VertexAttribPointer(5, 1, VertexAttribPointerType.Float, false, stride, (void*)(8 * sizeof(float)));
        gl.EnableVertexAttribArray(5);

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
