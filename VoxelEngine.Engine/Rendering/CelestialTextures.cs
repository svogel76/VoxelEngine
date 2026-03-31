using Silk.NET.OpenGL;

namespace VoxelEngine.Rendering;

public static class CelestialTextures
{
    private const int Size   = 32;
    private const float Cx   = 15.5f;  // pixel center X
    private const float Cy   = 15.5f;  // pixel center Y
    private const float Radius = 14f;

    public static Texture CreateSunTexture(GL gl)
    {
        byte[] data = new byte[Size * Size * 4];

        for (int py = 0; py < Size; py++)
        {
            for (int px = 0; px < Size; px++)
            {
                float dx = (px + 0.5f) - Cx;
                float dy = (py + 0.5f) - Cy;
                float r  = MathF.Sqrt(dx * dx + dy * dy);

                int idx = (py * Size + px) * 4;

                if (r >= Radius)
                {
                    // Transparent outside
                    data[idx + 0] = 0;
                    data[idx + 1] = 0;
                    data[idx + 2] = 0;
                    data[idx + 3] = 0;
                }
                else
                {
                    byte red, green, blue, alpha;

                    if (r < 8f)
                    {
                        // Core: #FFF4AA
                        red   = 255;
                        green = 244;
                        blue  = 170;
                    }
                    else
                    {
                        // Gradient from #FFF4AA to #FF8C00
                        float t = (r - 8f) / (Radius - 8f);
                        red   = 255;
                        green = (byte)(244 - (int)(104 * t));
                        blue  = (byte)(170 - (int)(170 * t));
                    }

                    // Soft edge: fade alpha in outer ring (r > 12)
                    if (r > 12f)
                        alpha = (byte)Math.Clamp((int)(255f * (1f - (r - 12f) / 2f)), 0, 255);
                    else
                        alpha = 255;

                    data[idx + 0] = red;
                    data[idx + 1] = green;
                    data[idx + 2] = blue;
                    data[idx + 3] = alpha;
                }
            }
        }

        return Texture.CreateFromBytes(gl, data, Size, Size);
    }

    public static Texture CreateMoonTexture(GL gl, int phase)
    {
        byte[] data = new byte[Size * Size * 4];
        var rng = new System.Random(phase * 42);

        for (int py = 0; py < Size; py++)
        {
            for (int px = 0; px < Size; px++)
            {
                float dx = (px + 0.5f) - Cx;
                float dy = (py + 0.5f) - Cy;
                float r  = MathF.Sqrt(dx * dx + dy * dy);

                int idx = (py * Size + px) * 4;

                if (r >= Radius)
                {
                    data[idx + 0] = 0;
                    data[idx + 1] = 0;
                    data[idx + 2] = 0;
                    data[idx + 3] = 0;
                    continue;
                }

                // Normalized coordinates in [-1, 1]
                float nx = dx / Radius;
                float ny = dy / Radius;

                if (phase == 0)
                {
                    // Neumond: kaum sichtbar
                    data[idx + 0] = 30;
                    data[idx + 1] = 30;
                    data[idx + 2] = 40;
                    data[idx + 3] = 20;
                    continue;
                }

                bool lit = IsLit(nx, ny, phase);

                if (!lit)
                {
                    // Dunkle Seite: sehr schwach sichtbar
                    data[idx + 0] = 10;
                    data[idx + 1] = 10;
                    data[idx + 2] = 20;
                    data[idx + 3] = 60;
                    continue;
                }

                // Beleuchtete Seite — Helligkeit skaliert mit Phase (Vollmond = am hellsten)
                float brightness = phase <= 4
                    ? (float)phase / 4f
                    : (float)(8 - phase) / 4f;

                // Leichter Grau-Noise für Mondkrater-Optik
                int noise = rng.Next(-20, 21);
                int baseValue  = (int)(220f * brightness);
                byte val  = (byte)Math.Clamp(baseValue + noise, 0, 255);

                // Rand des Mondes weich ausblenden
                float edgeFade = r > Radius - 2f
                    ? Math.Clamp((Radius - r) / 2f, 0f, 1f)
                    : 1f;
                byte alpha = (byte)(255 * edgeFade);

                data[idx + 0] = val;
                data[idx + 1] = val;
                data[idx + 2] = (byte)Math.Clamp(val + 10, 0, 255); // leicht bläulich
                data[idx + 3] = alpha;
            }
        }

        return Texture.CreateFromBytes(gl, data, Size, Size);
    }

    // Bestimmt ob ein normalisierter Punkt (nx, ny) auf der beleuchteten Seite liegt
    private static bool IsLit(float nx, float ny, int phase)
    {
        float disc = MathF.Sqrt(MathF.Max(0f, 1f - ny * ny));

        if (phase < 4)
        {
            // Zunehmend: rechte Seite beleuchtet
            // terminator_factor: phase 0 → 1 (rechter Rand), phase 4 → -1 (linker Rand)
            float factor = MathF.Cos(MathF.PI * phase / 4f);
            return nx > factor * disc;
        }
        else
        {
            // Abnehmend: linke Seite beleuchtet
            int   pEquiv = 8 - phase;
            float factor = MathF.Cos(MathF.PI * pEquiv / 4f);
            return nx < -factor * disc;
        }
    }
}
