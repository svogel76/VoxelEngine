using System.Globalization;

namespace VoxelEngine.Entity.Models;

public static class VxmModelLoader
{
    public static IVoxelModelDefinition LoadFromFile(string path, float voxelScale = 1.0f)
        => Parse(File.ReadAllText(path), Path.GetFileName(path), voxelScale);

    public static IVoxelModelDefinition Parse(string content, string sourceName = "<memory>", float voxelScale = 1.0f)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (voxelScale <= 0f)
            throw new ArgumentOutOfRangeException(nameof(voxelScale));

        string? modelId = null;
        float voxelSize = 0.25f;
        var voxels = new List<VoxelModelVoxel>();

        var lines = content.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        for (int index = 0; index < lines.Length; index++)
        {
            string line = lines[index].Trim();
            if (line.Length == 0 || line.StartsWith('#'))
                continue;

            var tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            switch (tokens[0].ToLowerInvariant())
            {
                case "model":
                    EnsureTokenCount(tokens, 2, sourceName, index, "model <id>");
                    modelId = tokens[1];
                    break;

                case "voxelsize":
                    EnsureTokenCount(tokens, 2, sourceName, index, "voxelSize <value>");
                    voxelSize = ParseFloat(tokens[1], sourceName, index);
                    if (voxelSize <= 0f)
                        throw CreateFormatException(sourceName, index, "voxelSize must be greater than zero.");
                    break;

                case "voxel":
                    if (tokens.Length != 6 && tokens.Length != 10)
                        throw CreateFormatException(sourceName, index, "voxel format must be 'voxel x y z tileX tileY [r g b a]'.");

                    voxels.Add(new VoxelModelVoxel(
                        ParseInt(tokens[1], sourceName, index),
                        ParseInt(tokens[2], sourceName, index),
                        ParseInt(tokens[3], sourceName, index),
                        ParseInt(tokens[4], sourceName, index),
                        ParseInt(tokens[5], sourceName, index),
                        tokens.Length == 10
                            ? new VoxelTint(
                                ParseByte(tokens[6], sourceName, index),
                                ParseByte(tokens[7], sourceName, index),
                                ParseByte(tokens[8], sourceName, index),
                                ParseByte(tokens[9], sourceName, index))
                            : VoxelTint.White));
                    break;

                default:
                    throw CreateFormatException(sourceName, index, $"Unknown directive '{tokens[0]}'.");
            }
        }

        if (string.IsNullOrWhiteSpace(modelId))
            throw CreateFormatException(sourceName, 0, "Missing 'model <id>' directive.");
        if (voxels.Count == 0)
            throw CreateFormatException(sourceName, 0, "The model must contain at least one voxel.");

        return new VoxelModelDefinition(modelId, voxelSize * voxelScale, voxels);
    }

    private static void EnsureTokenCount(string[] tokens, int expectedCount, string sourceName, int lineIndex, string usage)
    {
        if (tokens.Length != expectedCount)
            throw CreateFormatException(sourceName, lineIndex, $"Invalid directive. Expected '{usage}'.");
    }

    private static int ParseInt(string value, string sourceName, int lineIndex)
    {
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
            return result;

        throw CreateFormatException(sourceName, lineIndex, $"'{value}' is not a valid integer.");
    }

    private static float ParseFloat(string value, string sourceName, int lineIndex)
    {
        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float result))
            return result;

        throw CreateFormatException(sourceName, lineIndex, $"'{value}' is not a valid number.");
    }

    private static byte ParseByte(string value, string sourceName, int lineIndex)
    {
        if (byte.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out byte result))
            return result;

        throw CreateFormatException(sourceName, lineIndex, $"'{value}' is not a valid byte value.");
    }

    private static FormatException CreateFormatException(string sourceName, int lineIndex, string message)
        => new($"{sourceName}: line {lineIndex + 1}: {message}");
}
