namespace VoxelEngine.World;

public sealed record ClimateSpawnDefinition(
    string EntityId,
    int MaxCount,
    float MinSpawnDistance,
    float SpawnInterval,
    SpawnActivity Activity = SpawnActivity.Any);
