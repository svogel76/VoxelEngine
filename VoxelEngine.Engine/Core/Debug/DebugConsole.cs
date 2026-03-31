namespace VoxelEngine.Core.Debug;

public class DebugConsole : IDisposable
{
    private readonly GameContext _context;
    private readonly Dictionary<string, ICommand> _commands = new(StringComparer.OrdinalIgnoreCase);

    // Eingabe-History: enthält ausgeführte Befehle (älteste zuerst)
    private readonly List<string> _inputHistory = new();

    // Ausgabe-Log
    private readonly List<string> _output = new();

    private bool _isOpen = false;

    // History-Navigation: -1 = kein aktiver History-Eintrag (leeres / aktuelles Input)
    private int    _historyIndex   = -1;
    // Puffer für den aktuell tippten Text, bevor in der History navigiert wird
    private string _inputDraft     = "";

    public bool IsOpen => _isOpen;

    // Maximale Anzahl History-Einträge (aus EngineSettings)
    private int HistorySize => _context.Settings.ConsoleHistorySize;

    public DebugConsole(GameContext context)
    {
        _context = context;
    }

    public void Register(ICommand command)
        => _commands[command.Name] = command;

    public void Toggle()
    {
        _isOpen = !_isOpen;
        if (_isOpen)
            ResetHistoryNavigation();
    }

    public void Execute(string input)
    {
        input = input.Trim();
        if (string.IsNullOrEmpty(input))
            return;

        // Duplikat-Schutz: letzten Eintrag nicht doppelt speichern
        if (_inputHistory.Count == 0 || _inputHistory[^1] != input)
        {
            _inputHistory.Add(input);
            // History-Größe begrenzen (älteste Einträge entfernen)
            while (_inputHistory.Count > HistorySize)
                _inputHistory.RemoveAt(0);
        }

        // Nach Ausführung: Navigation zurücksetzen
        ResetHistoryNavigation();

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

    /// <summary>
    /// Navigiert in der History nach oben (ältere Befehle).
    /// Gibt den neuen Input-String zurück oder null wenn keine Änderung.
    /// </summary>
    public string? NavigateHistoryUp(string currentInput)
    {
        if (_inputHistory.Count == 0)
            return null;

        // Beim ersten Hoch-Drücken den aktuellen Tipp-Puffer merken
        if (_historyIndex == -1)
            _inputDraft = currentInput;

        int newIndex = _historyIndex == -1
            ? _inputHistory.Count - 1
            : Math.Max(0, _historyIndex - 1);

        if (newIndex == _historyIndex)
            return null; // Schon am ältesten Eintrag, nichts tun

        _historyIndex = newIndex;
        return _inputHistory[_historyIndex];
    }

    /// <summary>
    /// Navigiert in der History nach unten (neuere Befehle).
    /// Gibt den neuen Input-String zurück oder null wenn keine Änderung.
    /// </summary>
    public string? NavigateHistoryDown()
    {
        if (_historyIndex == -1)
            return null; // Nicht in der History

        if (_historyIndex == _inputHistory.Count - 1)
        {
            // Am Ende der History → zurück zum Tipp-Puffer
            _historyIndex = -1;
            return _inputDraft;
        }

        _historyIndex = Math.Min(_inputHistory.Count - 1, _historyIndex + 1);
        return _inputHistory[_historyIndex];
    }

    /// <summary>
    /// Tab-Autocomplete für das erste Wort (Kommando-Name).
    /// Gibt den vervollständigten Input-String zurück oder null wenn keine Änderung.
    /// Gibt im Mehrfach-Treffer-Fall alle Kandidaten ins Output-Log aus.
    /// </summary>
    public string? Autocomplete(string currentInput)
    {
        // Autocomplete nur auf das erste Wort anwenden
        var spaceIndex = currentInput.IndexOf(' ');
        if (spaceIndex != -1)
            return null; // Argumente: nichts tun

        var prefix = currentInput;
        var matches = _commands.Keys
            .Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (matches.Count == 0)
            return null;

        if (matches.Count == 1)
            return matches[0] + " ";

        // Längsten gemeinsamen Prefix bestimmen
        var lcp = LongestCommonPrefix(matches);

        // Kandidaten ins Log schreiben
        Log("  " + string.Join("  ", matches));

        // Wenn der LCP länger als der aktuelle Input ist, einsetzen
        return lcp.Length > prefix.Length ? lcp : null;
    }

    public void Log(string message)
    {
        _output.Add(message);
        if (_output.Count > 50)
            _output.RemoveAt(0);
    }

    // Für Engine.cs: Navigation zurücksetzen wenn Konsole geschlossen oder Enter gedrückt wird
    public void ResetHistoryNavigation()
    {
        _historyIndex = -1;
        _inputDraft   = "";
    }

    public IReadOnlyList<string>               GetOutput()   => _output;
    public IReadOnlyList<string>               GetHistory()  => _inputHistory;
    public IReadOnlyDictionary<string, ICommand> GetCommands() => _commands;

    public void Dispose() { }

    // Hilfsmethode: Längsten gemeinsamen Prefix einer Liste von Strings berechnen
    private static string LongestCommonPrefix(List<string> strings)
    {
        if (strings.Count == 0) return "";
        var first = strings[0];
        int length = first.Length;
        for (int i = 1; i < strings.Count; i++)
        {
            length = Math.Min(length, strings[i].Length);
            for (int j = 0; j < length; j++)
            {
                if (char.ToLowerInvariant(first[j]) != char.ToLowerInvariant(strings[i][j]))
                {
                    length = j;
                    break;
                }
            }
        }
        return first[..length];
    }
}
