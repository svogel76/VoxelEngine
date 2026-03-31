using System.Text.Json;
using System.Text.Json.Serialization;
using VoxelEngine.Entity;
using VoxelEngine.World;

namespace VoxelEngine.Core;

public class EngineSettings
{
    // Window
    public string Title          { get; init; } = "VoxelEngine";
    public int    WindowWidth    { get; init; } = 1280;
    public int    WindowHeight   { get; init; } = 720;
    public bool   VSync          { get; init; } = true;

    // Loop
    public double TargetUPS      { get; init; } = 60.0;

    /// <summary>0 = unbegrenzt, VSync bestimmt Obergrenze</summary>
    public double TargetFPS      { get; init; } = 0.0;

    // Camera
    public float MovementSpeed    { get; init; } = 5.0f;
    public float MouseSensitivity { get; init; } = 0.1f;
    public float Fov              { get; init; } = 75.0f;
    public float NearPlane        { get; init; } = 0.1f;
    public float FarPlane         { get; init; } = 500.0f;
    public bool  InvertMouseY     { get; init; } = false;

    // Camera start position
    public (float X, float Y, float Z) CameraStartPosition { get; init; } = (0f, 80f, 30f);
    public float InteractionReach { get; init; } = 5.0f;

    // Terrain
    /// <summary>Wird spaeter durch BiomeDefinitions[] ersetzt</summary>
    public NoiseSettings Terrain  { get; init; } = new NoiseSettings();
    public int           SeaLevel { get; init; } = 64;

    // World
    /// <summary>Radius in Chunks der um den Spieler geladen wird</summary>
    public int RenderDistance { get; init; } = 5;

    /// <summary>
    /// Chunks werden erst entladen wenn sie diesen Radius ueberschreiten.
    /// Muss groesser als RenderDistance sein um Chunk-Flicker zu vermeiden.
    /// </summary>
    public int UnloadDistance { get; init; } = 7;

    /// <summary>Maximale Anzahl neu geladener Chunks pro Update-Schritt</summary>
    public int MaxChunksLoadedPerFrame { get; init; } = 8;

    /// <summary>Maximale Anzahl an GL-Mesh-Uploads pro Frame.</summary>
    public int MaxGlUploadsPerFrame { get; init; } = 16;

    /// <summary>Radius in Chunks fuer priorisierte Start-Chunks direkt nach dem Laden</summary>
    public int InitialChunkLoadRadius { get; init; } = 3;

    /// <summary>Maximale Baum-Ausdehnung in Bloecken fuer Chunk- und Sample-Queries</summary>
    public int TreeInfluenceRadius { get; init; } = 8;

    /// <summary>Zellgroesse des Entity-Spatial-Hashings in Weltbloecken.</summary>
    public float EntitySpatialHashCellSize { get; init; } = Chunk.Width;
    /// <summary>Skalierungsfaktor fuer importierte Entity-Voxelmodelle. 1.0 = 1 Voxel entspricht 1 Welt-Unit.</summary>
    public float EntityVoxelScale { get; init; } = 1.0f;
    /// <summary>Maximaler horizontaler Radius um den Spieler fuer klimaabhaengige Spawn- und Despawn-Pruefungen.</summary>
    public float MaxSpawnDistance { get; init; } = Chunk.Width * 2;
    /// <summary>Zeitintervall in Sekunden zwischen zwei periodischen Spawn- und Despawn-Pruefungen.</summary>
    public float SpawnTickInterval { get; init; } = 5f;
    /// <summary>Anzahl zufaelliger Kandidaten pro Spawn-Check bevor ein Spawn-Versuch aufgegeben wird.</summary>
    public int EntitySpawnPlacementAttempts { get; init; } = 16;
    /// <summary>Radius um den Spieler in dem inaktive Entities beim Tag/Nacht-Wechsel nicht despawnen.</summary>
    public float EntityDespawnProtectionRadius { get; init; } = Chunk.Width * 2;

