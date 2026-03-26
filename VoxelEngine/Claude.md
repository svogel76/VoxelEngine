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
- WorldTime ist zentrale Zeitvariable — alle Zeit-abhängigen Systeme bauen darauf auf
- Multithreading: Chunk-Generierung + Mesh-Building im Background, GL-Uploads nur Main Thread
- SampleBlock(): deterministisches Meshing unabhängig von Chunk-Ladereihenfolge
- Spieler-Entity trennt Position/Physik von der Kamera

## Projektstruktur
VoxelEngine/
├── Assets/
│   ├── Fonts/
│   │   └── font.png
│   └── Shaders/
│       ├── basic.vert            # MVP + UV + TileLayer + AO + FaceLight
│       ├── basic.frag            # Beleuchtung + Fog + Alpha-Multiplier
│       ├── text.vert             # Orthografische 2D Projektion
│       ├── text.frag             # Font-Rendering mit discard
│       ├── skybox.vert           # Far Plane (pos.xyww)
│       ├── skybox.frag           # Gradient Zenith/Horizont/Boden
│       ├── celestial.vert        # Billboard Far Plane
│       ├── celestial.frag        # Sonne/Mond Textur + Opacity
│       ├── stars.vert            # Instanced Billboards
│       ├── stars.frag            # Twinkle Effekt
│       ├── highlight.vert        # Block-Highlight Wireframe
│       └── highlight.frag        # Block-Highlight Farbe
├── Core/
│   ├── Debug/
│   │   ├── Commands/
│   │   │   ├── ChunkInfoCommand.cs
│   │   │   ├── FlyCommand.cs         # fly / fly on / fly off
│   │   │   ├── FogCommand.cs
│   │   │   ├── HelpCommand.cs
│   │   │   ├── PosCommand.cs
│   │   │   ├── ReachCommand.cs       # reach <n>
│   │   │   ├── RenderDistanceCommand.cs
│   │   │   ├── SkyboxCommand.cs
│   │   │   ├── TeleportCommand.cs
│   │   │   ├── TimeCommand.cs
│   │   │   └── WireframeCommand.cs
│   │   ├── DebugConsole.cs
│   │   └── ICommand.cs
│   ├── Engine.cs
│   ├── EngineSettings.cs
│   ├── FastNoiseLite.cs
│   ├── GameContext.cs
│   ├── GameLoop.cs
│   └── InputHandler.cs
├── Rendering/
│   ├── ArrayTexture.cs           # Texture2DArray, 11 Schichten (inkl. Water/Glass/Ice)
│   ├── BitmapFont.cs
│   ├── BlockHighlightRenderer.cs # Wireframe-Highlight mit Depth-Test
│   ├── Camera.cs
│   ├── CelestialBody.cs          # Billboard Quad für Sonne/Mond
│   ├── CelestialTextures.cs      # Sonne + Mondphasen programmatisch
│   ├── ChunkRenderer.cs          # Chunk-Meshes + Ghost-Block Rendering
│   ├── DebugOverlay.cs           # HUD: FPS, Pos, Chunks, Verts, Time, Block, Reach
│   ├── FrustumCuller.cs
│   ├── GreedyMeshBuilder.cs      # 3-Achsen-Sweep, NeedsFace, SampleBlock
│   ├── Mesh.cs                   # VAO/VBO/EBO, Stride 8 floats
│   ├── Renderer.cs
│   ├── Shader.cs
│   ├── Skybox.cs                 # Gradient + Sonne/Mond + Sterne
│   ├── SkyColorCurve.cs          # 8 Keyframes, AmbientLight, SunColor
│   ├── StarField.cs              # 1500 Sterne, Instanced Rendering
│   ├── TextRenderer.cs
│   └── Texture.cs
└── World/
    ├── BlockRaycaster.cs         # DDA Ray-Casting + PlacementPreview
    ├── BlockTextures.cs          # Tile-Index pro BlockType + FaceDirection
    ├── BlockType.cs              # Air/Grass/Dirt/Stone/Sand/Water/Glass/Ice
    ├── Chunk.cs
    ├── ChunkJob.cs               # Generate/Rebuild Jobs für ChunkWorker
    ├── ChunkManager.cs           # Laden/Entladen + Rebuild-Queue
    ├── ChunkResult.cs            # Fertige Mesh-Daten für GPU-Upload
    ├── ChunkWorker.cs            # Background ThreadPool, ConcurrentQueues
    ├── CollisionAndGravity.md    # Konzeptnotiz für Spieler-Physik
    ├── FaceDirection.cs
    ├── NoiseSettings.cs
    ├── Player.cs                 # Position, Velocity, FlyMode, Physik, Step-up
    ├── RayCasting.md             # Konzeptnotiz für Block-Interaktion
    ├── StepUp.md                 # Konzeptnotiz für Step-up
    ├── World.cs                  # ConcurrentDictionary, AddChunk, SampleBlock
    ├── WorldGenerator.cs         # GenerateChunk() gibt Chunk zurück, SeaLevel=64
    └── WorldTime.cs              # Time, DayCount, MoonPhase, TimeScale

## Koordinaten-System
- Chunk-Koordinate:  Math.Floor(worldCoord / Chunk.Width)
- Lokal-Koordinate:  ((worldCoord % Width) + Width) % Width
- Y hat keine Chunk-Unterteilung — Chunks gehen von Y=0 bis Y=255

## Vertex-Format
8 floats pro Vertex: x, y, z, u, v, tileLayer, ao, faceLight

## Physik-Konstanten (EngineSettings)
- Gravity, MaxFallSpeed, JumpVelocity
- StepHeight (0.6f), EnableStepUp
- PlayerHeight (1.8f), PlayerWidth (0.6f), EyeHeight (1.62f)

