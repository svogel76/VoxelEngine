namespace VoxelEngine.Core.Debug.Commands;

public class PosCommand : ICommand
{
    public string Name        => "pos";
    public string Description => "Zeigt aktuelle Kamera-Position";
    public string Usage       => "pos";

    public void Execute(string[] args, GameContext context)
    {
        var p = context.Camera.Position;
        context.Console.Log($"Position: ({p.X:F2}, {p.Y:F2}, {p.Z:F2})");
    }
}
