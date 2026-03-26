namespace VoxelEngine.World;

public sealed class ClimateSystem
{
    private const float TemperatureLatitudeSpan = 1800f;
    private const float TemperatureNoiseFrequency = 0.0011f;
    private const float TemperatureNoiseStrength = 0.20f;
    private const float HumidityNoiseFrequency = 0.0014f;
    private const float ColdTemperateBoundary = 0.38f;
    private const float TemperateHotBoundary = 0.68f;
    private const float TemperatureBlendWidth = 0.06f;
    private const float HumidityBoundary = 0.52f;
    private const float HumidityBlendWidth = 0.18f;
    private const int ClimateSeedOffset = 10_000;

    private readonly FastNoiseLite _temperatureNoise;
    private readonly FastNoiseLite _humidityNoise;
    private readonly ClimateZone[,] _zoneLookup;
    private readonly Dictionary<ClimateZone, FastNoiseLite> _terrainNoiseByZone;

    public ClimateSystem(NoiseSettings temperateDefaults)
    {
        int baseSeed = temperateDefaults.Seed;

        _temperatureNoise = CreateNoise(baseSeed + ClimateSeedOffset + 1, TemperatureNoiseFrequency, 2);
        _humidityNoise = CreateNoise(baseSeed + ClimateSeedOffset + 2, HumidityNoiseFrequency, 3);

        _zoneLookup = CreateZoneLookup(temperateDefaults);
        _terrainNoiseByZone = _zoneLookup.Cast<ClimateZone>()
            .Distinct()
            .ToDictionary(zone => zone, zone => CreateNoise(zone.Terrain.Seed, zone.Terrain.Frequency, zone.Terrain.Octaves));
    }

    public ClimateSample Sample(int worldX, int worldZ)
    {
        float temperature = SampleTemperature(worldX, worldZ);
        float humidity = SampleHumidity(worldX, worldZ);

        var (tempLower, tempUpper, tempBlend) = GetTemperatureBlend(temperature);
        var (humidityLower, humidityUpper, humidityBlend) = GetHumidityBlend(humidity);

        var weights = new Dictionary<ClimateZone, float>();
        AddWeight(weights, _zoneLookup[humidityLower, tempLower], (1f - tempBlend) * (1f - humidityBlend));
        AddWeight(weights, _zoneLookup[humidityLower, tempUpper], tempBlend * (1f - humidityBlend));
        AddWeight(weights, _zoneLookup[humidityUpper, tempLower], (1f - tempBlend) * humidityBlend);
        AddWeight(weights, _zoneLookup[humidityUpper, tempUpper], tempBlend * humidityBlend);

        if (weights.Count == 0)
            throw new InvalidOperationException("Climate sampling produced no zone weights.");

        var orderedWeights = weights
            .OrderByDescending(pair => pair.Value)
            .ToArray();

        var primary = orderedWeights[0];
        var secondary = orderedWeights.Length > 1 ? orderedWeights[1] : primary;
        float transition = orderedWeights.Length > 1
            ? secondary.Value / MathF.Max(primary.Value + secondary.Value, float.Epsilon)
            : 0f;

        float height = 0f;
        foreach (var (zone, weight) in weights)
        {
            height += SampleZoneHeight(zone, worldX, worldZ) * weight;
        }

        int surfaceHeight = Math.Clamp((int)MathF.Round(height), 1, Chunk.Height - 1);
        byte surfaceBlock = surfaceHeight >= primary.Key.SnowLine ? BlockType.Snow : primary.Key.SurfaceBlock;

        return new ClimateSample(
            temperature,
            humidity,
            primary.Key,
            secondary.Key,
            transition,
            surfaceHeight,
            surfaceBlock,
            primary.Key.SubsurfaceBlock,
            primary.Key.StoneBlock,
            primary.Key.SeaBlock);
    }

    private float SampleTemperature(int worldX, int worldZ)
    {
        float latitude = 0.5f - worldZ / TemperatureLatitudeSpan;
        latitude = Math.Clamp(latitude, 0f, 1f);

        float noise = _temperatureNoise.GetNoise(worldX, worldZ) * TemperatureNoiseStrength;
        float temperature = latitude * (1f + noise);
        return Math.Clamp(temperature, 0f, 1f);
    }

    private float SampleHumidity(int worldX, int worldZ)
    {
        float humidity = 0.5f + _humidityNoise.GetNoise(worldX, worldZ) * 0.5f;
        return Math.Clamp(humidity, 0f, 1f);
    }

    private (int lower, int upper, float blend) GetTemperatureBlend(float value)
    {
        float halfWidth = TemperatureBlendWidth * 0.5f;

        if (value <= ColdTemperateBoundary - halfWidth)
            return (0, 0, 0f);
        if (value < ColdTemperateBoundary + halfWidth)
            return (0, 1, Smoothstep(ColdTemperateBoundary - halfWidth, ColdTemperateBoundary + halfWidth, value));
        if (value <= TemperateHotBoundary - halfWidth)
            return (1, 1, 0f);
        if (value < TemperateHotBoundary + halfWidth)
            return (1, 2, Smoothstep(TemperateHotBoundary - halfWidth, TemperateHotBoundary + halfWidth, value));
        return (2, 2, 0f);
    }

