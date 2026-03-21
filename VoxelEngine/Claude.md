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
│   └── Shaders/
│       ├── basic.vert        # MVP-Transformation + UV-Koordinaten
│       └── basic.frag        # Textur-Sampling
├── Core/
│   ├── Engine.cs             # Hauptklasse, besitzt Window/GL/Camera/Renderer/World
│   ├── EngineSettings.cs     # Zentrale Konfiguration (Window, Loop, Camera)
│   ├── GameLoop.cs           # Loop-Konstanten / Dokumentation
│   └── InputHandler.cs       # Maus (Raw) + Tastatur via Silk.NET IInputContext
├── Rendering/
│   ├── Camera.cs             # Yaw/Pitch, View/Projection Matrix, WASD+Maus
│   ├── ChunkMeshBuilder.cs   # Naive Culling: sichtbare Flächen → float[] vertices
│   ├── ChunkRenderer.cs      # Dictionary<(int,int), Mesh>, BuildMeshes, Render
│   ├── Mesh.cs               # VAO/VBO/EBO, DrawElements
│   ├── Renderer.cs           # Koordiniert ChunkRenderer, Shader, Texture
│   ├── Shader.cs             # Kompilierung, Linking, Uniform-Setter
│   └── Texture.cs            # Laden via StbImageSharp, oder CreateFromBytes()
└── World/
    ├── BlockType.cs          # byte-Konstanten: Air=0, Grass=1, Dirt=2, Stone=3
    ├── Chunk.cs              # 16×256×16 byte[,,], ChunkPosition, Get/SetBlock
    ├── World.cs              # Dictionary<(int,int),Chunk>, Koordinaten-Umrechnung
    └── WorldGenerator.cs     # GenerateFlat() — flache Testwelt

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
- [x] FPS-Anzeige im Fenstertitel (geglättet über 0.5s)
- [x] MVP-Matrix Pipeline (Model/View/Projection als Uniforms)
- [x] Chunk-Datenstruktur (BlockType, Chunk, World, WorldGenerator)
- [x] Flache Testwelt (5×5 Chunks, Grass/Dirt/Stone)
- [x] Naive Culling Meshing (ChunkMeshBuilder)
- [x] ChunkRenderer — Welt wird gerendert
- [ ] Echte Texturen pro Block-Typ (Textur-Atlas)
- [ ] Chunk-Manager (dynamisches Laden/Entladen)
- [ ] Perlin Noise Höhenkarte
- [ ] Face Culling (Backface Culling in OpenGL)

## Coding-Konventionen
- IDisposable konsequent implementieren
- Alle Ressourcen unter Assets/
- Keine Magic Numbers — alles über EngineSettings
- Unsafe-Blöcke nur wo OpenGL es erfordert
- Shader-Fehler werfen Exceptions mit InfoLog-Text
- World/ niemals Silk.NET importieren

## Nächste Schritte (Phase 2 Fortsetzung)
1. Perlin Noise Höhenkarte — erste echte Terrain-Generation
2. Chunk-Manager — dynamisches Laden um Spielerposition
3. Textur-Atlas — verschiedene Texturen pro Block-Typ
4. Backface Culling — GPU rendert Rückseiten nicht