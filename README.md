# VoxelEngine

> Eine prozedural generierte Voxel-Engine im Minecraft-Stil, gebaut mit C# und OpenGL (Silk.NET). Schwerpunkte: performantes Greedy-Meshing, dynamischer Tageszyklus mit Atmosphäre, multithreading-basierte Chunk-Verwaltung sowie eine saubere Engine/Mod-Trennung — das Spiel selbst ist eine Mod.

## 🗂 Projektstruktur

```
VoxelEngine.sln
├── VoxelEngine.Api/                  # Interfaces only — keine Implementierung
│   └── IGameMod, IGameContext, IComponent, IBehaviourNode, IModContext, ...
│
├── VoxelEngine.Engine/               # Class Library — wiederverwendbarer Engine-Kern
│   ├── Assets/
│   │   └── Shaders/                  # GLSL-Shader (embedded resource)
│   ├── Core/                         # Game Loop, Input, Konfiguration, HUD-Framework
│   │   └── Debug/
│   │       └── Commands/             # Debug-Konsolen-Befehle
│   ├── Entity/                       # Entity, EntityManager, IComponent-Implementierungen
│   ├── Persistence/                  # IWorldPersistence, LocalFilePersistence
│   ├── Rendering/                    # OpenGL-Rendering (Shader, Mesh, Renderer, Skybox)
│   ├── World/                        # Chunks, Generator, Physik, Registries
│   ├── BehaviourRegistry.cs          # IBehaviourNode-Lookup (Conditions + Actions)
│   └── EngineRunner.cs               # Lifecycle (RegisterBlocks → Init → Loop → Shutdown)
│
├── VoxelEngine.Game/                 # Mod-DLL → Mods/VoxelGame/VoxelGame.dll
│   ├── Assets/                       # Texturen, hud.json, Fonts, Keybindings
│   ├── Blocks/                       # BlockDefinitions & Registrierung
│   └── VoxelGame.cs                  # IGameMod-Implementierung
│
├── VoxelEngine.Launcher/             # Executable — 2-Zeilen Bootstrap
│   └── Program.cs                    # ModLoader().LoadAll() + EngineRunner().Run()
│
├── VoxelEngine.Tests/                # xUnit + FluentAssertions
│   └── Mocks/
│       ├── MinimalTestGame.cs        # No-Op IGameMod-Stub
│       └── TestBootstrap.cs          # Block-Registrierung fuer Tests
│
├── Mods/
│   └── VoxelGame/                    # Laufzeit-Ausgabe von VoxelEngine.Game
│       ├── VoxelGame.dll
│       ├── mod.json
│       └── Assets/
│
└── Run/                              # Startverzeichnis von VoxelEngine.Launcher (gitignored)
```

## 🏛 Architektur-Überblick

### Engine/Mod-Trennung

Die Engine (`VoxelEngine.Engine.dll`) kennt keinerlei spielspezifischen Code. Mods implementieren `IGameMod` aus `VoxelEngine.Api` — der einzigen Abhängigkeit für alle Mods:

```csharp
// Program.cs (VoxelEngine.Launcher)
var mods = new ModLoader().LoadAll("Mods/");
new EngineRunner().Run(mods);
```

Der `EngineRunner` steuert den vollständigen Lifecycle:

```
RegisterBlocks(IBlockRegistry)
  → RegisterComponents(IComponentRegistry)
    → RegisterBehaviours(IBehaviourRegistry)
      → Initialize(IGameContext)
        → Update(deltaTime) + Render(deltaTime)  [Game Loop]
          → Shutdown()
```

### Component System

Alle Entities sind plain `Entity`-Instanzen, zusammengesetzt aus `IComponent`-Implementierungen. Keine Vererbungshierarchie — Verhalten kommt aus Komponenten:

```json
{
  "type": "animal",
  "components": [
    { "type": "health", "max": 10 },
    { "type": "physics", "mass": 1.0 },
    { "type": "ai", "behaviour_tree": "wander_flee" },
    { "type": "drops", "items": ["wood"] },
    { "type": "render", "model": "cow.vox" }
  ]
}
```

Neue Komponenten werden per `IComponentRegistry.Register(name, factory)` angemeldet — keine Engine-Änderung nötig.

### Behaviour Trees

AI-Logik ist datengetrieben via JSON — kein hardcodierter Zustands-Automat. Conditions und Actions werden per Name aus der `BehaviourRegistry` aufgelöst:

```json
{
  "type": "selector",
  "children": [
    { "type": "sequence", "children": [
      { "type": "player_near", "radius": 8 },
      { "type": "flee", "speed": 4.0, "radius": 12 }
    ]},
    { "type": "wander", "speed": 1.5, "radius": 20, "pause_seconds": 3 }
  ]
}
```

