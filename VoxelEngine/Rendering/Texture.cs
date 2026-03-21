using Silk.NET.OpenGL;
using StbImageSharp;

namespace VoxelEngine.Rendering;

public class Texture : IDisposable
{
    private readonly GL   _gl;
    private readonly uint _handle;

    public Texture(GL gl, string path)
    {
        _gl     = gl;
        _handle = gl.GenTexture();
        gl.BindTexture(TextureTarget.Texture2D, _handle);
        SetParameters();

        StbImage.stbi_set_flip_vertically_on_load(1);
        using var stream = File.OpenRead(path);
        var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

        Upload(image.Data, (uint)image.Width, (uint)image.Height);
        gl.GenerateMipmap(TextureTarget.Texture2D);
    }

    private Texture(GL gl)
    {
        _gl     = gl;
        _handle = gl.GenTexture();
        gl.BindTexture(TextureTarget.Texture2D, _handle);
        SetParameters();
    }

    public static Texture CreateFromBytes(GL gl, byte[] data, uint width, uint height)
    {
        var tex = new Texture(gl);
        tex.Upload(data, width, height);
        gl.GenerateMipmap(TextureTarget.Texture2D);
        return tex;
    }

    private void SetParameters()
    {
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS,     (int)TextureWrapMode.Repeat);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT,     (int)TextureWrapMode.Repeat);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
    }

    private unsafe void Upload(byte[] data, uint width, uint height)
    {
        fixed (byte* ptr = data)
            _gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba,
                           width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, ptr);
    }

    public void Bind(TextureUnit unit)
    {
        _gl.ActiveTexture(unit);
        _gl.BindTexture(TextureTarget.Texture2D, _handle);
    }

    public void Dispose() => _gl.DeleteTexture(_handle);
}