## Aktueller Stand
- [x] Engine-Grundstruktur mit Fixed Timestep Game Loop
- [x] EngineSettings als zentrale Konfiguration
- [x] Kamera mit Maus/Tastatur (WASD + Space/Shift, InvertMouseY)
- [x] InputHandler mit Raw Mouse Input
- [x] Shader-System (Shader.cs mit Fehlerprüfung)
- [x] Mesh-System (VAO/VBO/EBO, Stride 8 floats)
- [x] Texture-System (StbImageSharp)
- [x] FPS-Anzeige im Fenstertitel + HUD
- [x] MVP-Matrix Pipeline
- [x] Chunk-Datenstruktur (BlockType, Chunk, World, WorldGenerator)
- [x] Perlin Noise Terrain-Generation mit NoiseSettings
- [x] Greedy Meshing (3-Achsen-Sweep, NeedsFace, AO-korrekter Merge)
- [x] ArrayTexture (Texture2DArray, 11 Schichten)
- [x] Two-Pass Rendering (opaque + transparent, DepthMask)
- [x] Transparente Blöcke (Water=5, Glass=6, Ice=7)
- [x] Meeresspiegel Y=64
- [x] SampleBlock() — deterministisches Meshing
- [x] NeedsFace() — korrekte Flächen-Logik für alle Block-Kombinationen
- [x] Backface Culling (CCW Winding Order)
- [x] GameContext (zentraler Container)
- [x] Bitmap Font System (CP437)
- [x] Debug-Konsole (F1, ICommand Interface)
- [x] HUD (FPS, Pos, Chunks, Verts, Time, Block, Reach)
- [x] Kommandos: help, pos, tp, wireframe, chunk info, renderdistance, skybox, time, fog, fly, reach
- [x] Chunk-Manager (dynamisches Laden/Entladen, Hysterese)
- [x] Multithreading (ChunkWorker, ConcurrentQueues, GL-Upload Main Thread)
- [x] Frustum Culling (Gribb-Hartmann, AABB-Test)
- [x] Ambient Occlusion (VertexAO, Diagonal-Flip)
- [x] Skybox (prozeduraler Gradient)
- [x] WorldTime (Time, DayCount, MoonPhase, TimeScale)
- [x] SkyColorCurve (8 Keyframes, SunColor, AmbientLight)
- [x] Sonne + Mond (Billboard Quads, Mondphasen)
- [x] Sterne (Instanced Rendering, Twinkle)
- [x] Diffuse Beleuchtung (FaceLight, uGlobalLight, uSunColor)
- [x] Fog (linearer Nebel, FogColor aus Skybox, Tag/Nacht)
- [x] Block-Interaktion (Ray-Casting DDA, Abbauen + Setzen)
- [x] Block-Highlight (Wireframe, Depth-Test, sichtbare Kanten)
- [x] Ghost-Block Platzierungs-Vorschau (halbtransparent)
- [x] Spieler-Entity (Player.cs, EyePosition, Kamera folgt Spieler)
- [x] Gravitation (Velocity.Y, MaxFallSpeed, konfigurierbar)
- [x] AABB-Kollision (X→Y→Z einzeln, IsOnGround)
- [x] Sprung (Space, JumpVelocity, nur IsOnGround)
- [x] Step-up (StepHeight 0.6f, konfigurierbar)
- [ ] Konsolen-History + Autocomplete
- [ ] Phase 3 — Klimazonen
- [ ] Inventar-System
- [ ] Wetter-System

## Anweisungen für Coding Agents

### Coding-Konventionen
- IDisposable konsequent implementieren
- Alle Ressourcen unter Assets/
- Keine Magic Numbers — alles über EngineSettings
- Unsafe-Blöcke nur wo OpenGL es erfordert
- Shader-Fehler werfen Exceptions mit InfoLog-Text
- World/ niemals Silk.NET importieren
- GL-Aufrufe NUR im Main Thread (niemals im ChunkWorker)
- Skybox vor Terrain rendern — DepthTest nach Skybox immer reaktivieren
- Chunk-Rebuild nach Block-Änderung — Nachbar-Chunks an Grenzen ebenfalls

### Coding-Regeln die immer gelten
- Keine neuen Abhängigkeiten in `World/` — niemals Silk.NET dort importieren
- Neue Block-Typen immer in `BlockType.cs` + `BlockTextures.cs` + `ArrayTexture.cs`
- Neue Debug-Kommandos immer in `Core/Debug/Commands/` als eigene Klasse
- Physik-Konstanten immer in `EngineSettings` — nie hardcoden
- GL-Aufrufe niemals im `ChunkWorker` — nur im Main Thread
- Nach Block-Änderungen immer betroffene Chunks + Nachbar-Chunks rebuilden

### Struktur-Konventionen
- `World/`      → pure C#, keine Framework-Abhängigkeiten
- `Rendering/`  → OpenGL + Silk.NET erlaubt
- `Core/`       → Engine-Infrastruktur, Silk.NET Window/Input
- `Assets/`     → alle externen Ressourcen (Shader, Fonts, Texturen)

### Nach jeder Implementierung
Aktualisiere die `CLAUDE.md` automatisch:
1. **Projektstruktur** — neue Dateien eintragen, gelöschte entfernen,
   Kommentare aktuell halten
2. **Aktueller Stand** — erledigte Punkte mit `[x]` markieren,
   neue Punkte hinzufügen falls nötig
3. **Nächste Schritte** — abgearbeitete Punkte entfernen

## Nächste Schritte
1. Phase 3 — Klimazonen (ClimateSystem, ClimateZone, Interpolation)
2. Konsolen-History + Autocomplete
3. Inventar-System
4. Wetter-System