Neue Conditions/Actions via `IBehaviourRegistry.RegisterCondition/RegisterAction` — kein Rebuild.

### Mod System

`ModLoader` scannt `Mods/` zur Laufzeit, liest `mod.json` (id, name, version, dependencies), löst die Ladereihenfolge auf und lädt Assemblies in dieselbe AppDomain. Jede Mod erhält einen `IModContext` mit `AssetBasePath`:

```
Mods/
  VoxelGame/
    VoxelGame.dll    ← implementiert IGameMod
    mod.json         ← { "id": "voxelgame", "version": "1.0.0", "dependencies": [] }
    Assets/          ← alles relativ zu AssetBasePath
```

`VoxelEngine.Game` ist eine gewöhnliche Mod — keine Sonder-Privilegien.

### Chunk-System

Chunks haben eine feste Größe von 16 × 256 × 16 Blöcken. Ein `ChunkManager` verwaltet dynamisches Laden und Entladen um die Spielerposition (konfigurierbare `RenderDistance` / `UnloadDistance` mit Hysterese). Chunk-Generierung und Mesh-Erstellung laufen in einem Hintergrund-Thread-Pool (`ChunkWorker`); GPU-Uploads erfolgen ausschließlich im Haupt-Thread (max. 4 Uploads pro Frame).

### Rendering-Pipeline

1. **Skybox** – Prozeduraler Himmelsgradient mit 8-Keyframe-Farbkurve über 24 Stunden, Sonne/Mond als Billboard-Quads mit 8 Mondphasen, 1 500 instanziert gerenderte Sterne mit Twinkling-Effekt.
2. **Opaque Pass** – Alle undurchsichtigen Blöcke mit aktiviertem Depth-Write und Frustum-Culling (Gribb-Hartmann-Methode).
3. **Transparent Pass** – Wasser, Glas, Eis ohne Depth-Write, nach-hinten-vorne sortiert, Alpha-Blending aktiviert.
4. **Entity Pass** – Voxel-Modelle (`.vox`) und Billboard-Sprites mit separatem Entity-Atlas; Frustum-Culling via Spatial Hashing.
5. **HUD/UI Pass** – HUD-Elemente (FPS, Position, Hotbar, Health/Hunger), Inventar-Fenster, Debug-Konsole.

### Greedy Meshing

Der `GreedyMeshBuilder` verwendet einen 3-Achsen-Sweep, der benachbarte, identische Flächen zu größeren Quads zusammenführt. Dabei werden Ambient-Occlusion-Werte (0–3 pro Vertex) und Diagonalflip zur Vermeidung von Streifenartefakten berücksichtigt. Texturen werden über ein `Texture2DArray` kachel-korrekt aufgebracht.

### Weltgenerierung

`WorldGenerator` verwendet FastNoiseLite mit OpenSimplex2-Rauschen. Klimazonen-basierte Terrain-Generierung über datengetriebene JSON-Definitionen. `SampleBlock()` ermöglicht deterministisches Block-Sampling unabhängig von der Chunk-Ladereihenfolge.

### Persistenz

`IWorldPersistence` abstrahiert die Speicherebene. `LocalFilePersistence` schreibt binäre Region-Dateien (VXP5-Format). `InMemoryPersistence` dient für Tests. Dirty-Flag-System (`Chunk.PlayerEdits`, `IsDirty`) stellt sicher, dass nur veränderte Chunks geschrieben werden.

### Entity-System

`EntityManager` verwaltet Entities mit Spatial Hashing und Frustum-Culling. Entities sind Kompositionen aus `IComponent`-Instanzen; AI-Verhalten läuft über datengetriebene Behaviour Trees. Spawn-Konfiguration erfolgt über Klimazonen-JSON.

### Beleuchtung & Atmosphäre

- **Ambient Occlusion**: Vertex-AO, in Greedy-Meshing integriert
- **Diffuse Lighting**: Richtungsabhängige Flächenhelligkeit (Top 100 %, Seiten 60–85 %, Boden 40 %)
- **Globales Licht**: Tageszyklus mit 8-Keyframe-Kurve (Nacht 3 % – Tag 100 % Ambient)
- **Nebel**: Linearer Nebel, konfigurierbar über `EngineSettings`

## 🚀 Quickstart

**Voraussetzungen**
- .NET 10.0 SDK
- OpenGL-fähige Grafikkarte (OpenGL 3.3+)
- Visual Studio 2022 oder `dotnet`-CLI

**Bauen & Starten**

```bash
git clone https://github.com/svogel76/VoxelEngine
cd VoxelEngine

dotnet build
dotnet run --project VoxelEngine.Launcher/VoxelEngine.Launcher.csproj
```

**Tests**

```bash
dotnet test VoxelEngine.Tests/VoxelEngine.Tests.csproj
```

