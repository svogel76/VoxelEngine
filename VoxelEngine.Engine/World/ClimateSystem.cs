namespace VoxelEngine.World;

public sealed class ClimateSystem
{
    private const string DefaultClimateDirectory = "Assets/Climate";
    private const float TemperatureLatitudeSpan = 3600f;   // doppelt so großräumige Klimazonen
    private const float TemperatureNoiseFrequency = 0.0008f; // langsamere Übergangs-Noise
    private const float TemperatureNoiseStrength = 0.18f;
    private const float HumidityNoiseFrequency = 0.0010f;
    private const float ColdTemperateBoundary = 0.38f;
    private const float TemperateHotBoundary = 0.68f;
    private const float TemperatureBlendWidth = 0.10f;    // sanftere Übergänge
    private const float HumidityBoundary = 0.52f;
    private const float HumidityBlendWidth = 0.24f;       // breitere Blend-Zonen
    private const int ClimateSeedOffset = 10_000;

    private readonly FastNoiseLite _temperatureNoise;
    private readonly FastNoiseLite _humidityNoise;
    private readonly ClimateZone[,] _zoneLookup;
    private readonly Dictionary<ClimateZone, FastNoiseLite> _terrainNoiseByZone;
    private readonly Dictionary<string, ClimateZone> _zonesById;

    public ClimateSystem(NoiseSettings temperateDefaults, string? climateDirectory = null)
    {
        int baseSeed = temperateDefaults.Seed;

        _temperatureNoise = CreateNoise(baseSeed + ClimateSeedOffset + 1, TemperatureNoiseFrequency, 2);
        _humidityNoise = CreateNoise(baseSeed + ClimateSeedOffset + 2, HumidityNoiseFrequency, 3);

        _zoneLookup = CreateZoneLookup(temperateDefaults, climateDirectory);
        _zonesById = _zoneLookup.Cast<ClimateZone>()
            .Distinct()
            .ToDictionary(zone => zone.Id, StringComparer.OrdinalIgnoreCase);
        _terrainNoiseByZone = _zoneLookup.Cast<ClimateZone>()
            .Distinct()
            .ToDictionary(zone => zone, zone => CreateNoise(zone.Terrain.Seed, zone.Terrain.Frequency, zone.Terrain.Octaves));
    }

    public IReadOnlyCollection<ClimateZone> Zones => _zonesById.Values;

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

    private static ClimateZone[,] CreateZoneLookup(NoiseSettings temperateDefaults, string? climateDirectory)
    {
        string resolvedDirectory = ResolveClimateDirectory(climateDirectory);
        var documents = LoadClimateDocuments(resolvedDirectory);

        var steppe = CreateZone(documents, "steppe", temperateDefaults);
        var savanna = CreateZone(documents, "savanna", temperateDefaults);
        var desert = CreateZone(documents, "desert", temperateDefaults);
        var taiga = CreateZone(documents, "taiga", temperateDefaults);
        var temperate = CreateZone(documents, "temperate", temperateDefaults);
        var tropics = CreateZone(documents, "tropics", temperateDefaults);

        return new[,]
        {
            { steppe, savanna, desert },
            { taiga, temperate, tropics }
        };
    }

    private static string ResolveClimateDirectory(string? climateDirectory)
    {
        if (!string.IsNullOrWhiteSpace(climateDirectory))
            return climateDirectory;

        string baseDirectoryCandidate = Path.Combine(AppContext.BaseDirectory, "Assets", "Climate");
        if (Directory.Exists(baseDirectoryCandidate))
            return baseDirectoryCandidate;

        return DefaultClimateDirectory;
    }

    private static Dictionary<string, ClimateZoneDocument> LoadClimateDocuments(string climateDirectory)
    {
        if (!Directory.Exists(climateDirectory))
            throw new DirectoryNotFoundException($"Climate asset directory '{climateDirectory}' was not found.");

        var documents = new Dictionary<string, ClimateZoneDocument>(StringComparer.OrdinalIgnoreCase);
        var jsonOptions = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        foreach (string file in Directory.EnumerateFiles(climateDirectory, "*.json", SearchOption.TopDirectoryOnly))
        {
            try
            {
                string json = File.ReadAllText(file);
                var document = System.Text.Json.JsonSerializer.Deserialize<ClimateZoneDocument>(json, jsonOptions)
                    ?? throw new FormatException($"Climate zone file '{file}' is empty.");

                ValidateDocument(document, file);
                if (!documents.TryAdd(document.Id, document))
                    throw new FormatException($"Climate zone id '{document.Id}' is defined more than once.");
            }
            catch (System.Text.Json.JsonException exception)
            {
                throw new FormatException($"Invalid climate zone JSON in '{file}'.", exception);
            }
        }

        return documents;
    }

