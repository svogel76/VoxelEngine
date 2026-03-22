using System.Numerics;
using Silk.NET.OpenGL;
using VoxelEngine.World;

namespace VoxelEngine.Rendering;

public class Skybox : IDisposable
{
    private readonly GL             _gl;
    private readonly Shader         _shader;
    private readonly uint           _vao;
    private readonly uint           _vbo;
    private readonly uint           _ebo;
    private readonly SkyColorCurve  _colorCurve = new();

    public Vector3 ZenithColor      = new(0.1f, 0.4f, 0.8f);
    public Vector3 HorizonColor     = new(0.6f, 0.8f, 1.0f);
    public Vector3 GroundColor      = new(0.3f, 0.25f, 0.2f);
    public float   HorizonSharpness = 0.4f;

    public unsafe Skybox(GL gl)
    {
        _gl     = gl;
        _shader = new Shader(gl, "Assets/Shaders/skybox.vert", "Assets/Shaders/skybox.frag");

        float[] vertices =
        {
            -1, -1, -1,   1, -1, -1,   1,  1, -1,  -1,  1, -1,
            -1, -1,  1,   1, -1,  1,   1,  1,  1,  -1,  1,  1
        };

        uint[] indices =
        {
            0,1,2, 2,3,0,   // Rückseite
            4,5,6, 6,7,4,   // Vorderseite
            0,4,7, 7,3,0,   // Linke Seite
            1,5,6, 6,2,1,   // Rechte Seite
            3,2,6, 6,7,3,   // Oben
            0,1,5, 5,4,0    // Unten
        };

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

        uint stride = 3 * sizeof(float);
        gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, (void*)0);
        gl.EnableVertexAttribArray(0);

        gl.BindVertexArray(0);
    }

    public void UpdateColors(WorldTime time)
    {
        var frame    = _colorCurve.Evaluate(time.Time);
        ZenithColor  = frame.Zenith;
        HorizonColor = frame.Horizon;
        GroundColor  = frame.Ground;
    }

    public unsafe void Render(Camera camera, WorldTime time)
    {
        UpdateColors(time);
        _gl.Disable(GLEnum.DepthTest);
        _gl.DepthMask(false);
        _gl.Disable(GLEnum.CullFace);

        _shader.Use();

        // View-Matrix ohne Translation — Kamera bleibt immer im Würfel-Zentrum
        var skyView = camera.ViewMatrix;
        skyView.M41 = 0;
        skyView.M42 = 0;
        skyView.M43 = 0;

        _shader.SetMatrix4("view",       skyView);
        _shader.SetMatrix4("projection", camera.ProjectionMatrix);
        _shader.SetVector3("uZenithColor",  ZenithColor);
        _shader.SetVector3("uHorizonColor", HorizonColor);
        _shader.SetVector3("uGroundColor",  GroundColor);
        _shader.SetFloat("uHorizonSharpness", HorizonSharpness);

        _gl.BindVertexArray(_vao);
        _gl.DrawElements(PrimitiveType.Triangles, 36, DrawElementsType.UnsignedInt, (void*)0);

        _gl.DepthMask(true);
        _gl.Enable(GLEnum.CullFace);
    }

    public void Dispose()
    {
        _shader.Dispose();
        _gl.DeleteVertexArray(_vao);
        _gl.DeleteBuffer(_vbo);
        _gl.DeleteBuffer(_ebo);
        GC.SuppressFinalize(this);
    }
}
