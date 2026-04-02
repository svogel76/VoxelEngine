using System.Numerics;
using VoxelEngine.World;

namespace VoxelEngine.Rendering;

internal static class AtmosphericFogSystem
{
    internal const float MorningPeakHour = 6.5f;
    internal const float NoonClearHour = 13f;
    internal const float EveningPeakHour = 18.5f;

    private const float FogDisabledStart = 1e30f;
    private const float FogDisabledEnd = 2e30f;

    private static readonly Vector3 NightTint = new(0.55f, 0.62f, 0.75f);
    private static readonly Vector3 MorningTint = new(0.86f, 0.91f, 0.97f);
    private static readonly Vector3 NoonTint = new(0.95f, 0.97f, 0.99f);
    private static readonly Vector3 EveningTint = new(0.99f, 0.74f, 0.58f);

    public static FogProfile Build(
        float baseStartFactor,
        float baseEndFactor,
        int renderDistanceChunks,
        float worldTimeHours,
        float cameraHeight,
        ClimateSample climate,
        Vector3 baseSkyFogColor)
    {
        if (baseEndFactor <= 0f)
            return new FogProfile(FogDisabledStart, FogDisabledEnd, baseSkyFogColor);

        float climateDensity = climate.PrimaryZone.FogDensity;
        float secondaryDensity = climate.SecondaryZone.FogDensity;
        float blendedDensity = Lerp(climateDensity, secondaryDensity, Math.Clamp(climate.TransitionFactor, 0f, 1f));

        float dayDensity = ComputeDayDensity(worldTimeHours);
        float heightDensity = ComputeHeightDensity(cameraHeight, climate.SurfaceHeight);
        float fogDensity = Math.Clamp(blendedDensity * dayDensity * heightDensity, 0.45f, 2.5f);

        float startFactorRaw = baseStartFactor / fogDensity;
        float maxStartFactor = MathF.Min(0.94f, baseEndFactor - 0.05f);
        if (maxStartFactor < 0.18f)
            maxStartFactor = 0.18f;

        float startFactor = Math.Clamp(startFactorRaw, 0.18f, maxStartFactor);

        float minEndFactor = MathF.Min(0.995f, startFactor + 0.05f);
        float endFactorRaw = baseEndFactor / MathF.Sqrt(fogDensity);
        float endFactor = Math.Clamp(endFactorRaw, minEndFactor, 0.995f);

        float renderDistanceBlocks = renderDistanceChunks * Chunk.Width;
        Vector3 fogColor = ComputeFogColor(baseSkyFogColor, worldTimeHours, climate.PrimaryZone.FogTintStrength);

        return new FogProfile(renderDistanceBlocks * startFactor, renderDistanceBlocks * endFactor, fogColor);
    }

    private static float ComputeDayDensity(float worldTimeHours)
    {
        float morning = Gaussian(worldTimeHours, MorningPeakHour, 1.8f);
        float noon = Gaussian(worldTimeHours, NoonClearHour, 2.8f);
        float evening = Gaussian(worldTimeHours, EveningPeakHour, 2.2f);

        float density = 1f + morning * 0.55f + evening * 0.3f - noon * 0.28f;
        return Math.Clamp(density, 0.68f, 1.75f);
    }

    private static float ComputeHeightDensity(float cameraHeight, int surfaceHeight)
    {
        float relativeHeight = cameraHeight - surfaceHeight;
        float normalizedHeight = Math.Clamp((relativeHeight + 8f) / 96f, 0f, 1f);
        return Lerp(1.3f, 0.62f, normalizedHeight);
    }

    private static Vector3 ComputeFogColor(Vector3 baseSkyFogColor, float worldTimeHours, float baseTintStrength)
    {
        Vector3 dayTint = GetTimeTint(worldTimeHours);

        float eveningBoost = Gaussian(worldTimeHours, EveningPeakHour, 1.6f) * 0.18f;
        float morningBoost = Gaussian(worldTimeHours, MorningPeakHour, 1.7f) * 0.12f;
        float tintStrength = Math.Clamp(baseTintStrength + eveningBoost + morningBoost, 0f, 0.75f);

        return Vector3.Clamp(Vector3.Lerp(baseSkyFogColor, dayTint, tintStrength), Vector3.Zero, Vector3.One);
    }

    private static Vector3 GetTimeTint(float worldTimeHours)
    {
        float hour = NormalizeHour(worldTimeHours);

        if (hour < 5f)
            return NightTint;
        if (hour < 9f)
            return Vector3.Lerp(NightTint, MorningTint, (hour - 5f) / 4f);
        if (hour < 16f)
            return Vector3.Lerp(MorningTint, NoonTint, (hour - 9f) / 7f);
        if (hour < 20f)
            return Vector3.Lerp(NoonTint, EveningTint, (hour - 16f) / 4f);

        return Vector3.Lerp(EveningTint, NightTint, (hour - 20f) / 4f);
    }

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;

    private static float Gaussian(float x, float mean, float sigma)
    {
        float offset = x - mean;
        return MathF.Exp(-(offset * offset) / (2f * sigma * sigma));
    }

    private static float NormalizeHour(float worldTimeHours)
    {
        float normalized = worldTimeHours % 24f;
        if (normalized < 0f)
            normalized += 24f;

        return normalized;
    }
}
