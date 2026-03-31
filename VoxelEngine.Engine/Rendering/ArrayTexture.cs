using Silk.NET.OpenGL;
using VoxelEngine.World;

namespace VoxelEngine.Rendering;

public class ArrayTexture : IDisposable
{
    public const int TileSize  = 16;

    private readonly GL   _gl;
    private readonly uint _handle;
    private readonly int _tileCount;

    public unsafe ArrayTexture(GL gl)
    {
        _gl = gl;
        _tileCount = BlockRegistry.GetRequiredTextureLayerCount();

        _handle = gl.GenTexture();
        gl.BindTexture(GLEnum.Texture2DArray, _handle);

        // Allocate all layers at once
        gl.TexImage3D(GLEnum.Texture2DArray, 0, (int)InternalFormat.Rgba8,
                      (uint)TileSize, (uint)TileSize, (uint)_tileCount, 0,
                      GLEnum.Rgba, GLEnum.UnsignedByte, null);

        // Nearest-filtering — no blurring between tiles
        gl.TexParameter(GLEnum.Texture2DArray, GLEnum.TextureMinFilter, (int)GLEnum.Nearest);
        gl.TexParameter(GLEnum.Texture2DArray, GLEnum.TextureMagFilter, (int)GLEnum.Nearest);
        gl.TexParameter(GLEnum.Texture2DArray, GLEnum.TextureWrapS,     (int)GLEnum.Repeat);
        gl.TexParameter(GLEnum.Texture2DArray, GLEnum.TextureWrapT,     (int)GLEnum.Repeat);

        var rng = new Random(42);
        var layerFactories = CreateLayerFactories();

        foreach (int layer in BlockRegistry.GetUsedTextureLayers())
        {
            if (!layerFactories.TryGetValue(layer, out var factory))
                factory = static random => GenerateNoise(0x88, 0x88, 0x88, random);

            UploadLayer(gl, layer, factory(rng));
        }
    }

    public void Bind(TextureUnit unit)
    {
        _gl.ActiveTexture(unit);
        _gl.BindTexture(GLEnum.Texture2DArray, _handle);
    }

    public void Dispose() => _gl.DeleteTexture(_handle);

    private static Dictionary<int, Func<Random, byte[]>> CreateLayerFactories() => new()
    {
        [0] = static rng => GenerateNoise(0x7E, 0xC8, 0x50, rng),
        [1] = static rng => GenerateNoise(0x8B, 0x69, 0x14, rng),
        [2] = static rng => GenerateNoise(0x88, 0x88, 0x88, rng),
        [3] = static rng => GenerateGrassSide(rng),
        [4] = static rng => GenerateNoise(0xC8, 0xA8, 0x50, rng),
        [5] = static rng => GenerateSnow(rng),
        [6] = static rng => GenerateWoodSide(rng),
        [7] = static rng => GenerateWoodTop(rng),
        [8] = static rng => GenerateWater(rng),
        [9] = static rng => GenerateGlass(rng),
        [10] = static rng => GenerateIce(rng),
        [11] = static rng => GenerateDryGrassTop(rng),
        [12] = static rng => GenerateDryGrassSide(rng),
        [13] = static rng => GenerateTreeWood(rng),
        [14] = static rng => GenerateLeaves(rng),
    };

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

    private static byte[] GenerateDryGrassTop(Random rng)
        => GenerateNoise(0xB8, 0xBF, 0x52, rng);

    private static byte[] GenerateDryGrassSide(Random rng)
    {
        var pixels = new byte[TileSize * TileSize * 4];
        for (int py = 0; py < TileSize; py++)
        for (int px = 0; px < TileSize; px++)
        {
            int noise = rng.Next(-10, 11);
            bool isTopStrip = py >= TileSize - 4;
            byte r = isTopStrip ? (byte)0xB8 : (byte)0x8B;
            byte g = isTopStrip ? (byte)0xBF : (byte)0x69;
            byte b = isTopStrip ? (byte)0x52 : (byte)0x14;
            int idx = (py * TileSize + px) * 4;
            pixels[idx] = Clamp(r + noise);
            pixels[idx + 1] = Clamp(g + noise);
            pixels[idx + 2] = Clamp(b + noise);
            pixels[idx + 3] = 255;
        }
        return pixels;
    }

    private static byte[] GenerateSnow(Random rng)
        => GenerateNoise(0xF2, 0xF6, 0xFF, rng);

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

    private static byte[] GenerateTreeWood(Random rng)
        => GenerateNoise(0x7A, 0x54, 0x2F, rng);

    private static byte[] GenerateLeaves(Random rng)
    {
        var pixels = new byte[TileSize * TileSize * 4];
        for (int py = 0; py < TileSize; py++)
        for (int px = 0; px < TileSize; px++)
        {
            bool isVein = px == TileSize / 2 || py == TileSize / 2 || (px + py) % 7 == 0;
            int noise = rng.Next(-12, 13);
            int idx = (py * TileSize + px) * 4;
            byte baseR = isVein ? (byte)0x4D : (byte)0x5E;
            byte baseG = isVein ? (byte)0x98 : (byte)0xB2;
            byte baseB = isVein ? (byte)0x38 : (byte)0x4B;
            pixels[idx] = Clamp(baseR + noise);
            pixels[idx + 1] = Clamp(baseG + noise);
            pixels[idx + 2] = Clamp(baseB + noise);
            pixels[idx + 3] = (byte)(isVein ? 240 : 236);
        }
        return pixels;
    }

    private static byte Clamp(int v) => (byte)Math.Clamp(v, 0, 255);
}
