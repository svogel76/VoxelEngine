using VoxelEngine.Core.Hud;

namespace VoxelEngine.Core.Debug.Commands;

/// <summary>Debug-Kommando für HUD-Verwaltung: reload / toggle / list.</summary>
public class HudCommand : ICommand
{
    public string Name        => "hud";
    public string Description => "HUD-Verwaltung: reload, toggle, list";
    public string Usage       => "hud reload | hud toggle <id> | hud list";

    private readonly HudRegistry _registry;

    public HudCommand(HudRegistry registry)
    {
        _registry = registry;
    }

    public void Execute(string[] args, GameContext context)
    {
        if (args.Length == 0)
        {
            context.Console.Log("Usage: hud reload | hud toggle <id> | hud list");
            return;
        }

        switch (args[0].ToLowerInvariant())
        {
            case "reload":
                _registry.ReloadConfig();
                context.Console.Log("HUD config reloaded.");
                break;

            case "toggle" when args.Length >= 2:
            {
                var element = _registry.GetById(args[1]);
                if (element is null)
                    context.Console.Log($"Unknown HUD element: '{args[1]}'");
                else
                {
                    element.Visible = !element.Visible;
                    context.Console.Log($"HUD '{element.Id}' is now {(element.Visible ? "visible" : "hidden")}.");
                }
                break;
            }

            case "list":
            {
                var all = _registry.GetAll();
                if (all.Count == 0)
                {
                    context.Console.Log("No HUD elements registered.");
                    break;
                }
                foreach (var el in all)
                    context.Console.Log($"  {el.Id}: visible={el.Visible}  zOrder={el.Config.ZOrder}  anchor={el.Config.Anchor}");
                break;
            }

            default:
                context.Console.Log("Usage: hud reload | hud toggle <id> | hud list");
                break;
        }
    }
}
