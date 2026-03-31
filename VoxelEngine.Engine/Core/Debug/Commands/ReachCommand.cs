namespace VoxelEngine.Core.Debug.Commands;

/// <summary>
/// Zeigt die aktuelle Interaktionsreichweite an. Das tatsächliche Setzen erfolgt
/// über Engine._interactionReach — dieser Befehl ist zu Informationszwecken.
/// </summary>
public class ReachCommand : ICommand
{
    public string Name        => "reach";
    public string Description => "Zeigt die Block-Interaktionsreichweite";
    public string Usage       => "reach";

    public void Execute(string[] args, GameContext context)
    {
        context.Console.Log("Interaktionsreichweite wird über EngineSettings.InteractionReach konfiguriert.");
    }
}