**Steuerung**

| Taste | Aktion |
|---|---|
| W / A / S / D | Bewegen |
| Leertaste / Shift | Hoch / Runter |
| Maus | Umsehen |
| Linke Maustaste | Block abbauen |
| Rechte Maustaste | Block platzieren |
| 1–9 / Mausrad | Hotbar-Slot wählen |
| E | Inventar öffnen/schließen |
| Escape | Pause-Menü |
| F1 | Debug-Konsole öffnen/schließen |

In der Konsole `help` eingeben für eine Liste aller Befehle.

## 📚 Dokumentation

| Dokument | Beschreibung |
|---|---|
| [Backlog.md](Backlog.md) | Offene Aufgaben & priorisierte Features nach Phasen |
| [Claude.md](Claude.md) | Architekturentscheidungen & Konventionen |
| [VoxelEngine.Engine/Docs/Rendering/AmbientOcclusion.md](VoxelEngine.Engine/Docs/Rendering/AmbientOcclusion.md) | Vertex-AO-Implementierung |
| [VoxelEngine.Engine/Docs/Rendering/Fog.md](VoxelEngine.Engine/Docs/Rendering/Fog.md) | Linearer Nebel: Parameter, Farbmischung |
| [VoxelEngine.Engine/Docs/Rendering/GreedyMeshing.md](VoxelEngine.Engine/Docs/Rendering/GreedyMeshing.md) | 3-Achsen-Sweep, UV-Kachelung |
| [VoxelEngine.Engine/Docs/Rendering/MultiThreading.md](VoxelEngine.Engine/Docs/Rendering/MultiThreading.md) | Producer-Consumer-Muster, GPU-Upload |
| [VoxelEngine.Engine/Docs/World/ClimateZones.md](VoxelEngine.Engine/Docs/World/ClimateZones.md) | Klimazonen (Temperatur, Feuchtigkeit, Köppen) |
| [VoxelEngine.Engine/Docs/World/WorldTime.md](VoxelEngine.Engine/Docs/World/WorldTime.md) | 24-Stunden-Uhr, Tageszyklus |

## 🔧 Technologien

| Bibliothek | Version | Verwendung |
|---|---|---|
| [Silk.NET.OpenGL](https://github.com/dotnet/Silk.NET) | 2.23.0 | OpenGL-Bindings für Rendering |
| Silk.NET.Windowing | 2.23.0 | Fensterverwaltung, Game-Loop |
| Silk.NET.Input | 2.23.0 | Tastatur- und Mauseingabe |
| Silk.NET.Maths | 2.23.0 | Vektor- und Matrizenmathematik |
| [StbImageSharp](https://github.com/StbSharp/StbImageSharp) | 2.30.15 | Laden von Texturdaten |
| FastNoiseLite | (eingebettet) | OpenSimplex2-Rauschgenerierung |
| xUnit + FluentAssertions | — | Unit Tests (`VoxelEngine.Tests`) |

Laufzeitumgebung: **.NET 10.0**, Sprache: **C# 13** mit aktivierter Nullable-Analyse.

## 📋 Status

**Abgeschlossen**
- Engine/Mod-Trennung: `VoxelEngine.Api` (Interfaces), `VoxelEngine.Engine` (Kern), `VoxelEngine.Game` (Mod-DLL), `VoxelEngine.Launcher` (Bootstrap-Exe)
- Mod/Plugin-System vollständig: `ModLoader`, `IGameMod`, `mod.json`, `IModContext` mit `AssetBasePath`
- Component System: `IComponent`, `HealthComponent`, `PhysicsComponent`, `AIComponent`, `DropComponent`, `RenderComponent`
- Behaviour Trees: `IBehaviourNode`, `BehaviourRegistry`, JSON-datengetrieben
- Engine-Loop, Kamera, Eingabe, Shader-System (Shader als Embedded Resources)
- Chunk-Struktur, Weltgenerator, Noise-basiertes Terrain, Klimazonen
- Greedy-Meshing mit Ambient-Occlusion, Frustum-Culling, Diffuse-Beleuchtung
- Multithreading (Hintergrund-Generierung + Meshing), Chunk-Persistenz (VXP5)
- Debug-Konsole, HUD-Framework (JSON-konfigurierbar), Bitmap-Font
- Prozeduraler Himmel, Tageszyklus, Sonne/Mond/Sterne, Nebel, Transparenz
- Inventar (Hotbar, Drag & Drop, Equipment-Slots), Health/Hunger-System
- Entity-System mit Spatial Hashing, `.vox`-Rendering, Komponenten-AI

**In Arbeit (Phase 7)**
- Block-Pickup (Abbauen → Inventar) und Dekrement beim Platzieren
- Fog-Command Inversion Bug
- Verbleibende Vegetation + Tier-Features
