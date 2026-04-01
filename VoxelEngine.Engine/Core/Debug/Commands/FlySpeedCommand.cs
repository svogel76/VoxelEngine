using VoxelEngine.Entity.Components;

namespace VoxelEngine.Core.Debug.Commands;

/// <summary>
/// Setzt die Fluggeschwindigkeit des Spielers zur Laufzeit.
/// Verwendung: flyspeed [wert]
/// Default: 10.0 Blöcke/Sekunde (2× Laufgeschwindigkeit)
/// </summary>
public sealed class FlySpeedCommand : ICommand
{
    private const float DefaultSpeed = 10.0f;
    private const float MinSpeed     = 0.1f;
    private const float MaxSpeed     = 500.0f;

    private readonly InputComponent _input;

    public string Name        => "flyspeed";
    public string Description => "Setzt die Fluggeschwindigkeit des Spielers";
    public string Usage       => "flyspeed [wert]  (Default: 10.0, Min: 0.1, Max: 500)";

    public FlySpeedCommand(InputComponent input)
    {
        _input = input ?? throw new ArgumentNullException(nameof(input));
    }

    public void Execute(string[] args, GameContext context)
    {
        if (args.Length == 0)
        {
            context.Console.Log($"Fluggeschwindigkeit: {_input.FlySpeed:F1} Blöcke/s  (Default: {DefaultSpeed})");
            return;
        }

        if (args.Length == 1 && args[0].Equals("reset", StringComparison.OrdinalIgnoreCase))
        {
            _input.FlySpeed = DefaultSpeed;
            context.Console.Log($"Fluggeschwindigkeit zurückgesetzt auf {DefaultSpeed:F1} Blöcke/s.");
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

        _input.FlySpeed = value;
        context.Console.Log($"Fluggeschwindigkeit gesetzt: {value:F1} Blöcke/s");
    }
}
