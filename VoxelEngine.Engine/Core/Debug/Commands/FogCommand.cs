namespace VoxelEngine.Core.Debug.Commands;

public class FogCommand : ICommand
{
    public string Name        => "fog";
    public string Description => "Steuert den Entfernungs-Fog";
    public string Usage       => "fog on|off | fog start <0-1> | fog end <0-1>";

    private const float DefaultStart = 0.5f;
    private const float DefaultEnd   = 0.9f;

    public void Execute(string[] args, GameContext context)
    {
        if (args.Length == 0)
        {
            context.Console.Log($"Verwendung: {Usage}");
            return;
        }

        switch (args[0].ToLower())
        {
            case "off":
                context.Renderer.SetFog(DefaultStart, 0f);
                context.Console.Log("Fog deaktiviert");
                break;

            case "on":
                context.Renderer.SetFog(DefaultStart, DefaultEnd);
                context.Console.Log($"Fog aktiviert (start={DefaultStart}, end={DefaultEnd})");
                break;

            case "start":
                if (args.Length < 2 || !float.TryParse(args[1], System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out float s) || s < 0f || s > 1f)
                {
                    context.Console.Log("fog start erwartet einen Wert zwischen 0 und 1");
                    return;
                }
                context.Renderer.SetFog(s, context.Renderer.FogEndFactor);
                context.Console.Log($"FogStart gesetzt auf {s}");
                break;

            case "end":
                if (args.Length < 2 || !float.TryParse(args[1], System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out float e) || e < 0f || e > 1f)
                {
                    context.Console.Log("fog end erwartet einen Wert zwischen 0 und 1");
                    return;
                }
                context.Renderer.SetFog(context.Renderer.FogStartFactor, e);
                context.Console.Log($"FogEnd gesetzt auf {e}");
                break;

            default:
                context.Console.Log($"Unbekanntes Subkommando. Verwendung: {Usage}");
                break;
        }
    }
}
