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
    /// <summary>Wird später durch BiomeDefinitions[] ersetzt</summary>
    public NoiseSettings Terrain  { get; init; } = new NoiseSettings();
    public int           SeaLevel { get; init; } = 64;

    // World
    /// <summary>Radius in Chunks der um den Spieler geladen wird</summary>
    public int RenderDistance { get; init; } = 5;

    /// <summary>
    /// Chunks werden erst entladen wenn sie diesen Radius überschreiten.
    /// Muss größer als RenderDistance sein um Chunk-Flicker zu vermeiden.
    /// </summary>
    public int UnloadDistance { get; init; } = 7;

    /// <summary>Maximale Anzahl neu geladener Chunks pro Update-Schritt</summary>
    public int MaxChunksLoadedPerFrame { get; init; } = 8;

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
    /// <summary>Startzeit der Welt in Stunden (0.0–24.0)</summary>
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

    // Fog
    /// <summary>Fog beginnt bei diesem Anteil der Render Distance (0.0–1.0)</summary>
    public float FogStartFactor { get; init; } = 0.5f;
    /// <summary>Fog endet (vollständig) bei diesem Anteil der Render Distance (0.0–1.0)</summary>
    public float FogEndFactor   { get; init; } = 0.9f;

    // Persistence
    /// <summary>Verzeichnis für Spielstände (relativ zum Arbeitsverzeichnis)</summary>
    public string SaveDirectory { get; init; } = "saves/world";

    // Debug Console
    /// <summary>Maximale Anzahl der gespeicherten Einträge in der Konsolen-History</summary>
    public int ConsoleHistorySize { get; init; } = 50;

    // HUD / Hotbar
    /// <summary>Größe eines Hotbar-Slots in Pixeln</summary>
    public int  HotbarSlotSize         { get; init; } = 40;
    /// <summary>Zifferntasten 1-9 wählen Hotbar-Slot direkt an</summary>
    public bool EnableHotbarNumberKeys { get; init; } = true;

    // Vitals (Health & Hunger)
    /// <summary>Konfiguration der Spieler-Vitalwerte. null = Standardwerte aus VitalsConfig.</summary>
    public VitalsConfig? Vitals { get; init; } = null;
}