    // Time
    /// <summary>Startzeit der Welt in Stunden (0.0-24.0)</summary>
    public double InitialTime { get; init; } = 12.0;
    /// <summary>Zeitbeschleunigung: 72 = ~20min Tag (wie Minecraft), 1 = Echtzeit</summary>
    public double TimeScale { get; init; } = 0.0;

    // Player physics
    public float Gravity      { get; init; } = 28.0f;
    public float MaxFallSpeed { get; init; } = 50.0f;
    public float JumpVelocity { get; init; } = 8.0f;
    public float StepHeight   { get; init; } = 1.0f;
    public float StepUpMaxVisualDrop { get; init; } = 0.5f;
    public float StepUpSmoothingSpeed { get; init; } = 4.0f;
    public bool  EnableStepUp { get; init; } = true;

    // Player body
    public float PlayerWidth  { get; init; } = 0.6f;
    public float PlayerHeight { get; init; } = 1.8f;
    public float EyeHeight    { get; init; } = 1.62f;

    // Fog
    /// <summary>Fog beginnt bei diesem Anteil der Render Distance (0.0-1.0)</summary>
    public float FogStartFactor { get; init; } = 0.5f;
    /// <summary>Fog endet (vollstaendig) bei diesem Anteil der Render Distance (0.0-1.0)</summary>
    public float FogEndFactor   { get; init; } = 0.9f;

    // Persistence
    /// <summary>Verzeichnis fuer Spielstaende (relativ zum Arbeitsverzeichnis)</summary>
    public string SaveDirectory { get; init; } = "saves/world";

    // Debug Console
    /// <summary>Maximale Anzahl der gespeicherten Eintraege in der Konsolen-History</summary>
    public int ConsoleHistorySize { get; init; } = 50;
    public bool ShowFps { get; init; } = true;

    // HUD / Hotbar
    /// <summary>Groesse eines Hotbar-Slots in Pixeln</summary>
    public int  HotbarSlotSize         { get; init; } = 40;
    /// <summary>Zifferntasten 1-9 waehlen Hotbar-Slot direkt an</summary>
    public bool EnableHotbarNumberKeys { get; init; } = true;

    // Vitals (Health & Hunger)
    /// <summary>Konfiguration der Spieler-Vitalwerte. null = Standardwerte aus VitalsConfig.</summary>
    public VitalsConfig? Vitals { get; init; } = null;

