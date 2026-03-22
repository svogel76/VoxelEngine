using Silk.NET.OpenGL;

namespace VoxelEngine.Rendering;

public class ArrayTexture : IDisposable
{
    public const int TileSize  = 16;
    public const int TileCount = 11;

    private readonly GL   _gl;
    private readonly uint _handle;

    public unsafe ArrayTexture(GL gl)
    {
        _gl = gl;

        _handle = gl.GenTexture();
        gl.BindTexture(GLEnum.Texture2DArray, _handle);

        // Allocate all layers at once
        gl.TexImage3D(GLEnum.Texture2DArray, 0, InternalFormat.Rgba8,
                      TileSize, TileSize, TileCount, 0,
                      PixelFormat.Rgba, PixelType.UnsignedByte, null);

        // Nearest-filtering — no blurring between tiles
        gl.TexParameter(GLEnum.Texture2DArray, GLEnum.TextureMinFilter, (int)GLEnum.Nearest);
        gl.TexParameter(GLEnum.Texture2DArray, GLEnum.TextureMagFilter, (int)GLEnum.Nearest);
        gl.TexParameter(GLEnum.Texture2DArray, GLEnum.TextureWrapS,     (int)GLEnum.Repeat);
        gl.TexParameter(GLEnum.Texture2DArray, GLEnum.TextureWrapT,     (int)GLEnum.Repeat);

        var rng = new Random(42);

        UploadLayer(gl, 0, GenerateNoise(0x7E, 0xC8, 0x50, rng));  // Gras Top
        UploadLayer(gl, 1, GenerateNoise(0x8B, 0x69, 0x14, rng));  // Erde
        UploadLayer(gl, 2, GenerateNoise(0x88, 0x88, 0x88, rng));  // Stein
        UploadLayer(gl, 3, GenerateGrassSide(rng));                 // Gras Side
        UploadLayer(gl, 4, GenerateNoise(0xC8, 0xA8, 0x50, rng));  // Sand
        UploadLayer(gl, 5, GenerateNoise(0xFF, 0xFF, 0xFF, rng));  // Schnee Top
        UploadLayer(gl, 6, GenerateWoodSide(rng));                  // Holz Side
        UploadLayer(gl, 7, GenerateWoodTop(rng));                   // Holz Top
        UploadLayer(gl, 8, GenerateWater(rng));                     // Wasser
        UploadLayer(gl, 9, GenerateGlass(rng));                     // Glas
        UploadLayer(gl, 10, GenerateIce(rng));                      // Eis
    }

    public void Bind(TextureUnit unit)
    {
        _gl.ActiveTexture(unit);
        _gl.BindTexture(GLEnum.Texture2DArray, _handle);
    }

    public void Dispose() => _gl.DeleteTexture(_handle);

    // ─── Upload ──────────────────────────────────────────────────────────────

    private static unsafe void UploadLayer(GL gl, int layer, byte[] pixels)
    {
        fixed (byte* ptr = pixels)
            gl.TexSubImage3D(GLEnum.Texture2DArray, 0,
                             0, 0, layer,
                             TileSize, TileSize, 1,
                             PixelFormat.Rgba, PixelType.UnsignedByte, ptr);
    }

    // ─── Tile generation ─────────────────────────────────────────────────────

    private static byte[] GenerateNoise(byte baseR, byte baseG, byte baseB, Random rng)
    {
        var pixels = new byte[TileSize * TileSize * 4];
        for (int i = 0; i < TileSize * TileSize; i++)
        {
            int noise = rng.Next(-10, 11);
            pixels[i * 4]     = Clamp(baseR + noise);
            pixels[i * 4 + 1] = Clamp(baseG + noise);
            pixels[i * 4 + 2] = Clamp(baseB + noise);
            pixels[i * 4 + 3] = 255;
        }
        return pixels;
    }

    private static byte[] GenerateGrassSide(Random rng)
    {
        var pixels = new byte[TileSize * TileSize * 4];
        for (int py = 0; py < TileSize; py++)
        for (int px = 0; px < TileSize; px++)
        {
            int  noise   = rng.Next(-10, 11);
            bool isGreen = py >= TileSize - 4;  // grün an hohen py → hohe V → Oberkante der Fläche
            byte r = isGreen ? (byte)0x7E : (byte)0x8B;
            byte g = isGreen ? (byte)0xC8 : (byte)0x69;
            byte b = isGreen ? (byte)0x50 : (byte)0x14;
            int  idx = (py * TileSize + px) * 4;
            pixels[idx]     = Clamp(r + noise);
            pixels[idx + 1] = Clamp(g + noise);
            pixels[idx + 2] = Clamp(b + noise);
            pixels[idx + 3] = 255;
        }
        return pixels;
    }

