# VoxelEngine — Projektkontext für Claude Code

## Projektziel
Voxel-Engine im Minecraft-Stil mit Silk.NET und OpenGL in C# (.NET 8).
Lernprojekt: Architekturentscheidungen werden im Chat besprochen,
Implementierung erfolgt in Claude Code.

## Architekturentscheidungen
- Fixed Timestep Game Loop (60 UPS, konfigurierbar via EngineSettings)
- EngineSettings als zentrale Konfigurationsklasse (init-Properties, keine Magic Numbers)
- OpenGL only — kein Multi-Backend vorerst
- Shader als externe .glsl Dateien unter Assets/Shaders/
- Kamera mit Yaw/Pitch, InvertMouseY-Option in EngineSettings
- Keine Magic Numbers — alles über EngineSettings konfigurierbar
- World/ hat keine Silk.NET Abhängigkeiten — pure C# für Portabilität

## Projektstruktur
VoxelEngine/
├── Assets/
│   ├── Fonts/
│   │   └── font.png              # CP437 Bitmap Font (16×16 ASCII Grid)
│   └── Shaders/
│       ├── basic.vert            # MVP-Transformation + UV-Koordinaten
│       ├── basic.frag            # Textur-Sampling
│       ├── text.vert             # Orthografische 2D Projektion
│       └── text.frag             # Font-Rendering mit discard
├── Core/
│   ├── Debug/
│   │   ├── Commands/
│   │   │   ├── ChunkInfoCommand.cs
│   │   │   ├── HelpCommand.cs
│   │   │   ├── PosCommand.cs
│   │   │   ├── RenderDistanceCommand.cs  # renderdistance <n>
│   │   │   ├── TeleportCommand.cs
│   │   │   └── WireframeCommand.cs
│   │   ├── DebugConsole.cs       # Command-Registry, History, Output-Log
│   │   └── ICommand.cs           # Interface: Name, Description, Usage, Execute
│   ├── Engine.cs                 # Hauptklasse, Silk.NET Window + Loop
│   ├── EngineSettings.cs         # Zentrale Konfiguration
│   ├── GameContext.cs            # Container für alle Systeme
│   ├── GameLoop.cs
│   └── InputHandler.cs
├── Rendering/
│   ├── AtlasTexture.cs           # 64×64 Atlas, 4×4 Tiles, programmatisch generiert
│   ├── BitmapFont.cs             # Font-Atlas Textur, UV-Berechnung pro Zeichen
│   ├── Camera.cs                 # Yaw/Pitch, View/Projection Matrix, WASD+Maus
│   ├── ChunkMeshBuilder.cs       # Naive Culling, Atlas-UVs, FaceDirection
│   ├── ChunkRenderer.cs          # Dictionary<(int,int), Mesh>, FrustumCuller
│   ├── DebugOverlay.cs           # HUD + Konsolen-Overlay, Chunks: X/Y
│   ├── FrustumCuller.cs          # Gribb-Hartmann, AABB-Test, LastVisibleCount
│   ├── Mesh.cs                   # VAO/VBO/EBO, DrawElements
│   ├── Renderer.cs               # Koordiniert ChunkRenderer, Shader, Texture
│   ├── Shader.cs                 # Kompilierung, Linking, Uniform-Setter
│   ├── TextRenderer.cs           # DynamicDraw VBO, DrawArrays, 2D Quads
│   └── Texture.cs                # Laden via StbImageSharp
└── World/
    ├── BlockTextures.cs          # Tile-Index pro BlockType + FaceDirection
    ├── BlockType.cs              # byte-Konstanten: Air=0, Grass=1, Dirt=2, Stone=3, Sand=4
    ├── Chunk.cs                  # 16×256×16 byte[,,], ChunkPosition, Get/SetBlock
    ├── ChunkManager.cs           # Update, ProcessLoadQueue, Hysterese
    ├── FaceDirection.cs          # Enum: Top, Bottom, Front, Back, Left, Right
    ├── NoiseSettings.cs          # Seed, Frequency, Octaves, Amplitude, BaseHeight
    ├── World.cs                  # Dictionary<(int,int),Chunk>, Koordinaten-Umrechnung
    └── WorldGenerator.cs         # GenerateTerrain, GenerateChunk, FastNoiseLite

## Koordinaten-System
- Chunk-Koordinate:  Math.Floor(worldCoord / Chunk.Width)
- Lokal-Koordinate:  ((worldCoord % Width) + Width) % Width
- Y hat keine Chunk-Unterteilung — Chunks gehen von Y=0 bis Y=255

## Aktueller Stand
- [x] Engine-Grundstruktur mit Fixed Timestep Game Loop
- [x] EngineSettings als zentrale Konfiguration
- [x] Kamera mit Maus/Tastatur (WASD + Space/Shift, InvertMouseY)
- [x] InputHandler mit Raw Mouse Input
- [x] Shader-System (Shader.cs mit Fehlerprüfung)
- [x] Mesh-System (VAO/VBO/EBO)
- [x] Texture-System (StbImageSharp + CreateFromBytes Fallback)
- [x] FPS-Anzeige im Fenstertitel
- [x] MVP-Matrix Pipeline (Model/View/Projection als Uniforms)
- [x] Chunk-Datenstruktur (BlockType, Chunk, World, WorldGenerator)
- [x] Perlin Noise Terrain-Generation mit NoiseSettings
- [x] Naive Culling Meshing (ChunkMeshBuilder)
- [x] ChunkRenderer — Welt wird gerendert
- [x] Backface Culling (CCW Winding Order, alle 6 Seiten verifiziert)
- [x] GameContext (zentraler Container für alle Systeme)
- [x] Bitmap Font System (CP437, UV-Berechnung, Orthografische Projektion)
- [x] Debug-Konsole (F1, Command-Registry, ICommand Interface)
- [x] HUD (FPS + Position + Chunks X/Y, immer sichtbar)
- [x] Kommandos: help, pos, tp, wireframe, chunk info, renderdistance
- [x] Chunk-Manager (dynamisches Laden/Entladen, Hysterese, MaxChunksPerFrame)
- [x] Textur-Atlas (AtlasTexture, programmatisch generiert, Nearest-Filtering)
- [x] BlockTextures + FaceDirection (Tile-Index pro Block-Typ und Fläche)
- [x] Frustum Culling (FrustumCuller, Gribb-Hartmann, AABB-Test)
- [ ] Konsolen-History (Pfeiltasten blättern)
- [ ] Autocomplete (Tab)
- [ ] Greedy Meshing
- [ ] Transparente Blöcke (Wasser)
- [ ] Ambient Occlusion

## Coding-Konventionen
- IDisposable konsequent implementieren
- Alle Ressourcen unter Assets/
- Keine Magic Numbers — alles über EngineSettings
- Unsafe-Blöcke nur wo OpenGL es erfordert
- Shader-Fehler werfen Exceptions mit InfoLog-Text
- World/ niemals Silk.NET importieren — Portabilität gewährleisten