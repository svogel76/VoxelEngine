namespace VoxelEngine.Core.Debug.Commands;

public class RenderDistanceCommand : ICommand
{
    public string Name        => "renderdistance";
    public string Description => "Ändert die Sichtweite";
    public string Usage       => "renderdistance <radius>";

    public void Execute(string[] args, GameContext context)
    {
        if (args.Length < 1 || !int.TryParse(args[0], out int radius) || radius < 1)
        {
            context.Console.Log($"Verwendung: {Usage}  (aktuell: {context.ChunkManager.RenderDistance})");
            return;
        }

        context.ChunkManager.RenderDistance = radius;
        context.Console.Log($"RenderDistance gesetzt auf {radius}");
    }
}
