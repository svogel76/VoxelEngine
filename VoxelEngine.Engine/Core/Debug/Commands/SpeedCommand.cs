using VoxelEngine.Entity.Components;

namespace VoxelEngine.Core.Debug.Commands;

/// <summary>
/// Setzt die Laufgeschwindigkeit des Spielers zur Laufzeit.
/// Verwendung: speed [wert]
/// Default: 5.0 Blöcke/Sekunde
/// </summary>
public sealed class SpeedCommand : ICommand
{
    private const float DefaultSpeed = 5.0f;
    private const float MinSpeed     = 0.1f;
    private const float MaxSpeed     = 200.0f;

    private readonly InputComponent _input;

    public string Name        => "speed";
    public string Description => "Setzt die Laufgeschwindigkeit des Spielers";
    public string Usage       => "speed [wert]  (Default: 5.0, Min: 0.1, Max: 200)";

    public SpeedCommand(InputComponent input)
    {
        _input = input ?? throw new ArgumentNullException(nameof(input));
    }

    public void Execute(string[] args, GameContext context)
    {
        if (args.Length == 0)
        {
            context.Console.Log($"Laufgeschwindigkeit: {_input.WalkSpeed:F1} Blöcke/s  (Default: {DefaultSpeed})");
            return;
        }

        if (args.Length == 1 && args[0].Equals("reset", StringComparison.OrdinalIgnoreCase))
        {
            _input.WalkSpeed = DefaultSpeed;
            context.Console.Log($"Laufgeschwindigkeit zurückgesetzt auf {DefaultSpeed:F1} Blöcke/s.");
            return;
        }

        if (args.Length != 1 || !float.TryParse(args[0], System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out float value))
        {
            context.Console.Log($"Ungültiger Wert. Verwendung: {Usage}");
            return;
        }

        if (value < MinSpeed || value > MaxSpeed)
        {
            context.Console.Log($"Wert außerhalb des erlaubten Bereichs ({MinSpeed}–{MaxSpeed}). Verwendung: {Usage}");
            return;
        }

        _input.WalkSpeed = value;
        context.Console.Log($"Laufgeschwindigkeit gesetzt: {value:F1} Blöcke/s");
    }
}
