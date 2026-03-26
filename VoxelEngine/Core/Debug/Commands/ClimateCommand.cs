using VoxelEngine.World;

namespace VoxelEngine.Core.Debug.Commands;

public class ClimateCommand : ICommand
{
    public string Name        => "climate";
    public string Description => "Zeigt Klima-Informationen an der Spielerposition";
    public string Usage       => "climate info";

    public void Execute(string[] args, GameContext context)
    {
        if (args.Length != 1 || !args[0].Equals("info", StringComparison.OrdinalIgnoreCase))
        {
            context.Console.Log($"Verwendung: {Usage}");
            return;
        }

        int worldX = (int)MathF.Floor(context.Player.Position.X);
        int worldZ = (int)MathF.Floor(context.Player.Position.Z);
        ClimateSample sample = context.Generator.SampleClimate(worldX, worldZ);

        context.Console.Log(
            $"Klima: {sample.PrimaryZone.Name}  Temp: {sample.Temperature:F2}  Feuchtigkeit: {sample.Humidity:F2}");

        if (sample.TransitionFactor > 0.05f)
        {
            context.Console.Log(
                $"Blend zu {sample.SecondaryZone.Name}: {sample.TransitionFactor:P0}  Höhe: {sample.SurfaceHeight}");
        }
    }
}
