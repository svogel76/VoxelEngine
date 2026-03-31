namespace VoxelEngine.World;

public readonly record struct ChunkJob(int ChunkX, int ChunkZ, ChunkJobKind Kind = ChunkJobKind.Generate);

public enum ChunkJobKind
{
    Generate,
    Rebuild
}
