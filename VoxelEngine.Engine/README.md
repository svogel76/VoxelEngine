# VoxelEngine.Engine

> Wiederverwendbarer Voxel-Engine-Kern als Class Library (.NET 10). Enthält Rendering-Pipeline, Chunk-System, Physik, Persistenz und alle Infrastruktur-Interfaces. Spielspezifischer Code gehört in ein separates Projekt, das `IGame` implementiert.

## Schnellstart

```csharp
// 1. IGame implementieren
public class MyGame : IGame
{
    public void RegisterBlocks(IBlockRegistry registry) { /* Blöcke registrieren */ }
    public void Initialize(IGameContext context)        { /* Welt aufbauen */ }
    public void Update(double deltaTime)                { /* Spiellogik */ }
    public void Render(double deltaTime)                { /* Eigenes Rendering */ }
    public void Shutdown()                              { /* Aufräumen */ }
}

// 2. Engine starten
new EngineRunner().Run(new MyGame());
```

## IGame — Lifecycle

Der `EngineRunner` ruft die Methoden in fester Reihenfolge auf:

```
RegisterBlocks(IBlockRegistry)
  └─► Initialize(IGameContext)
        └─► [Game Loop]
              ├─► Update(deltaTime)
              └─► Render(deltaTime)
        └─► Shutdown()
```

| Methode | Zweck |
|---|---|
| `RegisterBlocks` | Alle `BlockDefinition`-Einträge in die Registry schreiben — vor Init |
| `Initialize` | `IGameContext` entgegennehmen, Welt/Spieler aufbauen |
| `Update` | Spiellogik pro Fixed Timestep (Standard: 60 UPS) |
| `Render` | Eigene Render-Aufrufe; Engine-Rendering läuft automatisch |
| `Shutdown` | Ressourcen freigeben |

## IGameContext — Engine-Zugriff

Das Spiel kommuniziert mit der Engine ausschließlich über `IGameContext`. Kein direkter Zugriff auf Engine-Klassen.

```csharp
public interface IGameContext
{
    IBlockRegistry BlockRegistry { get; }
    IWorldAccess   World         { get; }
    IInputState    Input         { get; }
}
```

### IBlockRegistry

```csharp
registry.Register(new BlockDefinition { Id = 1, Name = "Grass", ... });
BlockDefinition? def = registry.Get(1);
```

### IWorldAccess

Lesender/schreibender Zugriff auf die Voxel-Welt ohne direkte Chunk-Referenzen.

### IInputState

Tastatur- und Mausstatus im aktuellen Frame — für spielseitige Input-Verarbeitung in `Update()`.

## Architekturregeln

Diese Regeln gelten für alle Beiträge zur Engine:

| Regel | Begründung |
|---|---|
| `World/` hat **keine** Silk.NET-Abhängigkeit | Portabilität, Unit-Testbarkeit |
| GL-Aufrufe nur im **Main Thread** | OpenGL-Kontextbindung |
| Keine Magic Numbers — alles via `EngineSettings` | Konfigurierbarkeit |
| Neue Debug-Kommandos als eigene Klassen in `Core/Debug/Commands/` | Erweiterbarkeit |
| Persistenz durchgehend `async` | Nicht-blockierender IO |
| Unit Tests für alle neue `World/`-Logik (xUnit + FluentAssertions) | Korrektheit |

## Projektstruktur

```
VoxelEngine.Engine/
├── Core/                    # Game Loop, Input, EngineSettings, HUD-Framework, Debug-Konsole
│   └── Debug/Commands/      # Debug-Kommandos (je eine Klasse pro Kommando)
├── Entity/                  # EntityManager, Basisklassen, AI-Zustände
├── Persistence/             # IWorldPersistence, LocalFilePersistence, InMemoryPersistence
├── Rendering/               # OpenGL, Shader, Mesh, Chunk-Renderer, HUD-Renderer, Skybox
├── World/                   # Chunks, Generator, Physik, BlockRegistry, WorldTime
├── Assets/Shaders/          # GLSL-Shader (Embedded Resources — nicht im Ausgabeverzeichnis)
├── IGame.cs
├── IGameContext.cs
├── IBlockRegistry.cs
├── IWorldAccess.cs
├── IInputState.cs
└── EngineRunner.cs
```

## Koordinatensystem

```
Chunk-Koordinate  : Math.Floor(worldCoord / Chunk.Width)
Lokal-Koordinate  : ((worldCoord % Width) + Width) % Width
Y-Achse           : keine Chunk-Unterteilung — Y=0 bis Y=255
```

## Vertex-Format

9 Floats pro Vertex: `x, y, z, u, v, tileLayer, ao, faceLight, cutout`

## Shader

Shader liegen als **Embedded Resources** in der DLL (`VoxelEngine.Engine.Assets.Shaders.*`). Das Shader-System sucht zuerst nach einer gleichnamigen Datei im Ausgabeverzeichnis (überschreibbar für Entwicklung), fällt sonst auf die eingebettete Version zurück.

## EngineSettings

Zentrale Konfigurationsklasse mit `init`-Properties. Übergabe an den `EngineRunner` vor dem Start:

```csharp
var settings = new EngineSettings
{
    RenderDistance  = 8,
    UpdatesPerSecond = 60,
    Gravity         = -24f,
    PlayerHeight    = 1.8f,
    // ...
};
new EngineRunner(settings).Run(new MyGame());
```

Vollständige Liste aller Properties: siehe `Core/EngineSettings.cs`.

## Persistenz

```csharp
// Standard: binäre Region-Dateien (VXP4)
IWorldPersistence persistence = new LocalFilePersistence("saves/world1");

// Für Tests:
IWorldPersistence persistence = new InMemoryPersistence();
```

Das Dirty-Flag-System (`Chunk.PlayerEdits`, `IsDirty`) sorgt dafür, dass nur veränderte Chunks geschrieben werden.

## Technologien

| Bibliothek | Version | Verwendung |
|---|---|---|
| Silk.NET.OpenGL | 2.23.0 | OpenGL-Bindings |
| Silk.NET.Windowing | 2.23.0 | Fensterverwaltung, Game-Loop |
| Silk.NET.Input | 2.23.0 | Eingabe |
| Silk.NET.Maths | 2.23.0 | Vektor-/Matrizenmathematik |
| StbImageSharp | 2.30.15 | Texturen laden |
| FastNoiseLite | eingebettet | Terrain-Noise |

Laufzeitumgebung: **.NET 10.0 / C# 13**
