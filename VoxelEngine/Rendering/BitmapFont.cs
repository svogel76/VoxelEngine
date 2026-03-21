using Silk.NET.OpenGL;
using StbImageSharp;

namespace VoxelEngine.Rendering;

public class BitmapFont : IDisposable
{
    private readonly GL   _gl;
    private readonly uint _handle;
    private readonly int  _charsPerRow;

    public BitmapFont(GL gl, string texturePath, int charsPerRow = 16)
    {
        _gl          = gl;
        _charsPerRow = charsPerRow;

        _handle = gl.GenTexture();
        gl.BindTexture(TextureTarget.Texture2D, _handle);

        // Nearest filtering — kein Blur bei Pixel-Fonts
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS,     (int)TextureWrapMode.ClampToEdge);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT,     (int)TextureWrapMode.ClampToEdge);

        // Kein vertikales Flip — Zeile 0 des Bildes = GL v=0 (unten)
        // Das passt zur GetCharUV-Formel: v = row * vSize
        StbImage.stbi_set_flip_vertically_on_load(0);
        using var stream = File.OpenRead(texturePath);
        var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

        unsafe
        {
            fixed (byte* ptr = image.Data)
                gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba,
                              (uint)image.Width, (uint)image.Height, 0,
                              PixelFormat.Rgba, PixelType.UnsignedByte, ptr);
        }

        // Mipmaps nicht nötig für Nearest, aber kein Schaden
        gl.GenerateMipmap(TextureTarget.Texture2D);

        // Flip für alle folgenden Texture-Loads wieder aktivieren
        StbImage.stbi_set_flip_vertically_on_load(1);
    }

    /// <summary>
    /// Gibt UV-Koordinaten für ein Zeichen zurück.
    /// (u, v) = untere-linke Ecke des Zeichens im Texture-Atlas.
    /// </summary>
    public (float u, float v, float uSize, float vSize) GetCharUV(char c)
    {
        int ascii = (int)c;
        int col   = ascii % _charsPerRow;
        int row   = ascii / _charsPerRow;

        float uSize = 1.0f / _charsPerRow;
        float vSize = 1.0f / _charsPerRow;

        float u = col * uSize;
        float v = row * vSize;

        return (u, v, uSize, vSize);
    }

    public void Bind(TextureUnit unit)
    {
        _gl.ActiveTexture(unit);
        _gl.BindTexture(TextureTarget.Texture2D, _handle);
    }

    public void Dispose() => _gl.DeleteTexture(_handle);
}
