namespace VoxelEngine.World;

/// <summary>
/// Noise-Parameter für einen Biom-Typ.
/// Später pro Biom eine eigene Instanz.
/// </summary>
public class NoiseSettings
{
    public int Seed { get; init; } = 42;
    public float Frequency { get; init; } = 0.01f;
    public int Octaves { get; init; } = 4;
    public float Amplitude { get; init; } = 32f;
    public float BaseHeight { get; init; } = 64f;
}
