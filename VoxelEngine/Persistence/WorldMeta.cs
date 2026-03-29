namespace VoxelEngine.Persistence;

/// <summary>Gespeicherte Welt-Metadaten: Uhrzeit, Tag, Seed und Zeitskala.</summary>
public sealed record WorldMeta(
    double Time,
    int DayCount,
    int Seed,
    double TimeScale);
