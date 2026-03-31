using VoxelEngine.Entity.Components;

namespace VoxelEngine.Core.Debug.Commands;

public class FlyCommand : ICommand
{
    public string Name        => "fly";
    public string Description => "Schaltet den Fly-Modus um oder setzt ihn explizit";
    public string Usage       => "fly [on|off]";

    public void Execute(string[] args, GameContext context)
    {
        var phys = context.Player.GetComponent<PhysicsComponent>();
        if (phys is null)
        {
            context.Console.Log("Kein PhysicsComponent am Spieler.");
            return;
        }

        bool enabled;
        if (args.Length == 0)
            enabled = !phys.FlyMode;
        else if (args.Length == 1 && args[0].Equals("on", StringComparison.OrdinalIgnoreCase))
            enabled = true;
        else if (args.Length == 1 && args[0].Equals("off", StringComparison.OrdinalIgnoreCase))
            enabled = false;
        else
        {
            context.Console.Log($"Verwendung: {Usage}");
            return;
        }

        phys.SetFlyMode(enabled);
        if (!enabled) phys.SyncPhysics(context.Player);
        context.Console.Log($"Fly-Modus: {(enabled ? "AN" : "AUS")}");
    }
}