    public static EngineSettings LoadFrom(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        string directoryPath = ResolveDirectoryPath(path);
        string filePath = Path.Combine(directoryPath, "engine.json");
        var defaults = new EngineSettings();

        if (!File.Exists(filePath))
            return defaults;

        EngineSettingsFile? document;
        try
        {
            document = JsonSerializer.Deserialize<EngineSettingsFile>(File.ReadAllText(filePath), JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Invalid engine settings JSON in '{filePath}'.", ex);
        }

        if (document is null)
            return defaults;

        return new EngineSettings
        {
            Title = defaults.Title,
            WindowWidth = defaults.WindowWidth,
            WindowHeight = defaults.WindowHeight,
            VSync = defaults.VSync,
            TargetUPS = document.Engine?.UpdatesPerSecond ?? defaults.TargetUPS,
            TargetFPS = defaults.TargetFPS,
            MovementSpeed = defaults.MovementSpeed,
            MouseSensitivity = defaults.MouseSensitivity,
            Fov = defaults.Fov,
            NearPlane = defaults.NearPlane,
            FarPlane = defaults.FarPlane,
            InvertMouseY = defaults.InvertMouseY,
            CameraStartPosition = defaults.CameraStartPosition,
            InteractionReach = defaults.InteractionReach,
            Terrain = defaults.Terrain,
            SeaLevel = document.World?.SeaLevel ?? defaults.SeaLevel,
            RenderDistance = document.World?.RenderDistance ?? defaults.RenderDistance,
            UnloadDistance = document.World?.UnloadDistance ?? defaults.UnloadDistance,
            MaxChunksLoadedPerFrame = defaults.MaxChunksLoadedPerFrame,
            MaxGlUploadsPerFrame = document.Engine?.MaxGlUploadsPerFrame ?? defaults.MaxGlUploadsPerFrame,
            InitialChunkLoadRadius = defaults.InitialChunkLoadRadius,
            TreeInfluenceRadius = defaults.TreeInfluenceRadius,
            EntitySpatialHashCellSize = defaults.EntitySpatialHashCellSize,
            EntityVoxelScale = defaults.EntityVoxelScale,
            MaxSpawnDistance = defaults.MaxSpawnDistance,
            SpawnTickInterval = defaults.SpawnTickInterval,
            EntitySpawnPlacementAttempts = defaults.EntitySpawnPlacementAttempts,
            EntityDespawnProtectionRadius = defaults.EntityDespawnProtectionRadius,
            InitialTime = defaults.InitialTime,
            TimeScale = defaults.TimeScale,
            Gravity = AbsOrDefault(document.Physics?.Gravity, defaults.Gravity),
            MaxFallSpeed = AbsOrDefault(document.Physics?.MaxFallSpeed, defaults.MaxFallSpeed),
            JumpVelocity = document.Physics?.JumpVelocity ?? defaults.JumpVelocity,
            StepHeight = document.Physics?.StepHeight ?? defaults.StepHeight,
            StepUpMaxVisualDrop = defaults.StepUpMaxVisualDrop,
            StepUpSmoothingSpeed = defaults.StepUpSmoothingSpeed,
            EnableStepUp = document.Physics?.EnableStepUp ?? defaults.EnableStepUp,
            FogStartFactor = document.Fog?.StartPercent ?? defaults.FogStartFactor,
            FogEndFactor = document.Fog?.EndPercent ?? defaults.FogEndFactor,
            SaveDirectory = defaults.SaveDirectory,
            ConsoleHistorySize = defaults.ConsoleHistorySize,
            ShowFps = document.Debug?.ShowFps ?? defaults.ShowFps,
            HotbarSlotSize = defaults.HotbarSlotSize,
            EnableHotbarNumberKeys = defaults.EnableHotbarNumberKeys,
            Vitals = defaults.Vitals
        };
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static float AbsOrDefault(float? value, float fallback)
        => value is null ? fallback : MathF.Abs(value.Value);

    private static string ResolveDirectoryPath(string path)
    {
        if (Path.IsPathRooted(path))
            return path;

        string currentDirectoryPath = Path.GetFullPath(path);
        if (Directory.Exists(currentDirectoryPath))
            return currentDirectoryPath;

        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
    }

    private sealed class EngineSettingsFile
    {
        public EngineSection? Engine { get; init; }
        public WorldSection? World { get; init; }
        public PhysicsSection? Physics { get; init; }
        public FogSection? Fog { get; init; }
        public DebugSection? Debug { get; init; }
    }

    private sealed class EngineSection
    {
        [JsonPropertyName("updates_per_second")]
        public double? UpdatesPerSecond { get; init; }

        [JsonPropertyName("max_gl_uploads_per_frame")]
        public int? MaxGlUploadsPerFrame { get; init; }
    }

    private sealed class WorldSection
    {
        [JsonPropertyName("render_distance")]
        public int? RenderDistance { get; init; }

        [JsonPropertyName("unload_distance")]
        public int? UnloadDistance { get; init; }

        [JsonPropertyName("sea_level")]
        public int? SeaLevel { get; init; }
    }

    private sealed class PhysicsSection
    {
        public float? Gravity { get; init; }

        [JsonPropertyName("max_fall_speed")]
        public float? MaxFallSpeed { get; init; }

        [JsonPropertyName("jump_velocity")]
        public float? JumpVelocity { get; init; }

        [JsonPropertyName("step_height")]
        public float? StepHeight { get; init; }

        [JsonPropertyName("enable_step_up")]
        public bool? EnableStepUp { get; init; }
    }

    private sealed class FogSection
    {
        [JsonPropertyName("start_percent")]
        public float? StartPercent { get; init; }

        [JsonPropertyName("end_percent")]
        public float? EndPercent { get; init; }
    }

    private sealed class DebugSection
    {
        [JsonPropertyName("show_fps")]
        public bool? ShowFps { get; init; }
    }
}
