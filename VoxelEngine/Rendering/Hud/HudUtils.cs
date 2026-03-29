using VoxelEngine.Core.Hud;

namespace VoxelEngine.Rendering.Hud;

/// <summary>Hilfsklasse für HUD-Positionsberechnungen.</summary>
public static class HudUtils
{
    /// <summary>
    /// Berechnet die obere linke Ecke eines HUD-Elements in Pixeln
    /// aus Anchor, relativem Offset und Elementgröße.
    /// </summary>
    public static (float x, float y) ResolveAnchor(
        HudAnchor anchor,
        float     offsetX,
        float     offsetY,
        int       screenW,
        int       screenH,
        float     elementWidth  = 0f,
        float     elementHeight = 0f)
    {
        float ox = offsetX * screenW;
        float oy = offsetY * screenH;

        return anchor switch
        {
            HudAnchor.TopLeft      => (ox,                                          oy),
            HudAnchor.TopCenter    => (screenW / 2f - elementWidth / 2f + ox,      oy),
            HudAnchor.TopRight     => (screenW - elementWidth - ox,                 oy),
            HudAnchor.MiddleLeft   => (ox,                                          screenH / 2f - elementHeight / 2f + oy),
            HudAnchor.Center       => (screenW / 2f - elementWidth / 2f + ox,      screenH / 2f - elementHeight / 2f + oy),
            HudAnchor.MiddleRight  => (screenW - elementWidth - ox,                 screenH / 2f - elementHeight / 2f + oy),
            HudAnchor.BottomLeft   => (ox,                                          screenH - elementHeight - oy),
            HudAnchor.BottomCenter => (screenW / 2f - elementWidth / 2f + ox,      screenH - elementHeight - oy),
            HudAnchor.BottomRight  => (screenW - elementWidth - ox,                 screenH - elementHeight - oy),
            _                      => (ox, oy)
        };
    }
}
