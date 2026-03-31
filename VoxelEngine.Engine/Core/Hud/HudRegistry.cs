using System.Text.Json;
using System.Text.Json.Serialization;

namespace VoxelEngine.Core.Hud;

public sealed class HudRegistry
{
    private readonly Dictionary<string, IHudElement> _elements = new(StringComparer.OrdinalIgnoreCase);
    private string? _configPath;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public void Register(IHudElement element)
    {
        _elements[element.Id] = element;
    }

    public void LoadConfig(string jsonPath)
    {
        _configPath = jsonPath;
        ApplyConfig(jsonPath);
    }

    public void ReloadConfig()
    {
        if (_configPath is null)
            return;
        ApplyConfig(_configPath);
    }

    public IReadOnlyList<IHudElement> GetAll()
        => _elements.Values
            .OrderBy(e => e.Config.ZOrder)
            .ToList();

    public IHudElement? GetById(string id)
        => _elements.TryGetValue(id, out var element) ? element : null;

    // ── Privat ───────────────────────────────────────────────────────────────

    private void ApplyConfig(string jsonPath)
    {
        if (!File.Exists(jsonPath))
            return;

        var json = File.ReadAllText(jsonPath);
        var doc  = JsonSerializer.Deserialize<HudConfigDocument>(json, JsonOptions);
        if (doc?.Elements is null)
            return;

        foreach (var entry in doc.Elements)
        {
            if (!_elements.TryGetValue(entry.Id, out var element))
                continue;

            var anchor = Enum.Parse<HudAnchor>(entry.Anchor, ignoreCase: true);
            var config = new HudElementConfig(
                entry.Id,
                anchor,
                entry.OffsetX,
                entry.OffsetY,
                entry.Scale,
                entry.Visible,
                entry.ZOrder);

            element.ApplyConfig(config);
            element.Visible = config.Visible;
        }
    }

    // ── JSON-Datenmodell ──────────────────────────────────────────────────────

    private sealed class HudConfigDocument
    {
        public List<HudConfigEntry>? Elements { get; set; }
    }

    private sealed class HudConfigEntry
    {
        public string Id      { get; set; } = "";
        public string Anchor  { get; set; } = "TopLeft";
        public float  OffsetX { get; set; }
        public float  OffsetY { get; set; }
        public float  Scale   { get; set; } = 1f;
        public bool   Visible { get; set; } = true;
        public int    ZOrder  { get; set; }
    }
}
