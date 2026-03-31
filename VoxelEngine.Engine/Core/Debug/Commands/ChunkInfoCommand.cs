namespace VoxelEngine.Core.Debug.Commands;

public class ChunkInfoCommand : ICommand
{
    public string Name        => "chunk";
    public string Description => "Zeigt Chunk-Informationen";
    public string Usage       => "chunk info <x> <z>";

    public void Execute(string[] args, GameContext context)
    {
        if (args.Length < 3 || args[0] != "info")
        {
            context.Console.Log($"Verwendung: {Usage}");
            return;
        }

        if (!int.TryParse(args[1], out int x) ||
            !int.TryParse(args[2], out int z))
        {
            context.Console.Log("Fehler: x, z müssen ganzzahlige Chunk-Koordinaten sein.");
            return;
        }

        var chunk = context.World.GetChunk(x, z);
        context.Console.Log($"Gesamt geladene Chunks: {context.World.LoadedChunkCount}");

        if (chunk is null)
        {
            context.Console.Log($"Chunk ({x}, {z}) ist nicht geladen.");
            return;
        }

        context.Console.Log($"Chunk ({x}, {z}) — Position: {chunk.ChunkPosition}");
    }
}
