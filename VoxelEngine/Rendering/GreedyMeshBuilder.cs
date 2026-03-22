using VoxelEngine.World;

namespace VoxelEngine.Rendering;

/// <summary>
/// Greedy meshing: merges adjacent same-type faces into large quads.
/// Vertex format: (x, y, z, u, v, tileLayer, ao) — 7 floats per vertex.
/// UV coordinates range 0..w × 0..h so the texture tiles across merged rects.
/// AO: 0 = darkest (30 % brightness), 3 = brightest (100 %).
/// </summary>
public static class GreedyMeshBuilder
{
    private readonly struct MaskEntry
    {
        public readonly byte Block;
        public readonly int  TileLayer;
        public readonly bool IsBackFace;
        public readonly int  AO0, AO1, AO2, AO3;

        public bool IsEmpty => Block == BlockType.Air;

        public MaskEntry(byte block, int tileLayer, bool isBackFace,
                         int ao0, int ao1, int ao2, int ao3)
        {
            Block      = block;
            TileLayer  = tileLayer;
            IsBackFace = isBackFace;
            AO0 = ao0; AO1 = ao1; AO2 = ao2; AO3 = ao3;
        }

        public bool Matches(MaskEntry o) =>
            Block == o.Block && TileLayer == o.TileLayer && IsBackFace == o.IsBackFace &&
            AO0 == o.AO0 && AO1 == o.AO1 && AO2 == o.AO2 && AO3 == o.AO3;
    }

    private static readonly int[] Dims = { Chunk.Width, Chunk.Height, Chunk.Depth };

    public static (float[] vertices, uint[] indices) Build(Chunk chunk, World.World world)
    {
        var verts = new List<float>();
        var inds  = new List<uint>();

        int baseWX = chunk.ChunkPosition.X * Chunk.Width;
        int baseWZ = chunk.ChunkPosition.Z * Chunk.Depth;

        for (int axis = 0; axis < 3; axis++)
        {
            int uAxis = (axis + 1) % 3;
            int vAxis = (axis + 2) % 3;
            int uSize = Dims[uAxis];
            int vSize = Dims[vAxis];

            int[] pos = new int[3];
            int[] q   = new int[3];
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

            for (pos[axis] = -1; pos[axis] < Dims[axis]; )
            {
                // ── Build mask ────────────────────────────────────────────────
                int n = 0;
                for (pos[vAxis] = 0; pos[vAxis] < vSize; pos[vAxis]++)
                for (pos[uAxis] = 0; pos[uAxis] < uSize; pos[uAxis]++, n++)
                {
                    byte blockA = GetBlock(chunk, world, pos[0], pos[1], pos[2], baseWX, baseWZ);
                    byte blockB = GetBlock(chunk, world,
                                           pos[0] + q[0], pos[1] + q[1], pos[2] + q[2],
                                           baseWX, baseWZ);

                    bool solidA = blockA != BlockType.Air;
                    bool solidB = blockB != BlockType.Air;

                    if (solidA == solidB)
                    {
                        mask[n] = default;
                    }
                    else if (solidA)
                    {
                        // Forward face: solid block is blockA at pos[axis].
                        // Only generate if blockA is within this chunk — otherwise the
                        // neighbouring chunk already owns and generates this face.
                        if (pos[axis] >= 0)
                        {
                            int aoSlice = pos[axis] + 1;
                            ComputeAOs(pos[uAxis], pos[vAxis], axis, uAxis, vAxis,
                                       aoSlice, baseWX, baseWZ, world,
                                       out int ao0, out int ao1, out int ao2, out int ao3);
                            mask[n] = new MaskEntry(blockA,
                                                    BlockTextures.GetTileIndex(blockA, forwardDir),
                                                    false, ao0, ao1, ao2, ao3);
                        }
                        // else: blockA is in the neighbouring chunk → skip, it generates the face
                    }
                    else
                    {
                        // Back face: solid block is blockB at pos[axis]+1.
                        // Only generate if blockB is within this chunk.
                        if (pos[axis] + 1 < Dims[axis])
                        {
                            int aoSlice = pos[axis];
                            ComputeAOs(pos[uAxis], pos[vAxis], axis, uAxis, vAxis,
                                       aoSlice, baseWX, baseWZ, world,
                                       out int ao0, out int ao1, out int ao2, out int ao3);
                            mask[n] = new MaskEntry(blockB,
                                                    BlockTextures.GetTileIndex(blockB, backDir),
                                                    true, ao0, ao1, ao2, ao3);
                        }
                        // else: blockB is in the neighbouring chunk → skip
                    }
                }

                pos[axis]++;  // advance to face plane

                // ── Greedy merge ──────────────────────────────────────────────
                for (int j = 0; j < vSize; j++)
                for (int i = 0; i < uSize; )
                {
                    var entry = mask[j * uSize + i];
                    if (entry.IsEmpty) { i++; continue; }

                    // Width (u)
                    int w = 1;
                    while (i + w < uSize && entry.Matches(mask[j * uSize + i + w]))
                        w++;

                    // Height (v)
                    int  h    = 1;
                    bool done = false;
                    while (!done && j + h < vSize)
                    {
                        for (int k = 0; k < w; k++)
                        {
                            if (!entry.Matches(mask[(j + h) * uSize + i + k]))
                            { done = true; break; }
                        }
                        if (!done) h++;
                    }

                    // ── Emit quad ─────────────────────────────────────────────
                    int[] v0 = new int[3];
                    v0[axis]  = pos[axis];
                    v0[uAxis] = i;
                    v0[vAxis] = j;

                    int[] du = new int[3]; du[uAxis] = w;
                    int[] dv = new int[3]; dv[vAxis] = h;

                    AddQuad(verts, inds, v0, du, dv, w, h,
                            entry.TileLayer, entry.IsBackFace,
                            entry.AO0, entry.AO1, entry.AO2, entry.AO3,
                            baseWX, baseWZ, axis);

                    // ── Clear used cells ──────────────────────────────────────
                    for (int jj = j; jj < j + h; jj++)
                    for (int ii = i; ii < i + w; ii++)
                        mask[jj * uSize + ii] = default;

                    i += w;
                }
            }
        }

        if (verts.Count == 0)
            return (Array.Empty<float>(), Array.Empty<uint>());

        return (verts.ToArray(), inds.ToArray());
    }