    private static byte[] GenerateWoodSide(Random rng)
    {
        var pixels = new byte[TileSize * TileSize * 4];
        for (int py = 0; py < TileSize; py++)
        for (int px = 0; px < TileSize; px++)
        {
            bool light = (px % 3) != 0;
            int  noise = rng.Next(-6, 7);
            byte r = light ? (byte)0x8B : (byte)0x6B;
            byte g = light ? (byte)0x5E : (byte)0x44;
            byte b = light ? (byte)0x3C : (byte)0x23;
            int  idx = (py * TileSize + px) * 4;
            pixels[idx]     = Clamp(r + noise);
            pixels[idx + 1] = Clamp(g + noise);
            pixels[idx + 2] = Clamp(b + noise);
            pixels[idx + 3] = 255;
        }
        return pixels;
    }

    private static byte[] GenerateWoodTop(Random rng)
    {
        var pixels = new byte[TileSize * TileSize * 4];
        float cx = TileSize / 2.0f - 0.5f;
        float cy = TileSize / 2.0f - 0.5f;
        for (int py = 0; py < TileSize; py++)
        for (int px = 0; px < TileSize; px++)
        {
            float dist  = MathF.Sqrt((px - cx) * (px - cx) + (py - cy) * (py - cy));
            bool  light = ((int)dist % 2) == 0;
            int   noise = rng.Next(-6, 7);
            byte  r     = light ? (byte)0x8B : (byte)0x6B;
            byte  g     = light ? (byte)0x5E : (byte)0x44;
            byte  b     = light ? (byte)0x3C : (byte)0x23;
            int   idx   = (py * TileSize + px) * 4;
            pixels[idx]     = Clamp(r + noise);
            pixels[idx + 1] = Clamp(g + noise);
            pixels[idx + 2] = Clamp(b + noise);
            pixels[idx + 3] = 255;
        }
        return pixels;
    }

    // Wasser: tiefblau #1A5C8C, Alpha=180, leichtes Noise-Muster
    private static byte[] GenerateWater(Random rng)
    {
        var pixels = new byte[TileSize * TileSize * 4];
        for (int py = 0; py < TileSize; py++)
        for (int px = 0; px < TileSize; px++)
        {
            int  noise = rng.Next(-15, 16);
            int  idx   = (py * TileSize + px) * 4;
            pixels[idx]     = Clamp(0x1A + noise);
            pixels[idx + 1] = Clamp(0x5C + noise);
            pixels[idx + 2] = Clamp(0x8C + noise);
            pixels[idx + 3] = 180;
        }
        return pixels;
    }

    // Glas: helles Grau-Blau #E8F0F8, Alpha=80 — fast durchsichtig
    private static byte[] GenerateGlass(Random rng)
    {
        var pixels = new byte[TileSize * TileSize * 4];
        for (int py = 0; py < TileSize; py++)
        for (int px = 0; px < TileSize; px++)
        {
            // Leichter Rahmen-Effekt: Rand etwas dunkler
            bool isEdge = px == 0 || px == TileSize - 1 || py == 0 || py == TileSize - 1;
            int  noise  = rng.Next(-5, 6);
            byte base_  = isEdge ? (byte)0xC0 : (byte)0xE8;
            int  idx    = (py * TileSize + px) * 4;
            pixels[idx]     = Clamp(base_ + noise);
            pixels[idx + 1] = Clamp(0xF0 + noise);
            pixels[idx + 2] = Clamp(0xF8 + noise);
            pixels[idx + 3] = isEdge ? (byte)160 : (byte)80;
        }
        return pixels;
    }

    // Eis: hellblau #A8D4F0 mit weißen Einschlüssen, Alpha=160
    private static byte[] GenerateIce(Random rng)
    {
        var pixels = new byte[TileSize * TileSize * 4];
        for (int py = 0; py < TileSize; py++)
        for (int px = 0; px < TileSize; px++)
        {
            int  noise    = rng.Next(-12, 13);
            bool isCrack  = rng.Next(0, 10) == 0;  // gelegentliche weiße Einschlüsse
            int  idx      = (py * TileSize + px) * 4;
            pixels[idx]     = isCrack ? (byte)255 : Clamp(0xA8 + noise);
            pixels[idx + 1] = isCrack ? (byte)255 : Clamp(0xD4 + noise);
            pixels[idx + 2] = isCrack ? (byte)255 : Clamp(0xF0 + noise);
            pixels[idx + 3] = 160;
        }
        return pixels;
    }

    private static byte Clamp(int v) => (byte)Math.Clamp(v, 0, 255);
}