    private static (int lower, int upper, float blend) GetHumidityBlend(float value)
    {
        float halfWidth = HumidityBlendWidth * 0.5f;

        if (value <= HumidityBoundary - halfWidth)
            return (0, 0, 0f);
        if (value < HumidityBoundary + halfWidth)
            return (0, 1, Smoothstep(HumidityBoundary - halfWidth, HumidityBoundary + halfWidth, value));
        return (1, 1, 0f);
    }

    private float SampleZoneHeight(ClimateZone zone, int worldX, int worldZ)
    {
        var noise = _terrainNoiseByZone[zone];
        float noiseValue = noise.GetNoise(worldX, worldZ);
        float height = zone.Terrain.BaseHeight + noiseValue * zone.Terrain.Amplitude;
        return Math.Clamp(height, 1f, Chunk.Height - 1f);
    }

    private static void AddWeight(Dictionary<ClimateZone, float> weights, ClimateZone zone, float weight)
    {
        if (weight <= 0f)
            return;

        if (weights.TryGetValue(zone, out float current))
            weights[zone] = current + weight;
        else
            weights[zone] = weight;
    }

    private static float Smoothstep(float edge0, float edge1, float value)
    {
        if (MathF.Abs(edge1 - edge0) <= float.Epsilon)
            return 0f;

        float t = Math.Clamp((value - edge0) / (edge1 - edge0), 0f, 1f);
        return t * t * (3f - 2f * t);
    }

    private static FastNoiseLite CreateNoise(int seed, float frequency, int octaves)
    {
        var noise = new FastNoiseLite(seed);
        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        noise.SetFrequency(frequency);
        noise.SetFractalType(FastNoiseLite.FractalType.FBm);
        noise.SetFractalOctaves(octaves);
        return noise;
    }

    private static ClimateZone[,] CreateZoneLookup(NoiseSettings temperateDefaults)
    {
        var steppe = new ClimateZone(
            "Steppe",
            new NoiseSettings { Seed = temperateDefaults.Seed + 101, BaseHeight = 66f, Amplitude = 14f, Frequency = 0.0075f, Octaves = 3 },
            BlockType.DryGrass,
            BlockType.Dirt,
            BlockType.Stone,
            BlockType.Water,
            int.MaxValue);

        var savanna = new ClimateZone(
            "Savanne",
            new NoiseSettings { Seed = temperateDefaults.Seed + 102, BaseHeight = 68f, Amplitude = 18f, Frequency = 0.0085f, Octaves = 4 },
            BlockType.DryGrass,
            BlockType.Dirt,
            BlockType.Stone,
            BlockType.Water,
            int.MaxValue);

        var desert = new ClimateZone(
            "Wüste",
            new NoiseSettings { Seed = temperateDefaults.Seed + 103, BaseHeight = 62f, Amplitude = 10f, Frequency = 0.0060f, Octaves = 3 },
            BlockType.Sand,
            BlockType.Sand,
            BlockType.Stone,
            BlockType.Water,
            int.MaxValue);

        var taiga = new ClimateZone(
            "Taiga",
            new NoiseSettings { Seed = temperateDefaults.Seed + 104, BaseHeight = 78f, Amplitude = 40f, Frequency = 0.0070f, Octaves = 5 },
            BlockType.Grass,
            BlockType.Dirt,
            BlockType.Stone,
            BlockType.Water,
            100);

        var temperate = new ClimateZone(
            "Gemäßigt",
            new NoiseSettings
            {
                Seed = temperateDefaults.Seed + 105,
                BaseHeight = temperateDefaults.BaseHeight,
                Amplitude = temperateDefaults.Amplitude,
                Frequency = temperateDefaults.Frequency,
                Octaves = temperateDefaults.Octaves
            },
            BlockType.Grass,
            BlockType.Dirt,
            BlockType.Stone,
            BlockType.Water,
            int.MaxValue);

        var tropics = new ClimateZone(
            "Tropen",
            new NoiseSettings { Seed = temperateDefaults.Seed + 106, BaseHeight = 72f, Amplitude = 26f, Frequency = 0.0100f, Octaves = 4 },
            BlockType.Grass,
            BlockType.Dirt,
            BlockType.Stone,
            BlockType.Water,
            int.MaxValue);

        return new[,]
        {
            { steppe, savanna, desert },
            { taiga, temperate, tropics }
        };
    }
}

public readonly record struct ClimateSample(
    float Temperature,
    float Humidity,
    ClimateZone PrimaryZone,
    ClimateZone SecondaryZone,
    float TransitionFactor,
    int SurfaceHeight,
    byte SurfaceBlock,
    byte SubsurfaceBlock,
    byte StoneBlock,
    byte SeaBlock);