    // ─── AO Helpers ──────────────────────────────────────────────────────────

    /// <summary>
    /// Computes the 4 per-corner AO values for a mask cell at (bi, bj) in (uAxis, vAxis) face coords.
    /// aoSlice is the axis-coordinate on the viewer's side of the face (where AO blocks are sampled).
    /// Corner order: c0=(−u,−v), c1=(+u,−v), c2=(+u,+v), c3=(−u,+v) — matches AddQuad vertex order.
    /// </summary>
    private static void ComputeAOs(
        int bi, int bj, int axis, int uAxis, int vAxis,
        int aoSlice, int baseWX, int baseWZ, World.World world,
        out int ao0, out int ao1, out int ao2, out int ao3)
    {
        ao0 = SampleAO(bi, bj, -1, -1, axis, uAxis, vAxis, aoSlice, baseWX, baseWZ, world);
        ao1 = SampleAO(bi, bj, +1, -1, axis, uAxis, vAxis, aoSlice, baseWX, baseWZ, world);
        ao2 = SampleAO(bi, bj, +1, +1, axis, uAxis, vAxis, aoSlice, baseWX, baseWZ, world);
        ao3 = SampleAO(bi, bj, -1, +1, axis, uAxis, vAxis, aoSlice, baseWX, baseWZ, world);
    }

    /// <summary>
    /// Computes AO for one corner of block (bi, bj) in face-plane coords.
    /// (ou, ov) = outward direction from the block toward the corner (±1, ±1).
    /// S1 = neighbour along u, S2 = neighbour along v, SC = diagonal neighbour.
    /// </summary>
    private static int SampleAO(
        int bi, int bj, int ou, int ov,
        int axis, int uAxis, int vAxis,
        int aoSlice, int baseWX, int baseWZ, World.World world)
    {
        bool s1 = IsNeighborSolid(bi + ou, bj,      axis, uAxis, vAxis, aoSlice, baseWX, baseWZ, world);
        bool s2 = IsNeighborSolid(bi,      bj + ov, axis, uAxis, vAxis, aoSlice, baseWX, baseWZ, world);
        bool sc = IsNeighborSolid(bi + ou, bj + ov, axis, uAxis, vAxis, aoSlice, baseWX, baseWZ, world);
        return VertexAO(s1, s2, sc);
    }

    private static bool IsNeighborSolid(
        int gu, int gv,
        int axis, int uAxis, int vAxis,
        int aoSlice, int baseWX, int baseWZ, World.World world)
    {
        int[] lp = new int[3];
        lp[axis]  = aoSlice;
        lp[uAxis] = gu;
        lp[vAxis] = gv;

        int wy = lp[1];
        if (wy < 0 || wy >= Chunk.Height) return false;

        int wx = baseWX + lp[0];
        int wz = baseWZ + lp[2];
        return world.GetBlock(wx, wy, wz) != BlockType.Air;
    }

