namespace VoxelEngine.Core.Debug.Commands;

public class ReachCommand : ICommand
{
    public string Name        => "reach";
    public string Description => "Setzt die Block-Interaktionsreichweite";
    public string Usage       => "reach <blöcke>";

    public void Execute(string[] args, GameContext context)
    {
        if (args.Length < 1 || !float.TryParse(args[0], out float reach) || reach <= 0f)
        {
            context.Console.Log($"Verwendung: {Usage}  (aktuell: {context.Player.InteractionReach:F1})");
            return;
        }

        context.Player.SetInteractionReach(reach);
        context.Console.Log($"Interaktions-Reichweite gesetzt auf {reach:F1}");
    }
}