    private static ClimateZone CreateZone(
        IReadOnlyDictionary<string, ClimateZoneDocument> documents,
        string id,
        NoiseSettings temperateDefaults)
    {
        if (!documents.TryGetValue(id, out var document))
            throw new KeyNotFoundException($"Climate zone '{id}' is missing in Assets/Climate.");

        var blocks = document.Blocks!;
        var trees = document.Trees!;
        var spawns = document.Spawns!;

        return new ClimateZone(
            document.Id,
            ToDisplayName(document.Id),
            CreateTerrainSettings(document, temperateDefaults),
            ResolveBlock(blocks.Surface, document.Id, nameof(blocks.Surface)),
            ResolveBlock(blocks.Subsurface, document.Id, nameof(blocks.Subsurface)),
            ResolveBlock(blocks.Stone, document.Id, nameof(blocks.Stone)),
            ResolveBlock(blocks.Sea, document.Id, nameof(blocks.Sea)),
            document.SnowLine,
            trees.Density,
            ResolveTreeTemplate(trees.Template, document.Id),
            spawns
                .Select(spawn => CreateSpawnDefinition(spawn, document.Id))
                .ToArray());
    }

    private static NoiseSettings CreateTerrainSettings(ClimateZoneDocument document, NoiseSettings temperateDefaults)
    {
        var terrain = document.Terrain!;
        return new NoiseSettings
        {
            Seed = CreateZoneSeed(temperateDefaults.Seed, document.Id),
            BaseHeight = terrain.BaseHeight,
            Amplitude = terrain.Amplitude,
            Frequency = terrain.Frequency,
            Octaves = terrain.Octaves
        };
    }

    private static int CreateZoneSeed(int baseSeed, string id)
    {
        uint hash = 2166136261u;
        foreach (char character in id)
        {
            hash ^= char.ToLowerInvariant(character);
            hash *= 16777619u;
        }

        hash ^= (uint)baseSeed;
        hash *= 16777619u;
        return (int)(hash & 0x7FFFFFFF);
    }

    private static byte ResolveBlock(string blockName, string climateId, string propertyName)
    {
        try
        {
            return BlockRegistry.Get(blockName).Id;
        }
        catch (KeyNotFoundException exception)
        {
            throw new FormatException($"Climate zone '{climateId}' references unknown block '{blockName}' in '{propertyName}'.", exception);
        }
    }

    private static TreeTemplate ResolveTreeTemplate(string templateName, string climateId)
        => templateName.ToLowerInvariant() switch
        {
            "oak"          => TreeTemplate.Oak(),
            "large_oak"    => TreeTemplate.LargeOak(),
            "spruce"       => TreeTemplate.Spruce(),
            "tall_spruce"  => TreeTemplate.TallSpruce(),
            "cactus"       => TreeTemplate.Cactus(),
            "palm"         => TreeTemplate.Palm(),
            "tropical"     => TreeTemplate.Tropical(),
            "mega_tropical"=> TreeTemplate.MegaTropical(),
            "acacia"       => TreeTemplate.Acacia(),
            "shrub"        => TreeTemplate.Shrub(),
            _ => throw new FormatException($"Climate zone '{climateId}' references unknown tree template '{templateName}'.")
        };

    private static ClimateSpawnDefinition CreateSpawnDefinition(ClimateSpawnDocument spawn, string climateId)
    {
        if (string.IsNullOrWhiteSpace(spawn.Entity))
            throw new FormatException($"Climate zone '{climateId}' contains a spawn entry without an entity id.");
        if (spawn.MaxCount < 0)
            throw new FormatException($"Climate zone '{climateId}' contains a spawn entry with a negative maxCount.");
        if (spawn.MinSpawnDistance < 0f)
            throw new FormatException($"Climate zone '{climateId}' contains a spawn entry with a negative minSpawnDistance.");
        if (spawn.SpawnInterval < 0f)
            throw new FormatException($"Climate zone '{climateId}' contains a spawn entry with a negative spawnInterval.");

        return new ClimateSpawnDefinition(
            spawn.Entity,
            spawn.MaxCount,
            spawn.MinSpawnDistance,
            spawn.SpawnInterval,
            ParseSpawnActivity(spawn.Activity, climateId, spawn.Entity));
    }

