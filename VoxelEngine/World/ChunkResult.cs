namespace VoxelEngine.World;

public sealed class ChunkResult
{
    public int ChunkX { get; }
    public int ChunkZ { get; }
    public Chunk Chunk { get; }
    public float[] OpaqueVertices { get; }
    public uint[] OpaqueIndices { get; }
    public float[] CutoutVertices { get; }
    public uint[] CutoutIndices { get; }
    public float[] TransparentVertices { get; }
    public uint[] TransparentIndices { get; }

    public ChunkResult(
        int x,
        int z,
        Chunk chunk,
        float[] opaqueVerts,
        uint[] opaqueIdx,
        float[] cutoutVerts,
        uint[] cutoutIdx,
        float[] transparentVerts,
        uint[] transparentIdx)
    {
        ChunkX = x;
        ChunkZ = z;
        Chunk = chunk;
        OpaqueVertices = opaqueVerts;
        OpaqueIndices = opaqueIdx;
        CutoutVertices = cutoutVerts;
        CutoutIndices = cutoutIdx;
        TransparentVertices = transparentVerts;
        TransparentIndices = transparentIdx;
    }

    public bool HasOpaqueMesh => OpaqueVertices.Length > 0;
    public bool HasCutoutMesh => CutoutVertices.Length > 0;
    public bool HasTransparentMesh => TransparentVertices.Length > 0;
}
