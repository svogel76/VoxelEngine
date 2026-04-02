using VoxelEngine.World;

namespace VoxelEngine.Rendering;

/// <summary>
/// Greedy meshing: merges adjacent same-type faces into large quads.
/// Vertex format: (x, y, z, u, v, tileLayer, ao, faceLight) - 8 floats per vertex.
/// UV coordinates range 0..w x 0..h so the texture tiles across merged rects.
/// AO: 0 = darkest (30 % brightness), 3 = brightest (100 %).
/// faceLight: direction factor (Top=1.0, Bottom=0.4, sides=0.6-0.8)
/// multiplied by propagated sky-light mapped from [0..15] to [minAmbient..1].
/// </summary>
public static class GreedyMeshBuilder
{
    private readonly struct MaskEntry
    {
        public readonly byte Block;
        public readonly int TileLayer;
        public readonly bool IsCutout;
        public readonly bool IsBackFace;
        public readonly byte SkyLight;
        public readonly int AO0, AO1, AO2, AO3;

        public bool IsEmpty => Block == BlockType.Air;

        public MaskEntry(byte block, int tileLayer, bool isCutout, bool isBackFace, byte skyLight,
            int ao0, int ao1, int ao2, int ao3)
        {
            Block = block;
            TileLayer = tileLayer;
            IsCutout = isCutout;
            IsBackFace = isBackFace;
            SkyLight = skyLight;
            AO0 = ao0;
            AO1 = ao1;
            AO2 = ao2;
            AO3 = ao3;
        }

        public bool Matches(MaskEntry o) =>
            Block == o.Block &&
            TileLayer == o.TileLayer &&
            IsCutout == o.IsCutout &&
            IsBackFace == o.IsBackFace &&
            SkyLight == o.SkyLight &&
            AO0 == o.AO0 && AO1 == o.AO1 && AO2 == o.AO2 && AO3 == o.AO3;
    }

    private static readonly int[] Dims = { Chunk.Width, Chunk.Height, Chunk.Depth };