    private static SpawnActivity ParseSpawnActivity(string? activity, string climateId, string entityId)
    {
        if (string.IsNullOrWhiteSpace(activity))
            return SpawnActivity.Any;

        return activity.ToLowerInvariant() switch
        {
            "any" => SpawnActivity.Any,
            "diurnal" => SpawnActivity.Diurnal,
            "nocturnal" => SpawnActivity.Nocturnal,
            _ => throw new FormatException(
                $"Climate zone '{climateId}' references unknown activity '{activity}' for spawn '{entityId}'.")
        };
    }

    private static void ValidateDocument(ClimateZoneDocument document, string file)
    {
        if (string.IsNullOrWhiteSpace(document.Id))
            throw new FormatException($"Climate zone file '{file}' is missing an id.");
        if (document.Terrain is null)
            throw new FormatException($"Climate zone '{document.Id}' is missing terrain settings.");
        if (document.Blocks is null)
            throw new FormatException($"Climate zone '{document.Id}' is missing block settings.");
        if (document.Trees is null)
            throw new FormatException($"Climate zone '{document.Id}' is missing tree settings.");
        if (document.Terrain.Frequency <= 0f)
            throw new FormatException($"Climate zone '{document.Id}' must define a terrain frequency greater than zero.");
        if (document.Terrain.Octaves <= 0)
            throw new FormatException($"Climate zone '{document.Id}' must define at least one terrain octave.");
        if (document.Terrain.Amplitude < 0f)
            throw new FormatException($"Climate zone '{document.Id}' must define a non-negative terrain amplitude.");
        if (document.Terrain.BaseHeight < 0f)
            throw new FormatException($"Climate zone '{document.Id}' must define a non-negative base height.");
        if (document.Trees.Density < 0f)
            throw new FormatException($"Climate zone '{document.Id}' must define a non-negative tree density.");
        if (string.IsNullOrWhiteSpace(document.Blocks.Surface) ||
            string.IsNullOrWhiteSpace(document.Blocks.Subsurface) ||
            string.IsNullOrWhiteSpace(document.Blocks.Stone) ||
            string.IsNullOrWhiteSpace(document.Blocks.Sea))
        {
            throw new FormatException($"Climate zone '{document.Id}' must define all block ids.");
        }

        if (string.IsNullOrWhiteSpace(document.Trees.Template))
            throw new FormatException($"Climate zone '{document.Id}' must define a tree template.");

        document.Spawns ??= [];
    }

    private static string ToDisplayName(string id)
    {
        string normalized = id.Replace('_', ' ').Replace('-', ' ');
        return string.Join(
            ' ',
            normalized
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(static part => char.ToUpperInvariant(part[0]) + part[1..].ToLowerInvariant()));
    }

    private sealed class ClimateZoneDocument
    {
        public string Id { get; set; } = string.Empty;
        public TerrainDocument? Terrain { get; set; }
        public BlockDocument? Blocks { get; set; }
        public int SnowLine { get; set; }
        public TreeDocument? Trees { get; set; }
        public List<ClimateSpawnDocument>? Spawns { get; set; }
    }

    private sealed class TerrainDocument
    {
        public float BaseHeight { get; set; }
        public float Amplitude { get; set; }
        public float Frequency { get; set; }
        public int Octaves { get; set; }
    }

    private sealed class BlockDocument
    {
        public string Surface { get; set; } = string.Empty;
        public string Subsurface { get; set; } = string.Empty;
        public string Stone { get; set; } = string.Empty;
        public string Sea { get; set; } = string.Empty;
    }

    private sealed class TreeDocument
    {
        public float Density { get; set; }
        public string Template { get; set; } = string.Empty;
    }

    private sealed class ClimateSpawnDocument
    {
        public string Entity { get; set; } = string.Empty;
        public int MaxCount { get; set; }
        public float MinSpawnDistance { get; set; }
        public float SpawnInterval { get; set; }
        public string? Activity { get; set; }
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
