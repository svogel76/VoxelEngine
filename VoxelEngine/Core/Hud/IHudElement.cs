namespace VoxelEngine.Core.Hud;

public interface IHudElement
{
    string           Id      { get; }
    bool             Visible { get; set; }
    HudElementConfig Config  { get; }
    void ApplyConfig(HudElementConfig config);
    void Update(GameContext ctx);
}
