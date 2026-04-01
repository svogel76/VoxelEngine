# VoxelEngine.Engine

> Wiederverwendbarer Voxel-Engine-Kern als Class Library (.NET 10). Enthaelt Rendering-Pipeline, Chunk-System, Physik und Persistenz. Oeffentliche Vertrags-Typen fuer Spiel und Mods liegen in `VoxelEngine.Api`; spielspezifischer Code gehoert in ein separates Projekt, das `IGameMod` implementiert.

## Schnellstart

```csharp
// 1. IGameMod implementieren (in eigenem Projekt, nur VoxelEngine.Api referenzieren)
public class MyGame : IGameMod
{
    public void RegisterBlocks(IBlockRegistry registry)     { /* Bloecke registrieren */ }
    public void RegisterComponents(IComponentRegistry reg)  { /* Eigene Komponenten */ }
    public void RegisterBehaviours(IBehaviourRegistry reg)  { /* Eigene Conditions/Actions */ }
    public void Initialize(IGameContext context)             { /* Welt aufbauen */ }
    public void Update(double deltaTime)                    { /* Spiellogik */ }
    public void Render(double deltaTime)                    { /* Eigenes Rendering */ }
    public void Shutdown()                                  { /* Aufraeumen */ }
}

// 2. Als Mod bereitstellen: VoxelEngine.Game.dll + mod.json in Mods/<modid>/
// 3. Launcher startet alles: ModLoader().LoadAll("Mods/") + EngineRunner().Run(mods)
```

## IGameMod - Lifecycle

Der `EngineRunner` ruft die Methoden in fester Reihenfolge auf:

```
RegisterBlocks(IBlockRegistry)
  -> RegisterComponents(IComponentRegistry)
    -> RegisterBehaviours(IBehaviourRegistry)
      -> Initialize(IGameContext)
            -> [Game Loop]
                  -> Update(deltaTime)
                  -> Render(deltaTime)
            -> Shutdown()
```

## IGameContext - Engine-Zugriff

Das Spiel kommuniziert mit der Engine ausschliesslich ueber `VoxelEngine.Api`. Kein direkter Zugriff auf Engine-Klassen ist fuer den Vertragslayer noetig.

## Mod System

### IBehaviourRegistry — eigene Conditions/Actions registrieren

```csharp
public void RegisterBehaviours(IBehaviourRegistry registry)
{
    // Eigene Condition
    registry.RegisterCondition("is_raining", ctx =>
        ctx.GameContext.World.IsRaining);

    // Eigene Action
    registry.RegisterAction("seek_shelter", (ctx, node) =>
        new SeekShelterAction(ctx, node));
}
```

Danach ist `"type": "is_raining"` und `"type": "seek_shelter"` in jedem Behaviour-Tree-JSON verwendbar — ohne Engine-Rebuild.

### IComponentRegistry — eigene Komponenten registrieren

```csharp
public void RegisterComponents(IComponentRegistry registry)
{
    registry.Register("magic_aura", (entity, config) =>
        new MagicAuraComponent(entity, config));
}
```

Danach ist `"type": "magic_aura"` in Entity-JSON nutzbar:

```json
{ "type": "magic_aura", "radius": 3.0, "damage_per_second": 2 }
```

### IModContext — was Mods erhalten

Jede Mod erhaelt beim Laden einen `IModContext` mit:

| Eigenschaft | Beschreibung |
|---|---|
| `AssetBasePath` | Absoluter Pfad zu `Mods/<modid>/Assets/` |
| `ComponentRegistry` | Zum Registrieren eigener Komponenten |
| `BehaviourRegistry` | Zum Registrieren eigener Conditions/Actions |
| `Logger` | Mod-scoped Logging |

Assets immer relativ zu `AssetBasePath` laden — nie absolute Pfade hardcoden.

## Projektstruktur

```
VoxelEngine.Api/       # Oeffentliche Vertraege fuer Engine und Mods
VoxelEngine.Engine/    # Laufzeit, Rendering, World, Persistence, BehaviourRegistry
Mods/<modid>/          # Laufzeit-Ordner pro Mod (DLL + mod.json + Assets)
```

## Architekturregeln

- `World/` hat keine Silk.NET-Abhaengigkeit
- GL-Aufrufe nur im Main Thread
- Keine Magic Numbers - alles via `EngineSettings`
- Neue Debug-Kommandos als eigene Klassen in `Core/Debug/Commands/`
- Persistenz durchgehend `async`
- Unit Tests fuer neue `World/`-Logik
