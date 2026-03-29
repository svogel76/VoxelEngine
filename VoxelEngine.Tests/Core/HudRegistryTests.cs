using VoxelEngine.Core;
using VoxelEngine.Core.Hud;
using Xunit;

namespace VoxelEngine.Tests.Core;

// Minimale IHudElement-Implementierung für Tests (kein GameContext nötig)
public class TestHudElement : IHudElement
{
    public TestHudElement(string id, int zOrder = 10)
    {
        Id      = id;
        _config = new HudElementConfig(id, HudAnchor.TopLeft, 0f, 0f, 1f, true, zOrder);
    }

    public string           Id      { get; }
    public bool             Visible { get; set; } = true;
    private HudElementConfig _config;
    public HudElementConfig Config  => _config;

    public void ApplyConfig(HudElementConfig config) => _config = config;
    public void Update(GameContext ctx) { }
}

public class HudRegistryTests
{
    [Fact]
    public void Register_ElementAvailableViaGetById()
    {
        var registry = new HudRegistry();
        var element  = new TestHudElement("test");
        registry.Register(element);
        Assert.Same(element, registry.GetById("test"));
    }

    [Fact]
    public void GetAll_ReturnsSortedByZOrder()
    {
        var registry = new HudRegistry();
        registry.Register(new TestHudElement("high", zOrder: 20));
        registry.Register(new TestHudElement("low",  zOrder: 10));
        var all = registry.GetAll();
        Assert.Equal("low",  all[0].Id);
        Assert.Equal("high", all[1].Id);
    }

    [Fact]
    public void ReloadConfig_UpdatesExistingElements()
    {
        var registry = new HudRegistry();
        var element  = new TestHudElement("debug");
        registry.Register(element);

        string tmpPath = Path.GetTempFileName();
        File.WriteAllText(tmpPath, """
        {
          "elements": [
            { "id": "debug", "anchor": "TopRight", "offsetX": 0.1, "offsetY": 0.2, "scale": 2.0, "visible": false, "zOrder": 99 }
          ]
        }
        """);

        try
        {
            registry.LoadConfig(tmpPath);
            Assert.Equal(HudAnchor.TopRight, element.Config.Anchor);
            Assert.Equal(0.1f,  element.Config.OffsetX, precision: 3);
            Assert.False(element.Visible);
        }
        finally
        {
            File.Delete(tmpPath);
        }
    }

    [Fact]
    public void Register_OverwritesExistingElementWithSameId()
    {
        var registry = new HudRegistry();
        var first    = new TestHudElement("dup");
        var second   = new TestHudElement("dup");
        registry.Register(first);
        registry.Register(second);
        Assert.Same(second, registry.GetById("dup"));
    }
}
