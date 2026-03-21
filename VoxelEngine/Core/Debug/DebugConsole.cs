namespace VoxelEngine.Core.Debug;

public class DebugConsole : IDisposable
{
    private readonly GameContext _context;
    private readonly Dictionary<string, ICommand> _commands = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _history = new();
    private readonly List<string> _output  = new();
    private bool _isOpen = false;

    public bool IsOpen => _isOpen;

    public DebugConsole(GameContext context)
    {
        _context = context;
    }

    public void Register(ICommand command)
        => _commands[command.Name] = command;

    public void Toggle()
        => _isOpen = !_isOpen;

    public void Execute(string input)
    {
        input = input.Trim();
        if (string.IsNullOrEmpty(input))
            return;

        _history.Add(input);

        var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var name  = parts[0];
        var args  = parts.Length > 1 ? parts[1..] : Array.Empty<string>();

        try
        {
            if (_commands.TryGetValue(name, out var command))
            {
                command.Execute(args, _context);
                Log($"> {input}");
            }
            else
            {
                Log($"Unbekanntes Kommando: '{name}'. Tippe 'help' für eine Übersicht.");
            }
        }
        catch (Exception ex)
        {
            Log($"Fehler bei '{name}': {ex.Message}");
        }
    }

    public void Log(string message)
    {
        _output.Add(message);
        if (_output.Count > 50)
            _output.RemoveAt(0);
    }

    public IReadOnlyList<string>               GetOutput()   => _output;
    public IReadOnlyList<string>               GetHistory()  => _history;
    public IReadOnlyDictionary<string, ICommand> GetCommands() => _commands;

    public void Dispose() { }
}