    private static int VertexAO(bool side1, bool side2, bool corner)
    {
        if (side1 && side2) return 0;
        return 3 - (side1 ? 1 : 0) - (side2 ? 1 : 0) - (corner ? 1 : 0);
    }

    // ─── Quad emission ───────────────────────────────────────────────────────

    private static void AddQuad(
        List<float> vertices, List<uint> indices,
        int[] v0, int[] du, int[] dv,
        int w, int h, int tileLayer, bool backFace,
        int ao0, int ao1, int ao2, int ao3,
        int baseWX, int baseWZ, int axis)
    {
        var c0 = LocalToWorld(v0[0],                    v0[1],                    v0[2],                    baseWX, baseWZ);
        var c1 = LocalToWorld(v0[0] + du[0],            v0[1] + du[1],            v0[2] + du[2],            baseWX, baseWZ);
        var c2 = LocalToWorld(v0[0] + du[0] + dv[0],   v0[1] + du[1] + dv[1],   v0[2] + du[2] + dv[2],   baseWX, baseWZ);
        var c3 = LocalToWorld(v0[0] + dv[0],            v0[1] + dv[1],            v0[2] + dv[2],            baseWX, baseWZ);

        // Für axis=0 (X-Flächen): UV tauschen damit V auf Y (vertikal) mappt
        (float u, float v)[] uvs = axis == 0
            ? new (float, float)[] { (0f, 0f), (0f, w), (h, w), (h, 0f) }
            : new (float, float)[] { (0f, 0f), (w, 0f), (w, h), (0f, h) };

        uint baseIdx = (uint)(vertices.Count / 7);

        void AddVertex((float x, float y, float z) c, (float u, float v) uv, float ao)
        {
            vertices.Add(c.x); vertices.Add(c.y); vertices.Add(c.z);
            vertices.Add(uv.u); vertices.Add(uv.v);
            vertices.Add(tileLayer);
            vertices.Add(ao);
        }

        AddVertex(c0, uvs[0], ao0);
        AddVertex(c1, uvs[1], ao1);
        AddVertex(c2, uvs[2], ao2);
        AddVertex(c3, uvs[3], ao3);

        // Flip the quad diagonal so it always passes through the two darker corners.
        // Without this, the interpolation seam between the two triangles causes
        // visible striping artefacts when AO values differ along one diagonal.
        bool flipDiag = ao0 + ao2 > ao1 + ao3;

        if (!backFace)
        {
            if (!flipDiag)
            {
                // Default 0→2 diagonal
                indices.Add(baseIdx);     indices.Add(baseIdx + 1); indices.Add(baseIdx + 2);
                indices.Add(baseIdx + 2); indices.Add(baseIdx + 3); indices.Add(baseIdx);
            }
            else
            {
                // Flipped 1→3 diagonal
                indices.Add(baseIdx);     indices.Add(baseIdx + 1); indices.Add(baseIdx + 3);
                indices.Add(baseIdx + 1); indices.Add(baseIdx + 2); indices.Add(baseIdx + 3);
            }
        }
        else
        {
            if (!flipDiag)
            {
                // Default 0→2 diagonal, reversed winding
                indices.Add(baseIdx);     indices.Add(baseIdx + 3); indices.Add(baseIdx + 2);
                indices.Add(baseIdx + 2); indices.Add(baseIdx + 1); indices.Add(baseIdx);
            }
            else
            {
                // Flipped 1→3 diagonal, reversed winding
                indices.Add(baseIdx);     indices.Add(baseIdx + 3); indices.Add(baseIdx + 1);
                indices.Add(baseIdx + 1); indices.Add(baseIdx + 3); indices.Add(baseIdx + 2);
            }
        }
    }

    private static (float x, float y, float z) LocalToWorld(int lx, int ly, int lz, int baseWX, int baseWZ)
        => (baseWX + lx, ly, baseWZ + lz);

    private static byte GetBlock(Chunk chunk, World.World world,
                                  int lx, int ly, int lz,
                                  int baseWX, int baseWZ)
    {
        if (ly < 0 || ly >= Chunk.Height) return BlockType.Air;
        if (lx >= 0 && lx < Chunk.Width && lz >= 0 && lz < Chunk.Depth)
            return chunk.GetBlock(lx, ly, lz);
        return world.GetBlock(baseWX + lx, ly, baseWZ + lz);
    }
}
