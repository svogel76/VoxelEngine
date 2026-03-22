using System.Numerics;

namespace VoxelEngine.Core.Debug.Commands;

public class SkyboxCommand : ICommand
{
    public string Name        => "skybox";
    public string Description => "Skybox-Farben zur Laufzeit ändern";
    public string Usage       => "skybox zenith|horizon|ground <r> <g> <b>";

    public void Execute(string[] args, GameContext context)
    {
        if (args.Length < 4 ||
            !float.TryParse(args[1], System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out float r) ||
            !float.TryParse(args[2], System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out float g) ||
            !float.TryParse(args[3], System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out float b))
        {
            context.Console.Log($"Verwendung: {Usage}");
            return;
        }

        var color  = new Vector3(r, g, b);
        var skybox = context.Renderer.Skybox;

        switch (args[0].ToLower())
        {
            case "zenith":
                skybox.ZenithColor = color;
                context.Console.Log($"Zenith: ({r:F2}, {g:F2}, {b:F2})");
                break;
            case "horizon":
                skybox.HorizonColor = color;
                context.Console.Log($"Horizon: ({r:F2}, {g:F2}, {b:F2})");
                break;
            case "ground":
                skybox.GroundColor = color;
                context.Console.Log($"Ground: ({r:F2}, {g:F2}, {b:F2})");
                break;
            default:
                context.Console.Log($"Unbekanntes Ziel '{args[0]}'. Verwendung: {Usage}");
                break;
        }
    }
}
