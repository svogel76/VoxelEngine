using Silk.NET.Maths;
using VoxelEngine.World;

namespace VoxelEngine.Core.Debug.Commands;

public class TeleportCommand : ICommand
{
    public string Name        => "tp";
    public string Description => "Teleportiert den Spieler zur Ziel-Augposition";
    public string Usage       => "tp <x> <y> <z>";

    public void Execute(string[] args, GameContext context)
    {
        if (args.Length < 3)
        {
            context.Console.Log($"Verwendung: {Usage}");
            return;
        }

        if (!float.TryParse(args[0], out float x) ||
            !float.TryParse(args[1], out float y) ||
            !float.TryParse(args[2], out float z))
        {
            context.Console.Log("Fehler: x, y, z müssen Zahlen sein.");
            return;
        }

        context.Player.Teleport(new System.Numerics.Vector3(x, y - Player.EyeHeight, z));
        context.Camera.Position = new Vector3D<float>(x, y, z);
        context.Console.Log($"Teleportiert nach ({x:F2}, {y:F2}, {z:F2})");
    }
}
