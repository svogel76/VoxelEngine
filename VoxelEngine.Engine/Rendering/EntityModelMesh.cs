using Silk.NET.OpenGL;

namespace VoxelEngine.Rendering;

public sealed class EntityModelMesh : IDisposable
{
    private const int FloatsPerVertex = 10;
    private const int FloatsPerInstance = 16;

    private readonly GL _gl;
    private readonly uint _vao;
    private readonly uint _vertexBuffer;
    private readonly uint _indexBuffer;
    private readonly uint _instanceBuffer;
    private readonly uint _indexCount;
    private int _instanceCapacity;

    public EntityModelMesh(GL gl, float[] vertices, uint[] indices)
    {
        _gl = gl;
        _indexCount = (uint)indices.Length;

        _vao = gl.GenVertexArray();
        _vertexBuffer = gl.GenBuffer();
        _indexBuffer = gl.GenBuffer();
        _instanceBuffer = gl.GenBuffer();

        gl.BindVertexArray(_vao);

        gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vertexBuffer);
        gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(vertices.Length * sizeof(float)), vertices.AsSpan(), BufferUsageARB.StaticDraw);

        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _indexBuffer);
        gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(indices.Length * sizeof(uint)), indices.AsSpan(), BufferUsageARB.StaticDraw);

        uint vertexStride = (uint)(FloatsPerVertex * sizeof(float));
        unsafe
        {
            gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, vertexStride, (void*)0);
            gl.EnableVertexAttribArray(0);

            gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, vertexStride, (void*)(3 * sizeof(float)));
            gl.EnableVertexAttribArray(1);

            gl.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, false, vertexStride, (void*)(5 * sizeof(float)));
            gl.EnableVertexAttribArray(2);

            gl.VertexAttribPointer(3, 1, VertexAttribPointerType.Float, false, vertexStride, (void*)(9 * sizeof(float)));
            gl.EnableVertexAttribArray(3);
        }

        gl.BindBuffer(BufferTargetARB.ArrayBuffer, _instanceBuffer);
        uint instanceStride = (uint)(FloatsPerInstance * sizeof(float));
        for (uint column = 0; column < 4; column++)
        {
            unsafe
            {
                gl.VertexAttribPointer(4 + column, 4, VertexAttribPointerType.Float, false, instanceStride, (void*)(column * 4 * sizeof(float)));
            }

            gl.EnableVertexAttribArray(4 + column);
            gl.VertexAttribDivisor(4 + column, 1);
        }

        gl.BindVertexArray(0);
    }

    public void UpdateInstances(float[] matrices, int instanceCount)
    {
        if (instanceCount <= 0)
            return;

        int requiredFloatCount = instanceCount * FloatsPerInstance;
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _instanceBuffer);

        if (_instanceCapacity < requiredFloatCount)
        {
            _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(requiredFloatCount * sizeof(float)), matrices.AsSpan(0, requiredFloatCount), BufferUsageARB.DynamicDraw);
            _instanceCapacity = requiredFloatCount;
            return;
        }

        _gl.BufferSubData(BufferTargetARB.ArrayBuffer, 0, (nuint)(requiredFloatCount * sizeof(float)), matrices.AsSpan(0, requiredFloatCount));
    }

    public unsafe void DrawInstanced(int instanceCount)
    {
        if (instanceCount <= 0)
            return;

        _gl.BindVertexArray(_vao);
        _gl.DrawElementsInstanced(PrimitiveType.Triangles, _indexCount, DrawElementsType.UnsignedInt, (void*)0, (uint)instanceCount);
    }

    public void Dispose()
    {
        _gl.DeleteVertexArray(_vao);
        _gl.DeleteBuffer(_vertexBuffer);
        _gl.DeleteBuffer(_indexBuffer);
        _gl.DeleteBuffer(_instanceBuffer);
        GC.SuppressFinalize(this);
    }
}
