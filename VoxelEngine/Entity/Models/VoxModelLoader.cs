using System.Buffers.Binary;

namespace VoxelEngine.Entity.Models;

public static class VoxModelLoader
{
    private const int SupportedVersion = 150;

    public static IVoxelModelDefinition LoadFromFile(string path, float voxelScale = 1.0f)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return Parse(File.ReadAllBytes(path), Path.GetFileNameWithoutExtension(path), voxelScale);
    }

    public static IVoxelModelDefinition Parse(ReadOnlySpan<byte> data, string modelId, float voxelScale = 1.0f)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelId);
        if (voxelScale <= 0f)
            throw new ArgumentOutOfRangeException(nameof(voxelScale));

        var reader = new VoxBinaryReader(data);

        if (reader.ReadFourCC() != "VOX ")
            throw new FormatException("Invalid VOX header. Expected 'VOX '.");

        int version = reader.ReadInt32();
        if (version != SupportedVersion)
            throw new FormatException($"Unsupported VOX version {version}. Expected {SupportedVersion}.");

        string mainId = reader.ReadFourCC();
        if (mainId != "MAIN")
            throw new FormatException("Invalid VOX root chunk. Expected 'MAIN'.");

        int mainContentSize = reader.ReadInt32();
        int mainChildrenSize = reader.ReadInt32();
        reader.Skip(mainContentSize);

        long mainChildrenEnd = reader.Position + mainChildrenSize;

        VoxModelSize? size = null;
        var voxels = new List<VoxVoxel>();
        uint[] palette = CreateDefaultPalette();
        int modelCount = 0;

        while (reader.Position < mainChildrenEnd)
        {
            string chunkId = reader.ReadFourCC();
            int contentSize = reader.ReadInt32();
            int childrenSize = reader.ReadInt32();
            long chunkContentStart = reader.Position;
            long chunkEnd = chunkContentStart + contentSize + childrenSize;

            switch (chunkId)
            {
                case "PACK":
                    modelCount = reader.ReadInt32();
                    break;

                case "SIZE":
                    EnsureNoChildren(chunkId, childrenSize);
                    size = new VoxModelSize(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
                    break;

                case "XYZI":
                    EnsureNoChildren(chunkId, childrenSize);
                    if (size is null)
                        throw new FormatException("Encountered XYZI chunk before SIZE chunk.");

                    if (voxels.Count > 0)
                        throw new NotSupportedException("VOX files with multiple models are not supported yet.");

                    int voxelCount = reader.ReadInt32();
                    for (int i = 0; i < voxelCount; i++)
                    {
                        voxels.Add(new VoxVoxel(
                            reader.ReadByte(),
                            reader.ReadByte(),
                            reader.ReadByte(),
                            reader.ReadByte()));
                    }
                    break;

                case "RGBA":
                    EnsureNoChildren(chunkId, childrenSize);
                    palette = ReadPalette(reader);
                    break;

                default:
                    break;
            }

            reader.Seek(chunkEnd);
        }

        if (modelCount > 1)
            throw new NotSupportedException("VOX files with PACK > 1 are not supported yet.");

        if (size is null)
            throw new FormatException("Missing SIZE chunk.");
        if (voxels.Count == 0)
            throw new FormatException("Missing XYZI chunk or voxel data.");

        var modelVoxels = new List<VoxelModelVoxel>(voxels.Count);
        foreach (var voxel in voxels)
        {
            ValidateVoxelBounds(voxel, size.Value);

            if (voxel.ColorIndex == 0)
                throw new FormatException("VOX color index 0 is reserved and cannot be used by XYZI.");

            uint rgba = palette[voxel.ColorIndex];
            byte r = (byte)(rgba & 0xFF);
            byte g = (byte)((rgba >> 8) & 0xFF);
            byte b = (byte)((rgba >> 16) & 0xFF);
            byte a = (byte)((rgba >> 24) & 0xFF);

            modelVoxels.Add(new VoxelModelVoxel(
                voxel.X,
                voxel.Y,
                voxel.Z,
                0,
                0,
                new VoxelTint(r, g, b, a)));
        }

        return new VoxelModelDefinition(modelId, voxelScale, modelVoxels);
    }

    private static void EnsureNoChildren(string chunkId, int childrenSize)
    {
        if (childrenSize != 0)
            throw new FormatException($"Chunk '{chunkId}' is expected to have no child chunks.");
    }

    private static void ValidateVoxelBounds(VoxVoxel voxel, VoxModelSize size)
    {
        if (voxel.X >= size.X || voxel.Y >= size.Y || voxel.Z >= size.Z)
            throw new FormatException("A voxel coordinate is outside the SIZE bounds.");
    }

    private static uint[] ReadPalette(VoxBinaryReader reader)
    {
        var palette = new uint[256];
        for (int i = 1; i < 256; i++)
            palette[i] = reader.ReadUInt32();

        return palette;
    }

    private static uint[] CreateDefaultPalette()
        => new uint[]
        {
            0x00000000, 0xffffffff, 0xffccffff, 0xff99ffff, 0xff66ffff, 0xff33ffff, 0xff00ffff, 0xffffccff,
            0xffccccff, 0xff99ccff, 0xff66ccff, 0xff33ccff, 0xff00ccff, 0xffff99ff, 0xffcc99ff, 0xff9999ff,
            0xff6699ff, 0xff3399ff, 0xff0099ff, 0xffff66ff, 0xffcc66ff, 0xff9966ff, 0xff6666ff, 0xff3366ff,
            0xff0066ff, 0xffff33ff, 0xffcc33ff, 0xff9933ff, 0xff6633ff, 0xff3333ff, 0xff0033ff, 0xffff00ff,
            0xffcc00ff, 0xff9900ff, 0xff6600ff, 0xff3300ff, 0xff0000ff, 0xffffffcc, 0xffccffcc, 0xff99ffcc,
            0xff66ffcc, 0xff33ffcc, 0xff00ffcc, 0xffffcccc, 0xffcccccc, 0xff99cccc, 0xff66cccc, 0xff33cccc,
            0xff00cccc, 0xffff99cc, 0xffcc99cc, 0xff9999cc, 0xff6699cc, 0xff3399cc, 0xff0099cc, 0xffff66cc,
            0xffcc66cc, 0xff9966cc, 0xff6666cc, 0xff3366cc, 0xff0066cc, 0xffff33cc, 0xffcc33cc, 0xff9933cc,
            0xff6633cc, 0xff3333cc, 0xff0033cc, 0xffff00cc, 0xffcc00cc, 0xff9900cc, 0xff6600cc, 0xff3300cc,
            0xff0000cc, 0xffffff99, 0xffccff99, 0xff99ff99, 0xff66ff99, 0xff33ff99, 0xff00ff99, 0xffffcc99,
            0xffcccc99, 0xff99cc99, 0xff66cc99, 0xff33cc99, 0xff00cc99, 0xffff9999, 0xffcc9999, 0xff999999,
            0xff669999, 0xff339999, 0xff009999, 0xffff6699, 0xffcc6699, 0xff996699, 0xff666699, 0xff336699,
            0xff006699, 0xffff3399, 0xffcc3399, 0xff993399, 0xff663399, 0xff333399, 0xff003399, 0xffff0099,
            0xffcc0099, 0xff990099, 0xff660099, 0xff330099, 0xff000099, 0xffffff66, 0xffccff66, 0xff99ff66,
            0xff66ff66, 0xff33ff66, 0xff00ff66, 0xffffcc66, 0xffcccc66, 0xff99cc66, 0xff66cc66, 0xff33cc66,
            0xff00cc66, 0xffff9966, 0xffcc9966, 0xff999966, 0xff669966, 0xff339966, 0xff009966, 0xffff6666,
            0xffcc6666, 0xff996666, 0xff666666, 0xff336666, 0xff006666, 0xffff3366, 0xffcc3366, 0xff993366,
            0xff663366, 0xff333366, 0xff003366, 0xffff0066, 0xffcc0066, 0xff990066, 0xff660066, 0xff330066,
            0xff000066, 0xffffff33, 0xffccff33, 0xff99ff33, 0xff66ff33, 0xff33ff33, 0xff00ff33, 0xffffcc33,
            0xffcccc33, 0xff99cc33, 0xff66cc33, 0xff33cc33, 0xff00cc33, 0xffff9933, 0xffcc9933, 0xff999933,
            0xff669933, 0xff339933, 0xff009933, 0xffff6633, 0xffcc6633, 0xff996633, 0xff666633, 0xff336633,
            0xff006633, 0xffff3333, 0xffcc3333, 0xff993333, 0xff663333, 0xff333333, 0xff003333, 0xffff0033,
            0xffcc0033, 0xff990033, 0xff660033, 0xff330033, 0xff000033, 0xffffff00, 0xffccff00, 0xff99ff00,
            0xff66ff00, 0xff33ff00, 0xff00ff00, 0xffffcc00, 0xffcccc00, 0xff99cc00, 0xff66cc00, 0xff33cc00,
            0xff00cc00, 0xffff9900, 0xffcc9900, 0xff999900, 0xff669900, 0xff339900, 0xff009900, 0xffff6600,
            0xffcc6600, 0xff996600, 0xff666600, 0xff336600, 0xff006600, 0xffff3300, 0xffcc3300, 0xff993300,
            0xff663300, 0xff333300, 0xff003300, 0xffff0000, 0xffcc0000, 0xff990000, 0xff660000, 0xff330000,
            0xff0000ee, 0xff0000dd, 0xff0000bb, 0xff0000aa, 0xff000088, 0xff000077, 0xff000055, 0xff000044,
            0xff000022, 0xff000011, 0xff00ee00, 0xff00dd00, 0xff00bb00, 0xff00aa00, 0xff008800, 0xff007700,
            0xff005500, 0xff004400, 0xff002200, 0xff001100, 0xffee0000, 0xffdd0000, 0xffbb0000, 0xffaa0000,
            0xff880000, 0xff770000, 0xff550000, 0xff440000, 0xff220000, 0xff110000, 0xffeeeeee, 0xffdddddd,
            0xffbbbbbb, 0xffaaaaaa, 0xff888888, 0xff777777, 0xff555555, 0xff444444, 0xff222222, 0xff111111
        };

    private readonly record struct VoxModelSize(int X, int Y, int Z);
    private readonly record struct VoxVoxel(byte X, byte Y, byte Z, byte ColorIndex);

    private ref struct VoxBinaryReader
    {
        private readonly ReadOnlySpan<byte> _data;

        public int Position { get; private set; }

        public VoxBinaryReader(ReadOnlySpan<byte> data)
        {
            _data = data;
            Position = 0;
        }

        public string ReadFourCC()
        {
            EnsureAvailable(4);
            string value = System.Text.Encoding.ASCII.GetString(_data.Slice(Position, 4));
            Position += 4;
            return value;
        }

        public int ReadInt32()
        {
            EnsureAvailable(4);
            int value = BinaryPrimitives.ReadInt32LittleEndian(_data.Slice(Position, 4));
            Position += 4;
            return value;
        }

        public uint ReadUInt32()
        {
            EnsureAvailable(4);
            uint value = BinaryPrimitives.ReadUInt32LittleEndian(_data.Slice(Position, 4));
            Position += 4;
            return value;
        }

        public byte ReadByte()
        {
            EnsureAvailable(1);
            return _data[Position++];
        }

        public void Skip(int byteCount)
        {
            EnsureAvailable(byteCount);
            Position += byteCount;
        }

        public void Seek(long absolutePosition)
        {
            if (absolutePosition < 0 || absolutePosition > _data.Length)
                throw new FormatException("VOX chunk exceeds available file data.");

            Position = (int)absolutePosition;
        }

        private void EnsureAvailable(int byteCount)
        {
            if (Position + byteCount > _data.Length)
                throw new FormatException("Unexpected end of VOX file.");
        }
    }
}
