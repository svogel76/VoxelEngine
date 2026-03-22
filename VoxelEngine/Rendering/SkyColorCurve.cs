using System.Numerics;

namespace VoxelEngine.Rendering;

public struct SkyKeyframe
{
    public float   Time;
    public Vector3 Zenith;
    public Vector3 Horizon;
    public Vector3 Ground;
    public float   AmbientLight;
}

public class SkyColorCurve
{
    private static Vector3 Hex(int r, int g, int b)
        => new(r / 255f, g / 255f, b / 255f);

    private readonly SkyKeyframe[] _keyframes =
    {
        new() { Time =  0.0f, Zenith = Hex(0x0A,0x0A,0x2A), Horizon = Hex(0x0D,0x0D,0x3A), Ground = Hex(0x05,0x05,0x10), AmbientLight = 0.05f },
        new() { Time =  5.5f, Zenith = Hex(0xFF,0x6B,0x35), Horizon = Hex(0xFF,0xB3,0x47), Ground = Hex(0x3A,0x20,0x10), AmbientLight = 0.15f },
        new() { Time =  7.0f, Zenith = Hex(0x4A,0x90,0xD9), Horizon = Hex(0x87,0xCE,0xEB), Ground = Hex(0x3A,0x50,0x20), AmbientLight = 0.70f },
        new() { Time = 12.0f, Zenith = Hex(0x1A,0x6B,0xA0), Horizon = Hex(0xC8,0xE8,0xF8), Ground = Hex(0x3A,0x55,0x20), AmbientLight = 1.00f },
        new() { Time = 17.0f, Zenith = Hex(0x4A,0x90,0xD9), Horizon = Hex(0x87,0xCE,0xEB), Ground = Hex(0x3A,0x50,0x20), AmbientLight = 0.70f },
        new() { Time = 18.5f, Zenith = Hex(0xFF,0x45,0x00), Horizon = Hex(0xFF,0x8C,0x00), Ground = Hex(0x3A,0x20,0x10), AmbientLight = 0.15f },
        new() { Time = 20.0f, Zenith = Hex(0x0A,0x0A,0x2A), Horizon = Hex(0x0D,0x0D,0x3A), Ground = Hex(0x05,0x05,0x10), AmbientLight = 0.05f },
        new() { Time = 24.0f, Zenith = Hex(0x0A,0x0A,0x2A), Horizon = Hex(0x0D,0x0D,0x3A), Ground = Hex(0x05,0x05,0x10), AmbientLight = 0.05f },
    };

    public SkyKeyframe Evaluate(double worldTime)
    {
        float t = (float)worldTime;

        // Find surrounding keyframes
        int next = 1;
        while (next < _keyframes.Length - 1 && _keyframes[next].Time < t)
            next++;

        var before = _keyframes[next - 1];
        var after  = _keyframes[next];

        float span = after.Time - before.Time;
        float alpha = span > 0f ? (t - before.Time) / span : 0f;

        return new SkyKeyframe
        {
            Time         = t,
            Zenith       = Vector3.Lerp(before.Zenith,  after.Zenith,  alpha),
            Horizon      = Vector3.Lerp(before.Horizon, after.Horizon, alpha),
            Ground       = Vector3.Lerp(before.Ground,  after.Ground,  alpha),
            AmbientLight = before.AmbientLight + (after.AmbientLight - before.AmbientLight) * alpha,
        };
    }
}
