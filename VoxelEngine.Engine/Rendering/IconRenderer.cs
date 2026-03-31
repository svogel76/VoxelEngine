using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace VoxelEngine.Rendering;

/// <summary>
/// 2D-Icon-Renderer für Block-Icons aus der ArrayTexture.
///
/// Vertex-Layout: [x, y, u, v, layer] — 5 floats pro Vertex.
/// Alle Icons eines Frames werden in einem einzigen Draw-Call gebatcht.
///
/// Verwendung:
/// <code>
///   renderer.BeginFrame(screenW, screenH);
///   renderer.DrawIcon(x, y, size, layerIndex);
///   ...
///   renderer.EndFrame();
/// </code>
/// </summary>
public sealed class IconRenderer : IDisposable
{
    // Floats pro Vertex: pos(2) + uv(2) + layer(1)
    private const int FloatsPerVertex  = 5;
    private const int VerticesPerQuad  = 6;    // 2 Dreiecke
    private const int MaxIconsPerBatch = 512;

    private readonly GL     _gl;
    private readonly Shader _shader;
    private readonly uint   _vao;
    private readonly uint   _vbo;

    // CPU-Buffer für den aktuellen Batch
    private readonly float[] _buffer = new float[MaxIconsPerBatch * VerticesPerQuad * FloatsPerVertex];
    private int _vertexCount;

    // Aktuell gebundene ArrayTexture (pro Frame gesetzt)
    private ArrayTexture? _currentAtlas;

    public IconRenderer(GL gl)
    {
        _gl     = gl;
        _shader = new Shader(gl, "Assets/Shaders/icon.vert", "Assets/Shaders/icon.frag");

        _vao = gl.GenVertexArray();
        _vbo = gl.GenBuffer();

        gl.BindVertexArray(_vao);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        gl.BufferData(BufferTargetARB.ArrayBuffer,
                      (nuint)(_buffer.Length * sizeof(float)),
                      Span<float>.Empty,
                      BufferUsageARB.DynamicDraw);

        uint stride = (uint)(FloatsPerVertex * sizeof(float));
        unsafe
        {
            // location 0: aPosition (2 floats)
            gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, stride, (void*)0);
            gl.EnableVertexAttribArray(0);
            // location 1: aTexCoord (2 floats, offset 8 bytes)
            gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, (void*)(2 * sizeof(float)));
            gl.EnableVertexAttribArray(1);
            // location 2: aTileLayer (1 float, offset 16 bytes)
            gl.VertexAttribPointer(2, 1, VertexAttribPointerType.Float, false, stride, (void*)(4 * sizeof(float)));
            gl.EnableVertexAttribArray(2);
        }

        gl.BindVertexArray(0);
    }

    /// <summary>
    /// Setzt den Rendering-Zustand für den aktuellen Frame.
    /// Muss vor allen DrawIcon-Aufrufen aufgerufen werden.
    /// </summary>
    public void BeginFrame(int screenW, int screenH, ArrayTexture atlas)
    {
        _currentAtlas = atlas;
        _vertexCount  = 0;

        _gl.Disable(GLEnum.DepthTest);
        _gl.Disable(GLEnum.CullFace);
        _gl.Enable(GLEnum.Blend);
        _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        var projection = Matrix4X4.CreateOrthographicOffCenter(
            0f, screenW, screenH, 0f, -1f, 1f);

        _shader.Use();
        _shader.SetMatrix4("uProjection", projection);
        _shader.SetVector4("uColor", 1f, 1f, 1f, 1f);

        atlas.Bind(TextureUnit.Texture0);
        _shader.SetInt("uTexture", 0);
    }

    /// <summary>
    /// Zeichnet ein Block-Icon an Position (x, y) mit der angegebenen Größe.
    /// <paramref name="layer"/> ist der ArrayTexture-Layer-Index (TopTextureIndex der BlockDefinition).
    /// </summary>
    public void DrawIcon(float x, float y, float size, int layer)
    {
        if (_vertexCount + VerticesPerQuad > MaxIconsPerBatch * VerticesPerQuad)
            Flush();   // Batch-Limit erreicht — zwischenflush

        float x0 = x;
        float x1 = x + size;
        float y0 = y;
        float y1 = y + size;
        float l  = layer;

        // UV: [0,1]×[0,1] → volle Tile-Textur (Texture2DArray skaliert automatisch)
        AddVertex(x0, y0, 0f, 0f, l);
        AddVertex(x0, y1, 0f, 1f, l);
        AddVertex(x1, y1, 1f, 1f, l);

        AddVertex(x0, y0, 0f, 0f, l);
        AddVertex(x1, y1, 1f, 1f, l);
        AddVertex(x1, y0, 1f, 0f, l);
    }

    /// <summary>Schreibt alle gepufferten Icons auf die GPU und zeichnet sie.</summary>
    public void EndFrame()
    {
        if (_vertexCount > 0)
            Flush();

        _gl.Enable(GLEnum.DepthTest);
        _gl.Enable(GLEnum.CullFace);
        _gl.Disable(GLEnum.Blend);

        _currentAtlas = null;
    }

    // ── intern ───────────────────────────────────────────────────────────────

    private void Flush()
    {
        if (_vertexCount == 0)
            return;

        _gl.BindVertexArray(_vao);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        _gl.BufferData(BufferTargetARB.ArrayBuffer,
                       (nuint)(_vertexCount * FloatsPerVertex * sizeof(float)),
                       _buffer.AsSpan(0, _vertexCount * FloatsPerVertex),
                       BufferUsageARB.DynamicDraw);

        _gl.DrawArrays(PrimitiveType.Triangles, 0, (uint)_vertexCount);
        _vertexCount = 0;
    }

    private void AddVertex(float x, float y, float u, float v, float layer)
    {
        int base_ = _vertexCount * FloatsPerVertex;
        _buffer[base_]     = x;
        _buffer[base_ + 1] = y;
        _buffer[base_ + 2] = u;
        _buffer[base_ + 3] = v;
        _buffer[base_ + 4] = layer;
        _vertexCount++;
    }

    public void Dispose()
    {
        _shader.Dispose();
        _gl.DeleteVertexArray(_vao);
        _gl.DeleteBuffer(_vbo);
        GC.SuppressFinalize(this);
    }
}