    public static (float[] opaqueVerts, uint[] opaqueIdx,
        float[] cutoutVerts, uint[] cutoutIdx,
        float[] transparentVerts, uint[] transparentIdx)
        Build(Chunk chunk, World.World world, WorldGenerator generator, float minSkyLightAmbient)
    {
        minSkyLightAmbient = Math.Clamp(minSkyLightAmbient, 0f, 1f);

        var opaqueVerts = new List<float>();
        var opaqueInds = new List<uint>();
        var cutoutVerts = new List<float>();
        var cutoutInds = new List<uint>();
        var transparentVerts = new List<float>();
        var transparentInds = new List<uint>();

        int baseWX = chunk.ChunkPosition.X * Chunk.Width;
        int baseWZ = chunk.ChunkPosition.Z * Chunk.Depth;

        for (int axis = 0; axis < 3; axis++)
        {
            int uAxis = (axis + 1) % 3;
            int vAxis = (axis + 2) % 3;
            int uSize = Dims[uAxis];
            int vSize = Dims[vAxis];

            int[] pos = new int[3];
            int[] q = new int[3];
            q[axis] = 1;

            var mask = new MaskEntry[uSize * vSize];

            FaceDirection forwardDir = axis switch
            {
                0 => FaceDirection.Right,
                1 => FaceDirection.Top,
                _ => FaceDirection.Front,
            };
            FaceDirection backDir = axis switch
            {
                0 => FaceDirection.Left,
                1 => FaceDirection.Bottom,
                _ => FaceDirection.Back,
            };

            for (pos[axis] = -1; pos[axis] < Dims[axis];)
            {
                int n = 0;
                for (pos[vAxis] = 0; pos[vAxis] < vSize; pos[vAxis]++)
                for (pos[uAxis] = 0; pos[uAxis] < uSize; pos[uAxis]++, n++)
                {
                    byte blockA = GetBlock(chunk, world, generator, pos[0], pos[1], pos[2], baseWX, baseWZ);
                    byte blockB = GetBlock(
                        chunk,
                        world,
                        generator,
                        pos[0] + q[0],
                        pos[1] + q[1],
                        pos[2] + q[2],
                        baseWX,
                        baseWZ);

                    if (!NeedsFace(blockA, blockB))
                    {
                        mask[n] = default;
                    }
                    else
                    {
                        bool aOwnsForward = blockA != BlockType.Air
                            && !(BlockRegistry.IsTransparent(blockA) && BlockRegistry.IsSolid(blockB));

                        if (aOwnsForward)
                        {
                            if (pos[axis] >= 0)
                            {
                                int aoSlice = pos[axis] + 1;
                                ComputeAOs(
                                    pos[uAxis],
                                    pos[vAxis],
                                    axis,
                                    uAxis,
                                    vAxis,
                                    aoSlice,
                                    baseWX,
                                    baseWZ,
                                    world,
                                    out int ao0,
                                    out int ao1,
                                    out int ao2,
                                    out int ao3,
                                    blockA);

                                byte skyLight = GetSkyLight(chunk, world, generator, pos[0] + q[0], pos[1] + q[1], pos[2] + q[2], baseWX, baseWZ);
                                mask[n] = new MaskEntry(
                                    blockA,
                                    BlockRegistry.Get(blockA).GetTile(forwardDir),
                                    BlockRegistry.IsCutout(blockA),
                                    false,
                                    skyLight,
                                    ao0,
                                    ao1,
                                    ao2,
                                    ao3);
                            }
                        }
                        else
                        {
                            if (pos[axis] + 1 < Dims[axis])
                            {
                                int aoSlice = pos[axis];
                                ComputeAOs(
                                    pos[uAxis],
                                    pos[vAxis],
                                    axis,
                                    uAxis,
                                    vAxis,
                                    aoSlice,
                                    baseWX,
                                    baseWZ,
                                    world,
                                    out int ao0,
                                    out int ao1,
                                    out int ao2,
                                    out int ao3,
                                    blockB);

                                byte skyLight = GetSkyLight(chunk, world, generator, pos[0], pos[1], pos[2], baseWX, baseWZ);
                                mask[n] = new MaskEntry(
                                    blockB,
                                    BlockRegistry.Get(blockB).GetTile(backDir),
                                    BlockRegistry.IsCutout(blockB),
                                    true,
                                    skyLight,
                                    ao0,
                                    ao1,
                                    ao2,
                                    ao3);
                            }
                        }
                    }
                }

                pos[axis]++;

                for (int j = 0; j < vSize; j++)
                for (int i = 0; i < uSize;)
                {
                    var entry = mask[j * uSize + i];
                    if (entry.IsEmpty)
                    {
                        i++;
                        continue;
                    }

                    int w = 1;
                    while (i + w < uSize && entry.Matches(mask[j * uSize + i + w]))
                        w++;

                    int h = 1;
                    bool done = false;
                    while (!done && j + h < vSize)
                    {
                        for (int k = 0; k < w; k++)
                        {
                            if (!entry.Matches(mask[(j + h) * uSize + i + k]))
                            {
                                done = true;
                                break;
                            }
                        }

                        if (!done)
                            h++;
                    }

                    int[] v0 = new int[3];
                    v0[axis] = pos[axis];
                    v0[uAxis] = i;
                    v0[vAxis] = j;

                    int[] du = new int[3];
                    du[uAxis] = w;
                    int[] dv = new int[3];
                    dv[vAxis] = h;

                    bool isTransparentFace = BlockRegistry.IsTransparent(entry.Block);
                    bool isCutoutFace = BlockRegistry.IsCutout(entry.Block);
                    var faceVerts = isTransparentFace
                        ? transparentVerts
                        : isCutoutFace
                            ? cutoutVerts
                            : opaqueVerts;
                    var faceInds = isTransparentFace
                        ? transparentInds
                        : isCutoutFace
                            ? cutoutInds
                            : opaqueInds;

                    AddQuad(
                        faceVerts,
                        faceInds,
                        v0,
                        du,
                        dv,
                        w,
                        h,
                        entry.TileLayer,
                        entry.IsCutout,
                        entry.IsBackFace,
                        entry.SkyLight,
                        entry.AO0,
                        entry.AO1,
                        entry.AO2,
                        entry.AO3,
                        baseWX,
                        baseWZ,
                        axis,
                        minSkyLightAmbient);

                    for (int jj = j; jj < j + h; jj++)
                    for (int ii = i; ii < i + w; ii++)
                        mask[jj * uSize + ii] = default;

                    i += w;
                }
            }
        }

        return (
            opaqueVerts.Count > 0 ? opaqueVerts.ToArray() : Array.Empty<float>(),
            opaqueInds.Count > 0 ? opaqueInds.ToArray() : Array.Empty<uint>(),
            cutoutVerts.Count > 0 ? cutoutVerts.ToArray() : Array.Empty<float>(),
            cutoutInds.Count > 0 ? cutoutInds.ToArray() : Array.Empty<uint>(),
            transparentVerts.Count > 0 ? transparentVerts.ToArray() : Array.Empty<float>(),
            transparentInds.Count > 0 ? transparentInds.ToArray() : Array.Empty<uint>());
    }

