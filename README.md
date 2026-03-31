# VoxelEngine

> Eine prozedural generierte Voxel-Engine im Minecraft-Stil, gebaut mit C# und OpenGL (Silk.NET). Schwerpunkte: performantes Greedy-Meshing, dynamischer Tageszyklus mit Atmosphäre, multithreading-basierte Chunk-Verwaltung sowie eine saubere Engine/Game-Trennung für Wiederverwendbarkeit und Testbarkeit.

## 🗂 Projektstruktur

```
VoxelEngine.sln
├── VoxelEngine.Engine/               # Class Library — wiederverwendbarer Engine-Kern
│   ├── Assets/
│   │   └── Shaders/                  # GLSL-Shader (embedded resource)
│   ├── Core/                         # Game Loop, Input, Konfiguration, HUD-Framework
│   │   └── Debug/
│   │       └── Commands/             # Debug-Konsolen-Befehle
│   ├── Entity/                       # Entity-Basisklassen, EntityManager
│   ├── Persistence/                  # IWorldPersistence, LocalFilePersistence
│   ├── Rendering/                    # OpenGL-Rendering (Shader, Mesh, Renderer, Skybox)
│   ├── World/                        # Chunks, Generator, Physik, Registries
│   ├── IGame.cs                      # Zentraler Vertrag zwischen Engine und Spiel
│   ├── IGameContext.cs               # Engine-Zugriff für das Spiel (World, Input, Registry)
│   ├── IBlockRegistry.cs
│   ├── IWorldAccess.cs
│   ├── IInputState.cs
│   └── EngineRunner.cs               # Lifecycle-Steuerung (RegisterBlocks → Init → Loop → Shutdown)
│
├── VoxelEngine.Game/                 # Executable — spielspezifischer Code
│   ├── Assets/                       # Texturen, hud.json, Fonts
│   ├── Blocks/                       # BlockDefinitions & Registrierung
│   ├── VoxelGame.cs                  # IGame-Implementierung
│   └── Program.cs                    # Einstiegspunkt: new EngineRunner().Run(new VoxelGame())
│
└── VoxelEngine.Tests/                # xUnit + FluentAssertions
    └── Mocks/
        ├── MinimalTestGame.cs        # No-Op IGame-Stub für Unit Tests
        └── TestBootstrap.cs          # Block-Registrierung für Tests
```

## 🏛 Architektur-Überblick

### Engine/Game-Trennung

Die Engine (`VoxelEngine.Engine.dll`) ist ein eigenständiges Class Library ohne spielspezifischen Code. Ein Spiel implementiert das `IGame`-Interface und übergibt sich selbst dem `EngineRunner`:

```csharp
// Program.cs
new EngineRunner().Run(new VoxelGame());
```

Der `EngineRunner` steuert den vollständigen Lifecycle:

```
RegisterBlocks(IBlockRegistry)
  → Initialize(IGameContext)
    → Update(deltaTime) + Render(deltaTime)  [Game Loop]
      → Shutdown()
```

Das Spiel kommuniziert mit der Engine ausschließlich über Interfaces (`IGameContext`, `IBlockRegistry`, `IWorldAccess`, `IInputState`) — nie direkt über Engine-Klassen.

### IGame-Interface

```csharp
public interface IGame {
    void RegisterBlocks(IBlockRegistry registry);
    void Initialize(IGameContext context);
    void Update(double deltaTime);
    void Render(double deltaTime);
    void Shutdown();
}
```

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

`IWorldPersistence` abstrahiert die Speicherebene. `LocalFilePersistence` schreibt binäre Region-Dateien (VXP4-Format). `InMemoryPersistence` dient für Tests. Dirty-Flag-System (`Chunk.PlayerEdits`, `IsDirty`) stellt sicher, dass nur veränderte Chunks geschrieben werden.

### Entity-System

`EntityManager` verwaltet Entities mit Spatial Hashing und Frustum-Culling. Entities nutzen eine Zustands-Maschine (Idle/Wander/Flee) mit tag-/nachtzeitabhängigem Verhalten. Spawn-Konfiguration erfolgt datengetrieben über Klimazonen-JSON.

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
dotnet run --project VoxelEngine.Game/VoxelEngine.Game.csproj
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
| Tab | Inventar öffnen/schließen |
| Escape | Pause-Menü |
| F1 | Debug-Konsole öffnen/schließen |

In der Konsole `help` eingeben für eine Liste aller Befehle.

## 📚 Dokumentation

| Dokument | Beschreibung |
|---|---|
| [Backlog.md](VoxelEngine.Engine/Backlog.md) | Offene Aufgaben & priorisierte Features nach Phasen |
| [Claude.md](VoxelEngine.Engine/Claude.md) | Architekturentscheidungen & Konventionen |
| [Docs/Rendering/AmbientOcclusion.md](VoxelEngine.Engine/Docs/Rendering/AmbientOcclusion.md) | Vertex-AO-Implementierung |
| [Docs/Rendering/Fog.md](VoxelEngine.Engine/Docs/Rendering/Fog.md) | Linearer Nebel: Parameter, Farbmischung |
| [Docs/Rendering/GreedyMeshing.md](VoxelEngine.Engine/Docs/Rendering/GreedyMeshing.md) | 3-Achsen-Sweep, UV-Kachelung |
| [Docs/Rendering/MultiThreading.md](VoxelEngine.Engine/Docs/Rendering/MultiThreading.md) | Producer-Consumer-Muster, GPU-Upload |
| [Docs/World/ClimateZones.md](VoxelEngine.Engine/Docs/World/ClimateZones.md) | Klimazonen (Temperatur, Feuchtigkeit, Köppen) |
| [Docs/World/WorldTime.md](VoxelEngine.Engine/Docs/World/WorldTime.md) | 24-Stunden-Uhr, Tageszyklus |

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
- Engine/Game-Trennung: `VoxelEngine.Engine` (DLL) + `VoxelEngine.Game` (EXE)
- `IGame`-Interface mit `EngineRunner`-Lifecycle
- Engine-Loop, Kamera, Eingabe, Shader-System (Shader als Embedded Resources)
- Chunk-Struktur, Weltgenerator, Noise-basiertes Terrain, Klimazonen
- Greedy-Meshing mit Ambient-Occlusion, Frustum-Culling, Diffuse-Beleuchtung
- Multithreading (Hintergrund-Generierung + Meshing), Chunk-Persistenz (VXP4)
- Debug-Konsole, HUD-Framework (JSON-konfigurierbar), Bitmap-Font
- Prozeduraler Himmel, Tageszyklus, Sonne/Mond/Sterne, Nebel, Transparenz
- Inventar (Hotbar, Drag & Drop, Equipment-Slots), Health/Hunger-System
- Entity-System mit Spatial Hashing, `.vox`-Rendering, Bewegungs-KI

**In Arbeit (Phase 7)**
- Block-Pickup (Abbauen → Inventar) und Dekrement beim Platzieren
- Verbleibende Entity/AI-Features