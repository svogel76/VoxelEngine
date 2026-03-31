namespace VoxelEngine.Core.Debug.Commands;

public class FlyCommand : ICommand
{
    public string Name        => "fly";
    public string Description => "Schaltet den Fly-Modus um oder setzt ihn explizit";
    public string Usage       => "fly [on|off]";

    public void Execute(string[] args, GameContext context)
    {
        bool enabled;

        if (args.Length == 0)
        {
            enabled = !context.Player.FlyMode;
        }
        else if (args.Length == 1 && args[0].Equals("on", StringComparison.OrdinalIgnoreCase))
        {
            enabled = true;
        }
        else if (args.Length == 1 && args[0].Equals("off", StringComparison.OrdinalIgnoreCase))
        {
            enabled = false;
        }
        else
        {
            context.Console.Log($"Verwendung: {Usage}");
            return;
        }

        context.Player.SetFlyMode(enabled);
        if (!enabled)
            context.Player.SyncPhysics(context.World);
        context.Console.Log($"Fly-Modus: {(enabled ? "AN" : "AUS")}");
    }
}