    private static bool NeedsFace(byte blockA, byte blockB)
    {
        if (blockA == blockB)
            return false;
        if (blockA == BlockType.Air || blockB == BlockType.Air)
            return true;
        if (!BlockRegistry.IsTransparent(blockA) && !BlockRegistry.IsTransparent(blockB))
            return false;

        return true;
    }

    private static void ComputeAOs(
        int bi,
        int bj,
        int axis,
        int uAxis,
        int vAxis,
        int aoSlice,
        int baseWX,
        int baseWZ,
        World.World world,
        out int ao0,
        out int ao1,
        out int ao2,
        out int ao3,
        byte ownerBlock = BlockType.Stone)
    {
        if (BlockRegistry.IsTransparent(ownerBlock) ||
            HasTransparentAONeighbor(bi, bj, axis, uAxis, vAxis, aoSlice, baseWX, baseWZ, world))
        {
            ao0 = ao1 = ao2 = ao3 = 3;
            return;
        }

        ao0 = SampleAO(bi, bj, -1, -1, axis, uAxis, vAxis, aoSlice, baseWX, baseWZ, world, ownerBlock);
        ao1 = SampleAO(bi, bj, +1, -1, axis, uAxis, vAxis, aoSlice, baseWX, baseWZ, world, ownerBlock);
        ao2 = SampleAO(bi, bj, +1, +1, axis, uAxis, vAxis, aoSlice, baseWX, baseWZ, world, ownerBlock);
        ao3 = SampleAO(bi, bj, -1, +1, axis, uAxis, vAxis, aoSlice, baseWX, baseWZ, world, ownerBlock);
    }

    private static bool HasTransparentAONeighbor(
        int bi,
        int bj,
        int axis,
        int uAxis,
        int vAxis,
        int aoSlice,
        int baseWX,
        int baseWZ,
        World.World world)
    {
        for (int ou = -1; ou <= 1; ou++)
        for (int ov = -1; ov <= 1; ov++)
        {
            if (ou == 0 && ov == 0)
                continue;

            int[] lp = new int[3];
            lp[axis] = aoSlice;
            lp[uAxis] = bi + ou;
            lp[vAxis] = bj + ov;

            int wy = lp[1];
            if (wy < 0 || wy >= Chunk.Height)
                continue;

            if (BlockRegistry.IsTransparent(world.GetBlock(baseWX + lp[0], wy, baseWZ + lp[2])))
                return true;
        }

        return false;
    }

