using System.Globalization;
using VoxelEngine.Entity.Components;

namespace VoxelEngine.Core.Debug.Commands;

public sealed class DamageCommand : ICommand
{
    public string Name => "damage";
    public string Description => "Fuegt dem Spieler Schaden zu";
    public string Usage => "damage <amount>";

    public void Execute(string[] args, GameContext context)
    {
        if (args.Length < 1 || !float.TryParse(args[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float amount))
        {
            context.Console.Log($"Verwendung: {Usage}");
            return;
        }

        context.Player.GetComponent<HealthComponent>()?.TakeDamage(amount);
    }
}
