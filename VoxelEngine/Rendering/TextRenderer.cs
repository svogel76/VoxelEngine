using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace VoxelEngine.Rendering;

public class TextRenderer : IDisposable
{
    private readonly GL         _gl;
    private readonly BitmapFont _font;
    private readonly Shader     _shader;

    private readonly uint _vao;
    private readonly uint _vbo;

    // 1×1 weißes Texel für einfarbige Quads
    private readonly uint _whiteTexture;

    public TextRenderer(GL gl, BitmapFont font, int windowWidth, int windowHeight)
    {
        _gl   = gl;
        _font = font;

        _shader = new Shader(gl, "Assets/Shaders/text.vert", "Assets/Shaders/text.frag");

        _vao = gl.GenVertexArray();
        _vbo = gl.GenBuffer();

        gl.BindVertexArray(_vao);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);

        // DynamicDraw — Inhalt ändert sich jeden Frame
        gl.BufferData(BufferTargetARB.ArrayBuffer, 0, Span<float>.Empty, BufferUsageARB.DynamicDraw);

        uint stride = 4 * sizeof(float);
        unsafe
        {
            // Location 0: Position (2 floats)
            gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, stride, (void*)0);
            gl.EnableVertexAttribArray(0);
            // Location 1: TexCoord (2 floats, offset 2 floats)
            gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, (void*)(2 * sizeof(float)));
            gl.EnableVertexAttribArray(1);
        }

        gl.BindVertexArray(0);

        _whiteTexture = CreateWhiteTexture();
    }

    private unsafe uint CreateWhiteTexture()
    {
        uint handle = _gl.GenTexture();
        _gl.BindTexture(TextureTarget.Texture2D, handle);
        byte[] white = [255, 255, 255, 255];
        fixed (byte* ptr = white)
            _gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba,
                           1, 1, 0, PixelFormat.Rgba, PixelType.UnsignedByte, ptr);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        return handle;
    }

    public void BeginFrame(int windowWidth, int windowHeight)
    {
        _gl.Disable(GLEnum.DepthTest);
        _gl.Disable(GLEnum.CullFace);
        _gl.Enable(GLEnum.Blend);
        _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        var projection = Matrix4X4.CreateOrthographicOffCenter(
            0f, windowWidth, windowHeight, 0f, -1f, 1f);

        _shader.Use();
        _shader.SetMatrix4("projection", projection);
    }

    public void EndFrame()
    {
        _gl.Enable(GLEnum.DepthTest);
        _gl.Enable(GLEnum.CullFace);
        _gl.Disable(GLEnum.Blend);
    }

    public void DrawText(string text, float x, float y,
                         float r = 1f, float g = 1f, float b = 1f, float a = 1f,
                         float charWidth = 12f, float charHeight = 16f)
    {
        if (string.IsNullOrEmpty(text)) return;

        // 6 Vertices pro Zeichen, 4 Floats pro Vertex
        var vertices = new float[text.Length * 6 * 4];
        int idx = 0;
        float cursorX = x;

        foreach (char c in text)
        {
            var (u, v, uSize, vSize) = _font.GetCharUV(c);

            float x0 = cursorX;
            float x1 = cursorX + charWidth;
            float y0 = y;
            float y1 = y + charHeight;

            // Dreieck 1: oben-links, unten-links, unten-rechts
            AddVertex(vertices, ref idx, x0, y0, u,        v        );
            AddVertex(vertices, ref idx, x0, y1, u,        v + vSize);
            AddVertex(vertices, ref idx, x1, y1, u + uSize, v + vSize);
            // Dreieck 2: oben-links, unten-rechts, oben-rechts
            AddVertex(vertices, ref idx, x0, y0, u,         v        );
            AddVertex(vertices, ref idx, x1, y1, u + uSize, v + vSize);
            AddVertex(vertices, ref idx, x1, y0, u + uSize, v        );

            cursorX += charWidth;
        }

        _font.Bind(TextureUnit.Texture0);
        _shader.SetInt("uTexture", 0);
        _shader.SetVector4("uColor", r, g, b, a);

        Upload(vertices);
        _gl.DrawArrays(PrimitiveType.Triangles, 0, (uint)(text.Length * 6));
    }

    public void DrawQuad(float x, float y, float width, float height,
                         float r, float g, float b, float a)
    {
        var vertices = new float[6 * 4];
        int idx = 0;

        // Dreieck 1
        AddVertex(vertices, ref idx, x,         y,          0f, 0f);
        AddVertex(vertices, ref idx, x,         y + height, 0f, 1f);
        AddVertex(vertices, ref idx, x + width, y + height, 1f, 1f);
        // Dreieck 2
        AddVertex(vertices, ref idx, x,         y,          0f, 0f);
        AddVertex(vertices, ref idx, x + width, y + height, 1f, 1f);
        AddVertex(vertices, ref idx, x + width, y,          1f, 0f);

        _gl.ActiveTexture(TextureUnit.Texture0);
        _gl.BindTexture(TextureTarget.Texture2D, _whiteTexture);
        _shader.SetInt("uTexture", 0);
        _shader.SetVector4("uColor", r, g, b, a);

        Upload(vertices);
        _gl.DrawArrays(PrimitiveType.Triangles, 0, 6);
    }

    private void Upload(float[] vertices)
    {
        _gl.BindVertexArray(_vao);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        _gl.BufferData(BufferTargetARB.ArrayBuffer,
                       (nuint)(vertices.Length * sizeof(float)),
                       vertices.AsSpan(), BufferUsageARB.DynamicDraw);
    }

    private static void AddVertex(float[] buf, ref int idx, float x, float y, float u, float v)
    {
        buf[idx++] = x;
        buf[idx++] = y;
        buf[idx++] = u;
        buf[idx++] = v;
    }

    public void Dispose()
    {
        _shader.Dispose();
        _gl.DeleteVertexArray(_vao);
        _gl.DeleteBuffer(_vbo);
        _gl.DeleteTexture(_whiteTexture);
        GC.SuppressFinalize(this);
    }
}
