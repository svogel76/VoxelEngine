namespace VoxelEngine.Core.Debug.Commands;

public class HelpCommand : ICommand
{
    private readonly DebugConsole _console;

    public string Name        => "help";
    public string Description => "Listet alle Kommandos auf";
    public string Usage       => "help";

    public HelpCommand(DebugConsole console)
    {
        _console = console;
    }

    public void Execute(string[] args, GameContext context)
    {
        context.Console.Log("--- Verfügbare Kommandos ---");
        foreach (var cmd in _console.GetCommands().Values)
            context.Console.Log($"  {cmd.Name,-12} {cmd.Usage,-20} — {cmd.Description}");
    }
}