    private static int SampleAO(
        int bi,
        int bj,
        int ou,
        int ov,
        int axis,
        int uAxis,
        int vAxis,
        int aoSlice,
        int baseWX,
        int baseWZ,
        World.World world,
        byte ownerBlock)
    {
        bool s1 = IsNeighborSolid(bi + ou, bj, axis, uAxis, vAxis, aoSlice, baseWX, baseWZ, world);
        bool s2 = IsNeighborSolid(bi, bj + ov, axis, uAxis, vAxis, aoSlice, baseWX, baseWZ, world);
        bool sc = IsNeighborSolid(bi + ou, bj + ov, axis, uAxis, vAxis, aoSlice, baseWX, baseWZ, world);
        return VertexAO(s1, s2, sc);
    }

    private static bool IsNeighborSolid(
        int gu,
        int gv,
        int axis,
        int uAxis,
        int vAxis,
        int aoSlice,
        int baseWX,
        int baseWZ,
        World.World world)
    {
        int[] lp = new int[3];
        lp[axis] = aoSlice;
        lp[uAxis] = gu;
        lp[vAxis] = gv;

        int wy = lp[1];
        if (wy < 0 || wy >= Chunk.Height)
            return false;

        int wx = baseWX + lp[0];
        int wz = baseWZ + lp[2];
        return BlockRegistry.IsSolid(world.GetBlock(wx, wy, wz));
    }

    private static int VertexAO(bool side1, bool side2, bool corner)
    {
        if (side1 && side2)
            return 0;

        return 3 - (side1 ? 1 : 0) - (side2 ? 1 : 0) - (corner ? 1 : 0);
    }

    private static void AddQuad(
        List<float> vertices,
        List<uint> indices,
        int[] v0,
        int[] du,
        int[] dv,
        int w,
        int h,
        int tileLayer,
        bool isCutout,
        bool backFace,
        byte skyLight,
        int ao0,
        int ao1,
        int ao2,
        int ao3,
        int baseWX,
        int baseWZ,
        int axis,
        float minSkyLightAmbient)
    {
        var c0 = LocalToWorld(v0[0], v0[1], v0[2], baseWX, baseWZ);
        var c1 = LocalToWorld(v0[0] + du[0], v0[1] + du[1], v0[2] + du[2], baseWX, baseWZ);
        var c2 = LocalToWorld(v0[0] + du[0] + dv[0], v0[1] + du[1] + dv[1], v0[2] + du[2] + dv[2], baseWX, baseWZ);
        var c3 = LocalToWorld(v0[0] + dv[0], v0[1] + dv[1], v0[2] + dv[2], baseWX, baseWZ);

        (float u, float v)[] uvs = axis == 0
            ? new (float, float)[] { (0f, 0f), (0f, w), (h, w), (h, 0f) }
            : new (float, float)[] { (0f, 0f), (w, 0f), (w, h), (0f, h) };

        float directional = GetDirectionalFaceLight(axis, backFace);
        float skyFactor = GetSkyLightFactor(skyLight, minSkyLightAmbient);
        float faceLight = directional * skyFactor;

        uint baseIdx = (uint)(vertices.Count / 9);

        void AddVertex((float x, float y, float z) c, (float u, float v) uv, float ao)
        {
            vertices.Add(c.x);
            vertices.Add(c.y);
            vertices.Add(c.z);
            vertices.Add(uv.u);
            vertices.Add(uv.v);
            vertices.Add(tileLayer);
            vertices.Add(ao);
            vertices.Add(faceLight);
            vertices.Add(isCutout ? 1f : 0f);
        }

        AddVertex(c0, uvs[0], ao0);
        AddVertex(c1, uvs[1], ao1);
        AddVertex(c2, uvs[2], ao2);
        AddVertex(c3, uvs[3], ao3);

        bool flipDiag = ao0 + ao2 > ao1 + ao3;

        if (!backFace)
        {
            if (!flipDiag)
            {
                indices.Add(baseIdx);
                indices.Add(baseIdx + 1);
                indices.Add(baseIdx + 2);
                indices.Add(baseIdx + 2);
                indices.Add(baseIdx + 3);
                indices.Add(baseIdx);
            }
            else
            {
                indices.Add(baseIdx);
                indices.Add(baseIdx + 1);
                indices.Add(baseIdx + 3);
                indices.Add(baseIdx + 1);
                indices.Add(baseIdx + 2);
                indices.Add(baseIdx + 3);
            }
        }
        else
        {
            if (!flipDiag)
            {
                indices.Add(baseIdx);
                indices.Add(baseIdx + 3);
                indices.Add(baseIdx + 2);
                indices.Add(baseIdx + 2);
                indices.Add(baseIdx + 1);
                indices.Add(baseIdx);
            }
            else
            {
                indices.Add(baseIdx);
                indices.Add(baseIdx + 3);
                indices.Add(baseIdx + 1);
                indices.Add(baseIdx + 1);
                indices.Add(baseIdx + 3);
                indices.Add(baseIdx + 2);
            }
        }
    }

