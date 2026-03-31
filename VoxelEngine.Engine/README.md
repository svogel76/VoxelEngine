# VoxelEngine.Engine

> Wiederverwendbarer Voxel-Engine-Kern als Class Library (.NET 10). Enthaelt Rendering-Pipeline, Chunk-System, Physik und Persistenz. Oeffentliche Vertrags-Typen fuer Spiel und Mods liegen in `VoxelEngine.Api`; spielspezifischer Code gehoert in ein separates Projekt, das `IGameMod` implementiert.

## Schnellstart

```csharp
// 1. IGameMod implementieren
public class MyGame : IGameMod
{
    public void RegisterBlocks(IBlockRegistry registry) { /* Bloecke registrieren */ }
    public void Initialize(IGameContext context)        { /* Welt aufbauen */ }
    public void Update(double deltaTime)                { /* Spiellogik */ }
    public void Render(double deltaTime)                { /* Eigenes Rendering */ }
    public void Shutdown()                              { /* Aufraeumen */ }
}

// 2. Engine starten
new EngineRunner(settings, bindings).Run(new MyGame());
```

## IGameMod - Lifecycle

Der `EngineRunner` ruft die Methoden in fester Reihenfolge auf:

```
RegisterBlocks(IBlockRegistry)
  -> Initialize(IGameContext)
        -> [Game Loop]
              -> Update(deltaTime)
              -> Render(deltaTime)
        -> Shutdown()
```

## IGameContext - Engine-Zugriff

Das Spiel kommuniziert mit der Engine ausschliesslich ueber `VoxelEngine.Api`. Kein direkter Zugriff auf Engine-Klassen ist fuer den Vertragslayer noetig.

## Projektstruktur

```
VoxelEngine.Api/       # Oeffentliche Vertraege fuer Engine und Mods
VoxelEngine.Engine/    # Laufzeit, Rendering, World, Persistence
```

## Architekturregeln

- `World/` hat keine Silk.NET-Abhaengigkeit
- GL-Aufrufe nur im Main Thread
- Keine Magic Numbers - alles via `EngineSettings`
- Neue Debug-Kommandos als eigene Klassen in `Core/Debug/Commands/`
- Persistenz durchgehend `async`
- Unit Tests fuer neue `World/`-Logik
