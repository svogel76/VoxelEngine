using System.Numerics;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace VoxelEngine.Rendering;

public class StarField : IDisposable
{
    private readonly GL     _gl;
    private readonly Shader _shader;
    private readonly uint   _vao;
    private readonly uint   _quadVbo;
    private readonly uint   _instanceVbo;
    private readonly uint   _ebo;
    private readonly int    _starCount;

    private const float SkyRadius = 0.85f;

    public unsafe StarField(GL gl, int starCount = 1500, int seed = 42)
    {
        _gl        = gl;
        _starCount = starCount;

        var rng = new System.Random(seed);

        // Pro Stern: Position (3 floats) + Size (1 float) + Phase (1 float) = 5 floats
        float[] instanceData = new float[starCount * 5];
        for (int i = 0; i < starCount; i++)
        {
            float theta = rng.NextSingle() * 2 * MathF.PI;
            float phi   = MathF.Acos(2 * rng.NextSingle() - 1);
            float x = MathF.Sin(phi) * MathF.Cos(theta) * SkyRadius;
            float y = MathF.Abs(MathF.Sin(phi) * MathF.Sin(theta)) * SkyRadius; // nur obere Hälfte
            float z = MathF.Cos(phi) * SkyRadius;
            float size  = 0.003f + rng.NextSingle() * 0.008f;
            float phase = rng.NextSingle() * MathF.PI * 2f;
            instanceData[i * 5 + 0] = x;
            instanceData[i * 5 + 1] = y;
            instanceData[i * 5 + 2] = z;
            instanceData[i * 5 + 3] = size;
            instanceData[i * 5 + 4] = phase;
        }

        float[] quadVertices = {
            -0.5f, -0.5f,
             0.5f, -0.5f,
             0.5f,  0.5f,
            -0.5f,  0.5f
        };
        uint[] quadIndices = { 0, 1, 2, 2, 3, 0 };

        _vao         = gl.GenVertexArray();
        _quadVbo     = gl.GenBuffer();
        _instanceVbo = gl.GenBuffer();
        _ebo         = gl.GenBuffer();

        gl.BindVertexArray(_vao);

        // Quad-Vertices — Location 0
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, _quadVbo);
        gl.BufferData(BufferTargetARB.ArrayBuffer,
                      (nuint)(quadVertices.Length * sizeof(float)),
                      quadVertices.AsSpan(), BufferUsageARB.StaticDraw);
        gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), (void*)0);
        gl.EnableVertexAttribArray(0);

        // EBO
        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
        gl.BufferData(BufferTargetARB.ElementArrayBuffer,
                      (nuint)(quadIndices.Length * sizeof(uint)),
                      quadIndices.AsSpan(), BufferUsageARB.StaticDraw);

        // Instanz-Daten — Locations 1-3
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, _instanceVbo);
        gl.BufferData(BufferTargetARB.ArrayBuffer,
                      (nuint)(instanceData.Length * sizeof(float)),
                      instanceData.AsSpan(), BufferUsageARB.StaticDraw);

        // Location 1: StarPos (3 floats, Offset 0)
        gl.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), (void*)0);
        gl.EnableVertexAttribArray(1);
        gl.VertexAttribDivisor(1, 1);

        // Location 2: StarSize (1 float, Offset 12)
        gl.VertexAttribPointer(2, 1, VertexAttribPointerType.Float, false, 5 * sizeof(float), (void*)(3 * sizeof(float)));
        gl.EnableVertexAttribArray(2);
        gl.VertexAttribDivisor(2, 1);

        // Location 3: StarPhase (1 float, Offset 16)
        gl.VertexAttribPointer(3, 1, VertexAttribPointerType.Float, false, 5 * sizeof(float), (void*)(4 * sizeof(float)));
        gl.EnableVertexAttribArray(3);
        gl.VertexAttribDivisor(3, 1);

        gl.BindVertexArray(0);

        _shader = new Shader(gl, "Assets/Shaders/stars.vert", "Assets/Shaders/stars.frag");
    }

    public unsafe void Render(Matrix4X4<float> projection, Matrix4X4<float> skyView, float opacity, float time)
    {
        if (opacity <= 0.01f)
            return;

        _gl.Disable(GLEnum.DepthTest);
        _gl.DepthMask(false);
        _gl.Enable(GLEnum.Blend);
        _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        _shader.Use();
        _shader.SetMatrix4("view",       skyView);
        _shader.SetMatrix4("projection", projection);
        _shader.SetFloat("uTime",        time);
        _shader.SetFloat("uOpacity",     opacity);
        _shader.SetVector3("uStarColor", new Vector3(1.0f, 1.0f, 0.95f));

        _gl.BindVertexArray(_vao);
        _gl.DrawElementsInstanced(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, (void*)0, (uint)_starCount);

        _gl.Disable(GLEnum.Blend);
        _gl.DepthMask(true);
        _gl.Enable(GLEnum.DepthTest);
    }

    public void Dispose()
    {
        _shader.Dispose();
        _gl.DeleteVertexArray(_vao);
        _gl.DeleteBuffer(_quadVbo);
        _gl.DeleteBuffer(_instanceVbo);
        _gl.DeleteBuffer(_ebo);
        GC.SuppressFinalize(this);
    }
}
