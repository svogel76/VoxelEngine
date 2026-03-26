# VoxelEngine

> Eine prozedural generierte Voxel-Engine im Minecraft-Stil, gebaut mit C# und OpenGL (Silk.NET). Schwerpunkte: performantes Greedy-Meshing, dynamischer Tageszyklus mit Atmosphäre sowie multithreading-basierte Chunk-Verwaltung.

## 🗂 Projektstruktur

```
VoxelEngine/
├── VoxelEngine/                  # Hauptprojekt (C#)
│   ├── Assets/
│   │   ├── Shaders/              # GLSL-Shader (basic, skybox, celestial, stars, text)
│   │   └── Fonts/                # CP437-Bitmap-Font (font.png)
│   ├── Core/                     # Engine-Kern (Loop, Input, Konfiguration)
│   │   └── Debug/
│   │       └── Commands/         # 9 Debug-Konsolen-Befehle
│   ├── Rendering/                # OpenGL-Rendering (Kamera, Shader, Meshes, Skybox, HUD)
│   ├── World/                    # Voxel-Welt (Chunks, Generator, Multithreading)
│   ├── Docs/
│   │   ├── Rendering/            # Technische Dokumentation (AO, Fog, Meshing …)
│   │   └── World/                # Technische Dokumentation (Himmel, Zeit, Biome …)
│   ├── Backlog.md
│   ├── Claude.md
│   └── VoxelEngine.csproj
└── VoxelEngine.slnx              # Visual-Studio-Solution
```

## 🚀 Quickstart

**Voraussetzungen**
- .NET 10.0 SDK
- OpenGL-fähige Grafikkarte (OpenGL 3.3+)
- Visual Studio 2022 oder `dotnet`-CLI

**Bauen & Starten**

```bash
# Repository klonen
git clone <repo-url>
cd VoxelEngine

# Bauen
dotnet build VoxelEngine/VoxelEngine.csproj

# Starten
dotnet run --project VoxelEngine/VoxelEngine.csproj
```

**Steuerung**

| Taste | Aktion |
|---|---|
| W / A / S / D | Bewegen |
| Leertaste / Shift | Hoch / Runter |
| Maus | Umsehen |
| F1 | Debug-Konsole öffnen/schließen |
| Escape | Programm beenden |

In der Konsole `help` eingeben für eine Liste aller Befehle.

## 🏗 Architektur

### Chunk-System
Chunks haben eine feste Größe von 16 × 256 × 16 Blöcken. Ein `ChunkManager` verwaltet dynamisches Laden und Entladen um die Spielerposition (konfigurierbare `RenderDistance` / `UnloadDistance` mit Hysterese). Die Chunk-Generierung und Mesh-Erstellung laufen in einem Hintergrund-Thread-Pool (`ChunkWorker`); GPU-Uploads erfolgen ausschließlich im Haupt-Thread (max. 4 Uploads pro Frame).

### Rendering-Pipeline
1. **Skybox** – Prozeduraler Himmelsgradient mit 8-Keyframe-Farbkurve über 24 Stunden, Sonne/Mond als Billboard-Quads mit 8 Mondphasen, 1500 instanziert gerenderte Sterne mit Twinkling-Effekt.
2. **Opaque Pass** – Alle undurchsichtigen Blöcke mit aktiviertem Depth-Write und Frustum-Culling (Gribb-Hartmann-Methode).
3. **Transparent Pass** – Wasser, Glas, Eis ohne Depth-Write, nach-hinten-vorne sortiert, Alpha-Blending aktiviert.
4. **Debug-Overlay** – HUD (FPS, Position, Chunk-Zähler, Zeit) und interaktive Konsole.

### Greedy Meshing
Der `GreedyMeshBuilder` verwendet einen 3-Achsen-Sweep, der benachbarte, identische Flächen zu größeren Quads zusammenführt. Dabei werden Ambient-Occlusion-Werte (0–3 pro Vertex) und Diagonalflip zur Vermeidung von Streifenartefakten berücksichtigt. Texturen werden über ein `Texture2DArray` mit 11 Layern kachel-korrekt aufgebracht.

### Weltgenerierung
`WorldGenerator` verwendet FastNoiseLite mit OpenSimplex2-Rauschen. Basis-Terrain mit konfigurierter Höhe und Amplitude, Wasserfüllung bis zum Meeresspiegel (Y=64). `SampleBlock()` ermöglicht deterministisches Block-Sampling unabhängig von der Chunk-Ladereihenfolge.

### Beleuchtung & Atmosphäre
- **Ambient Occlusion**: Vertex-AO, in Greedy-Meshing integriert
- **Diffuse Lighting**: Richtungsabhängige Flächenhelligkeit (Top=100 %, Seiten=60–85 %, Boden=40 %)
- **Globales Licht**: Tageszyklus mit 8-Keyframe-Kurve (Nacht 3 % – Tag 100 % Ambient)
- **Nebel**: Linearer Nebel, konfigurierbar (Standard: 50–90 % der RenderDistance)

