namespace VoxelEngine.Entity.Models;

public sealed class EntityAtlasDefinition
{
    public string Path { get; }
    public int TileColumns { get; }
    public int TileRows { get; }

    public EntityAtlasDefinition(string path, int tileColumns, int tileRows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (tileColumns <= 0)
            throw new ArgumentOutOfRangeException(nameof(tileColumns));
        if (tileRows <= 0)
            throw new ArgumentOutOfRangeException(nameof(tileRows));

        Path = path;
        TileColumns = tileColumns;
        TileRows = tileRows;
    }
}
