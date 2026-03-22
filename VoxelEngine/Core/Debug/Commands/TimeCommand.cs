namespace VoxelEngine.Core.Debug.Commands;

public class TimeCommand : ICommand
{
    public string Name        => "time";
    public string Description => "Weltzeit setzen oder anzeigen";
    public string Usage       => "time <0-24> | time pause | time speed <factor>";

    public void Execute(string[] args, GameContext context)
    {
        if (args.Length == 0)
        {
            var t = context.Time;
            context.Console.Log($"Zeit: {t.Time:F2}h  Scale: {t.TimeScale}x  Paused: {t.Paused}");
            return;
        }

        switch (args[0].ToLower())
        {
            case "pause":
                context.Time.Paused = !context.Time.Paused;
                context.Console.Log($"Zeit {(context.Time.Paused ? "pausiert" : "läuft")}");
                break;

            case "speed":
                if (args.Length < 2 || !double.TryParse(args[1],
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out double speed))
                {
                    context.Console.Log($"Verwendung: time speed <factor>");
                    return;
                }
                context.Time.TimeScale = speed;
                context.Console.Log($"TimeScale: {speed}x");
                break;

            default:
                if (double.TryParse(args[0],
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out double hours))
                {
                    context.Time.SetTime(hours);
                    context.Console.Log($"Zeit gesetzt: {hours:F2}h");
                }
                else
                {
                    context.Console.Log($"Verwendung: {Usage}");
                }
                break;
        }
    }
}