    private static float GetDirectionalFaceLight(int axis, bool backFace) => (axis, backFace) switch
    {
        (1, false) => 1.00f,
        (1, true) => 0.40f,
        (2, false) => 0.80f,
        (2, true) => 0.60f,
        _ => 0.70f,
    };

    private static float GetSkyLightFactor(byte skyLight, float minSkyLightAmbient)
    {
        float normalized = skyLight / (float)SkyLightPropagator.MaxSkyLight;
        return minSkyLightAmbient + (1f - minSkyLightAmbient) * normalized;
    }

    private static (float x, float y, float z) LocalToWorld(int lx, int ly, int lz, int baseWX, int baseWZ)
        => (baseWX + lx, ly, baseWZ + lz);

    private static byte GetSkyLight(
        Chunk chunk,
        World.World world,
        WorldGenerator generator,
        int lx,
        int ly,
        int lz,
        int baseWX,
        int baseWZ)
    {
        if (ly < 0 || ly >= Chunk.Height)
            return 0;

        if (lx >= 0 && lx < Chunk.Width && lz >= 0 && lz < Chunk.Depth)
            return chunk.GetSkyLight(lx, ly, lz);

        int worldX = baseWX + lx;
        int worldZ = baseWZ + lz;
        int chunkX = (int)Math.Floor(worldX / (float)Chunk.Width);
        int chunkZ = (int)Math.Floor(worldZ / (float)Chunk.Depth);

        if (world.GetChunk(chunkX, chunkZ) is not null)
            return world.GetSkyLight(worldX, ly, worldZ);

        byte sampledBlock = generator.SampleBlock(worldX, ly, worldZ);
        int attenuation = BlockRegistry.Get(sampledBlock).SkyLightAttenuation;
        int estimated = SkyLightPropagator.MaxSkyLight - attenuation;
        return (byte)Math.Max(0, estimated);
    }
    private static byte GetBlock(
        Chunk chunk,
        World.World world,
        WorldGenerator generator,
        int lx,
        int ly,
        int lz,
        int baseWX,
        int baseWZ)
    {
        if (ly < 0 || ly >= Chunk.Height)
            return BlockType.Air;

        if (lx >= 0 && lx < Chunk.Width && lz >= 0 && lz < Chunk.Depth)
            return chunk.GetBlock(lx, ly, lz);

        int worldX = baseWX + lx;
        int worldZ = baseWZ + lz;
        int chunkX = (int)Math.Floor(worldX / (float)Chunk.Width);
        int chunkZ = (int)Math.Floor(worldZ / (float)Chunk.Depth);

        return world.GetChunk(chunkX, chunkZ) is not null
            ? world.GetBlock(worldX, ly, worldZ)
            : generator.SampleBlock(worldX, ly, worldZ);
    }
}


