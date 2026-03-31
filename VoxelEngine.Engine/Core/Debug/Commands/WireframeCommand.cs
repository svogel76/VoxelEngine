namespace VoxelEngine.Core.Debug.Commands;

public class WireframeCommand : ICommand
{
    public string Name        => "wireframe";
    public string Description => "Wireframe-Rendering umschalten";
    public string Usage       => "wireframe";

    public void Execute(string[] args, GameContext context)
    {
        context.Renderer.IsWireframe = !context.Renderer.IsWireframe;
        context.Console.Log($"Wireframe: {(context.Renderer.IsWireframe ? "ein" : "aus")}");
    }
}
