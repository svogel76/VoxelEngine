using VoxelEngine.Core;
using VoxelEngine.Core.Hud;
using VoxelEngine.World;

namespace VoxelEngine.Rendering.Hud;

/// <summary>HUD-Element für Debug-Overlay und Konsole.</summary>
public sealed class DebugHudElement : IHudElement
{
    public string Id => "debug";
    public bool   Visible { get; set; } = true;

    private HudElementConfig _config = new(
        "debug", HudAnchor.TopLeft,
        OffsetX: 0.003f, OffsetY: 0.005f,
        Scale: 1f, Visible: true, ZOrder: 10);

    public HudElementConfig Config => _config;

    // ── Werte die via Update() befüllt werden ────────────────────────────────
    public double               Fps           { get; set; }
    public string               Position      { get; private set; } = "";
    public string               ChunkInfo     { get; private set; } = "";
    public string               VertInfo      { get; private set; } = "";
    public string               TimeStr       { get; private set; } = "";
    public string               SelectedBlock { get; private set; } = "";
    public string               ReachStr      { get; private set; } = "";
    public bool                 ConsoleOpen   { get; private set; }
    public IReadOnlyList<string> ConsoleOutput { get; private set; } = [];
    // Von Engine.cs gesetzt (nicht aus GameContext lesbar)
    public string               ConsoleInput  { get; set; } = "";

    public void ApplyConfig(HudElementConfig config) => _config = config;

    public void Update(GameContext ctx)
    {
        var pos  = ctx.Camera.Position;
        Position = $"X:{pos.X:F1} Y:{pos.Y:F1} Z:{pos.Z:F1}";

        int visible = ctx.Renderer.VisibleChunkCount;
        int loaded  = ctx.World.LoadedChunkCount;
        ChunkInfo = $"Chunks: {visible}/{loaded}";

        VertInfo = $"Verts: {ctx.Renderer.TotalVertexCount:N0}";

        var wt    = ctx.Time;
        TimeStr   = $"Time: {(int)wt.Time:D2}:{(int)(wt.Time % 1 * 60):D2}";

        SelectedBlock = $"Block: {BlockRegistry.Get(ctx.Player.SelectedBlock).Name}";
        ReachStr      = $"Reach: {ctx.Player.InteractionReach:F1}";

        ConsoleOpen   = ctx.Console.IsOpen;
        ConsoleOutput = ctx.Console.GetOutput();
    }
}
