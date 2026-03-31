using System.Numerics;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace VoxelEngine.Rendering;

public class CelestialBody : IDisposable
{
    private readonly GL     _gl;
    private readonly Shader _shader;
    private readonly uint   _vao;
    private readonly uint   _vbo;
    private readonly uint   _ebo;

    public float   Size    { get; set; } = 0.08f;
    public float   Angle   { get; set; } = 0.0f;
    public Vector3 Color   { get; set; } = Vector3.One;
    public float   Opacity { get; set; } = 1.0f;

    public unsafe CelestialBody(GL gl, Shader shader)
    {
        _gl     = gl;
        _shader = shader;

        float[] vertices =
        {
            -0.5f, -0.5f, 0f,  0f, 0f,
             0.5f, -0.5f, 0f,  1f, 0f,
             0.5f,  0.5f, 0f,  1f, 1f,
            -0.5f,  0.5f, 0f,  0f, 1f,
        };

        uint[] indices = { 0, 1, 2, 2, 3, 0 };

        _vao = gl.GenVertexArray();
        _vbo = gl.GenBuffer();
        _ebo = gl.GenBuffer();

        gl.BindVertexArray(_vao);

        gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        gl.BufferData(BufferTargetARB.ArrayBuffer,
                      (nuint)(vertices.Length * sizeof(float)),
                      vertices.AsSpan(), BufferUsageARB.StaticDraw);

        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
        gl.BufferData(BufferTargetARB.ElementArrayBuffer,
                      (nuint)(indices.Length * sizeof(uint)),
                      indices.AsSpan(), BufferUsageARB.StaticDraw);

        uint stride = 5 * sizeof(float);
        gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, (void*)0);
        gl.EnableVertexAttribArray(0);

        gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, (void*)(3 * sizeof(float)));
        gl.EnableVertexAttribArray(1);

        gl.BindVertexArray(0);
    }

    public unsafe void Render(Matrix4X4<float> projection, Matrix4X4<float> skyView)
    {
        float rad = Angle * MathF.PI / 180f;
        float x   = MathF.Cos(rad) * 0.9f;
        float y   = MathF.Sin(rad) * 0.9f;
        float z   = -0.5f;  // Fester Z-Offset — immer vor der Kamera (z_view muss < 0 sein)

        var model = Matrix4X4.CreateScale<float>(Size) *
                    Matrix4X4.CreateTranslation(new Vector3D<float>(x, y, z));

        _shader.Use();
        _shader.SetMatrix4("model",      model);
        _shader.SetMatrix4("view",       skyView);
        _shader.SetMatrix4("projection", projection);
        _shader.SetVector3("uColor",     Color);
        _shader.SetFloat("uOpacity",     Opacity);
        _shader.SetInt("uTexture",       0);

        _gl.Enable(GLEnum.Blend);
        _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        _gl.BindVertexArray(_vao);
        _gl.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, (void*)0);

        _gl.Disable(GLEnum.Blend);
    }

    public void Dispose()
    {
        _gl.DeleteVertexArray(_vao);
        _gl.DeleteBuffer(_vbo);
        _gl.DeleteBuffer(_ebo);
        GC.SuppressFinalize(this);
    }
}
