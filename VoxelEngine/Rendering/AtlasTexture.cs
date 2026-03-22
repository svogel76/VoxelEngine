using Silk.NET.OpenGL;

namespace VoxelEngine.Rendering;

public class AtlasTexture : IDisposable
{
    public const int   TileSize  = 16;
    public const int   AtlasSize = 4;
    public const float TileUV    = 1.0f / AtlasSize;  // 0.25

    private readonly GL   _gl;
    private readonly uint _handle;

    public AtlasTexture(GL gl)
    {
        _gl = gl;

        _handle = gl.GenTexture();
        gl.BindTexture(TextureTarget.Texture2D, _handle);

        // Nearest-Filtering — Tile-Grenzen dürfen nicht verwischen
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.NearestMipmapNearest);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS,     (int)TextureWrapMode.ClampToEdge);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT,     (int)TextureWrapMode.ClampToEdge);

        byte[] pixels = GenerateAtlas();

        unsafe
        {
            fixed (byte* ptr = pixels)
                gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba,
                              (uint)(TileSize * AtlasSize), (uint)(TileSize * AtlasSize),
                              0, PixelFormat.Rgba, PixelType.UnsignedByte, ptr);
        }

        gl.GenerateMipmap(TextureTarget.Texture2D);
    }

    /// <summary>
    /// Gibt die UV-Koordinate der unteren-linken Ecke des Tiles zurück.
    /// </summary>
    public (float u, float v) GetTileUV(int tileIndex)
    {
        int col = tileIndex % AtlasSize;
        int row = tileIndex / AtlasSize;
        return (col * TileUV, row * TileUV);
    }

    public void Bind(TextureUnit unit)
    {
        _gl.ActiveTexture(unit);
        _gl.BindTexture(TextureTarget.Texture2D, _handle);
    }

    public void Dispose() => _gl.DeleteTexture(_handle);

    // ─── Atlas-Generierung ───────────────────────────────────────────────────

    private static byte[] GenerateAtlas()
    {
        int size   = TileSize * AtlasSize;  // 64
        var pixels = new byte[size * size * 4];
        var rng    = new Random(42);

        // Tile 0 (col=0, row=0): Gras Top
        FillNoise(pixels, size, 0, 0, 0x7E, 0xC8, 0x50, rng);

        // Tile 1 (col=1, row=0): Erde
        FillNoise(pixels, size, 1, 0, 0x8B, 0x69, 0x14, rng);

        // Tile 2 (col=2, row=0): Stein
        FillNoise(pixels, size, 2, 0, 0x88, 0x88, 0x88, rng);

        // Tile 3 (col=3, row=0): Gras Side — oben grün, unten braun
        FillGrassSide(pixels, size, 3, 0, rng);

        // Tile 4 (col=0, row=1): Sand
        FillNoise(pixels, size, 0, 1, 0xC8, 0xA8, 0x50, rng);

        // Tile 5 (col=1, row=1): Schnee Top
        FillNoise(pixels, size, 1, 1, 0xFF, 0xFF, 0xFF, rng);

        // Tile 6 (col=2, row=1): Holz Side — vertikale Streifen
        FillWoodSide(pixels, size, 2, 1, rng);

        // Tile 7 (col=3, row=1): Holz Top — Jahresringe
        FillWoodTop(pixels, size, 3, 1, rng);

        return pixels;
    }

    private static void FillNoise(byte[] pixels, int size,
                                   int tileCol, int tileRow,
                                   byte baseR, byte baseG, byte baseB,
                                   Random rng)
    {
        int startX = tileCol * TileSize;
        int startY = tileRow * TileSize;

        for (int py = 0; py < TileSize; py++)
        for (int px = 0; px < TileSize; px++)
        {
            int noise = rng.Next(-10, 11);
            SetPixel(pixels, size, startX + px, startY + py,
                     Clamp(baseR + noise),
                     Clamp(baseG + noise),
                     Clamp(baseB + noise));
        }
    }

    private static void FillGrassSide(byte[] pixels, int size,
                                       int tileCol, int tileRow,
                                       Random rng)
    {
        int startX = tileCol * TileSize;
        int startY = tileRow * TileSize;

        for (int py = 0; py < TileSize; py++)
        for (int px = 0; px < TileSize; px++)
        {
            int noise = rng.Next(-10, 11);
            // Obere 4 Zeilen grün (werden zur Top-Kante der Seitenfläche)
            bool isGreen = py < 4;
            byte r = isGreen ? (byte)0x7E : (byte)0x8B;
            byte g = isGreen ? (byte)0xC8 : (byte)0x69;
            byte b = isGreen ? (byte)0x50 : (byte)0x14;
            SetPixel(pixels, size, startX + px, startY + py,
                     Clamp(r + noise), Clamp(g + noise), Clamp(b + noise));
        }
    }

    private static void FillWoodSide(byte[] pixels, int size,
                                      int tileCol, int tileRow,
                                      Random rng)
    {
        int startX = tileCol * TileSize;
        int startY = tileRow * TileSize;

        for (int py = 0; py < TileSize; py++)
        for (int px = 0; px < TileSize; px++)
        {
            // Vertikale Streifen: heller/dunkler abwechselnd alle 3 Pixel
            bool light = (px % 3) != 0;
            int noise = rng.Next(-6, 7);
            byte r = light ? (byte)0x8B : (byte)0x6B;
            byte g = light ? (byte)0x5E : (byte)0x44;
            byte b = light ? (byte)0x3C : (byte)0x23;
            SetPixel(pixels, size, startX + px, startY + py,
                     Clamp(r + noise), Clamp(g + noise), Clamp(b + noise));
        }
    }

    private static void FillWoodTop(byte[] pixels, int size,
                                     int tileCol, int tileRow,
                                     Random rng)
    {
        int startX = tileCol * TileSize;
        int startY = tileRow * TileSize;
        float cx   = TileSize / 2.0f - 0.5f;
        float cy   = TileSize / 2.0f - 0.5f;

        for (int py = 0; py < TileSize; py++)
        for (int px = 0; px < TileSize; px++)
        {
            float dist  = MathF.Sqrt((px - cx) * (px - cx) + (py - cy) * (py - cy));
            bool  light = ((int)dist % 2) == 0;
            int   noise = rng.Next(-6, 7);
            byte  r     = light ? (byte)0x8B : (byte)0x6B;
            byte  g     = light ? (byte)0x5E : (byte)0x44;
            byte  b     = light ? (byte)0x3C : (byte)0x23;
            SetPixel(pixels, size, startX + px, startY + py,
                     Clamp(r + noise), Clamp(g + noise), Clamp(b + noise));
        }
    }

    private static void SetPixel(byte[] pixels, int size, int x, int y,
                                  byte r, byte g, byte b)
    {
        int idx = (y * size + x) * 4;
        pixels[idx]     = r;
        pixels[idx + 1] = g;
        pixels[idx + 2] = b;
        pixels[idx + 3] = 255;
    }

    private static byte Clamp(int value)
        => (byte)Math.Clamp(value, 0, 255);
}