## 📚 Dokumentation

| Dokument | Beschreibung |
|---|---|
| [Backlog.md](VoxelEngine/Backlog.md) | Offene Aufgaben & priorisierte Features nach Phasen |
| [Claude.md](VoxelEngine/Claude.md) | Architekturentscheidungen & Konventionen für KI-Assistenz |
| [Docs/Rendering/AmbientConclusion.md](VoxelEngine/Docs/Rendering/AmbientConclusion.md) | Vertex-AO-Implementierung und Greedy-Merge-Überlegungen |
| [Docs/Rendering/DiffuseLighting.md](VoxelEngine/Docs/Rendering/DiffuseLighting.md) | Richtungsbasiertes Flächenlicht und globaler Lichtwert |
| [Docs/Rendering/Fog.md](VoxelEngine/Docs/Rendering/Fog.md) | Linearer Nebel: Parameter, Farbmischung mit Horizont |
| [Docs/Rendering/FrustumCulling.md](VoxelEngine/Docs/Rendering/FrustumCulling.md) | Gribb-Hartmann-Ebenenextraktion, AABB-Test, Performance |
| [Docs/Rendering/GreedyMeshing.md](VoxelEngine/Docs/Rendering/GreedyMeshing.md) | 3-Achsen-Sweep, UV-Kachelung, ArrayTexture-Lösung |
| [Docs/Rendering/MultiThreading.md](VoxelEngine/Docs/Rendering/MultiThreading.md) | Producer-Consumer-Muster, Hintergrundgenerierung, GPU-Upload |
| [Docs/Rendering/Transparency.md](VoxelEngine/Docs/Rendering/Transparency.md) | Zwei-Pass-Rendering, DepthMask, Sortierung |
| [Docs/World/ClimateZones.md](VoxelEngine/Docs/World/ClimateZones.md) | Geplante Klimazonen (Temperatur, Feuchtigkeit, Köppen-System) |
| [Docs/World/Nightsky.md](VoxelEngine/Docs/World/Nightsky.md) | Instanzierte Sterne, Marsaglia-Verteilung, Twinkling |
| [Docs/World/SkyBox.md](VoxelEngine/Docs/World/SkyBox.md) | Prozeduraler Himmelsgradient, View-Matrix-Trick |
| [Docs/World/WorldTime.md](VoxelEngine/Docs/World/WorldTime.md) | 24-Stunden-Uhr, 8-Keyframe-Farbkurve, Tag/Nacht |

## 🔧 Technologien

| Bibliothek | Version | Verwendung |
|---|---|---|
| [Silk.NET.OpenGL](https://github.com/dotnet/Silk.NET) | 2.23.0 | OpenGL-Bindings für Rendering |
| Silk.NET.Windowing | 2.23.0 | Fensterverwaltung, Game-Loop |
| Silk.NET.Input | 2.23.0 | Tastatur- und Mauseingabe |
| Silk.NET.Maths | 2.23.0 | Vektor- und Matrizenmathematik |
| [StbImageSharp](https://github.com/StbSharp/StbImageSharp) | 2.30.15 | Laden von Texturdaten |
| FastNoiseLite | (eingebettet) | OpenSimplex2-Rauschgenerierung |

Laufzeitumgebung: **.NET 10.0**, Sprache: **C# 13** mit `unsafe`-Blöcken und aktivierter Nullable-Analyse.

## 📋 Status & Roadmap

**Abgeschlossen (Phase 2–4)**
- Engine-Loop, Kamera, Eingabe, Shader-System
- Chunk-Struktur, Weltgenerator, Noise-basiertes Terrain
- Greedy-Meshing mit Ambient-Occlusion
- Dynamische Chunk-Verwaltung (Laden/Entladen mit Hysterese)
- Multithreading (Hintergrund-Generierung + Meshing)
- Frustum-Culling, Diffuse-Beleuchtung, Backface-Culling
- Debug-Konsole mit 9 Befehlen, HUD, Bitmap-Font
- Prozeduraler Himmel, Tageszyklus (24 h), Sonne/Mond/Sterne
- Nebel, Zwei-Pass-Transparenz (Wasser, Glas, Eis)

**Geplant**

| Phase | Feature | Priorität |
|---|---|---|
| 3 | Klimazonen & Biome (Temperatur, Feuchtigkeit, Köppen) | Hoch |
| 5 | Block-Interaktion (Platzieren/Abbauen, Dirty-Flag-System) | Hoch |
| 6 | Spieler-Entity (Kollision, Gravitation) | Hoch |
| 6 | Wasser-Simulation (Zelluläre Automaten) | Mittel |
| 6 | Höhlen (3D-Noise-Dichtefelder) | Mittel |
| 6 | Strukturen (Bäume, Ruinen) | Nice-to-have |
| – | Chunk-Serialisierung (Speichern/Laden) | Hoch |
| – | Sound-System (OpenAL via Silk.NET) | Nice-to-have |
