# VoxelEngine - Projektkontext fuer Claude Code

## Projektziel
Voxel-Engine im Minecraft-Stil mit Silk.NET und OpenGL in C# (.NET 8).
Lernprojekt: Architekturentscheidungen werden im Chat besprochen,
Implementierung erfolgt in Claude Code.

## Architekturentscheidungen
- Fixed Timestep Game Loop (60 UPS, konfigurierbar via EngineSettings)
- EngineSettings als zentrale Konfigurationsklasse (init-Properties, keine Magic Numbers)
- OpenGL only - kein Multi-Backend vorerst
- Shader als externe .glsl Dateien unter Assets/Shaders/
- Kamera mit Yaw/Pitch, InvertMouseY-Option in EngineSettings
- Keine Magic Numbers - alles ueber EngineSettings konfigurierbar
- World/ hat keine Silk.NET Abhaengigkeiten - pure C# fuer Portabilitaet
- WorldTime ist zentrale Zeitvariable - alle Zeit-abhaengigen Systeme bauen darauf auf
- Multithreading: Chunk-Generierung + Mesh-Building im Background, GL-Uploads nur Main Thread
- SampleBlock(): deterministisches Meshing unabhaengig von Chunk-Ladereihenfolge
- Spieler-Entity trennt Position/Physik von der Kamera
- BlockRegistry ist die zentrale Quelle der Wahrheit fuer alle Block-Eigenschaften

## Projektstruktur
VoxelEngine/
|-- Assets/
|   |-- Fonts/
|   |   `-- font.png
|   |-- Hud/
|   |   `-- hud.json              # Externe HUD-Konfiguration (Anchor, Offset, ZOrder)
|   `-- Shaders/
|       |-- basic.vert            # MVP + UV + TileLayer + AO + FaceLight + Cutout
|       |-- basic.frag            # Beleuchtung + Fog + Alpha-Multiplier + Alpha-Cutout
|       |-- text.vert             # Orthografische 2D Projektion
|       |-- text.frag             # Font-Rendering mit discard
|       |-- skybox.vert           # Far Plane (pos.xyww)
|       |-- skybox.frag           # Gradient Zenith/Horizont/Boden
|       |-- celestial.vert        # Billboard Far Plane
|       |-- celestial.frag        # Sonne/Mond Textur + Opacity
|       |-- stars.vert            # Instanced Billboards
|       |-- stars.frag            # Twinkle Effekt
|       |-- highlight.vert        # Block-Highlight Wireframe
|       `-- highlight.frag        # Block-Highlight Farbe
|-- Core/
|   |-- Hud/
|   |   |-- HudAnchor.cs
|   |   |-- HudElementConfig.cs
|   |   |-- IHudElement.cs
|   |   `-- HudRegistry.cs
|   |-- Debug/
|   |   |-- Commands/
|   |   |   |-- ChunkInfoCommand.cs
|   |   |   |-- ClimateCommand.cs     # climate info
|   |   |   |-- FlyCommand.cs         # fly / fly on / fly off
|   |   |   |-- FogCommand.cs
|   |   |   |-- HelpCommand.cs
|   |   |   |-- PosCommand.cs
|   |   |   |-- ReachCommand.cs       # reach <n>
|   |   |   |-- RenderDistanceCommand.cs
|   |   |   |-- SkyboxCommand.cs
|   |   |   |-- TeleportCommand.cs
|   |   |   |-- TimeCommand.cs
|   |   |   |-- WireframeCommand.cs
|   |   |   `-- HudCommand.cs            # hud reload / toggle / list
|   |   |-- DebugConsole.cs
|   |   `-- ICommand.cs
|   |-- Engine.cs
|   |-- EngineSettings.cs
|   |-- FastNoiseLite.cs
|   |-- GameContext.cs
|   |-- GameLoop.cs
|   `-- InputHandler.cs
|-- Rendering/
|   |-- Hud/
|   |   |-- DebugHudElement.cs    # IHudElement fuer Debug-Overlay + Konsole
|   |   |-- DebugHudRenderer.cs   # IHudRenderer fuer Debug-Overlay
|   |   |-- HotbarHudElement.cs   # IHudElement fuer Hotbar-Snapshot
|   |   |-- HotbarHudRenderer.cs  # IHudRenderer fuer 9-Slot-Hotbar
|   |   |-- HudManager.cs         # Rendert alle HUD-Elemente nach ZOrder
|   |   |-- HudUtils.cs           # ResolveAnchor() Pixel-Berechnung
|   |   `-- IHudRenderer.cs       # Interface fuer HUD-Renderer
|   |-- ArrayTexture.cs           # Texture2DArray, Schichten aus BlockRegistry
|   |-- BitmapFont.cs
|   |-- BlockHighlightRenderer.cs # Wireframe-Highlight mit Depth-Test
|   |-- Camera.cs
|   |-- CelestialBody.cs          # Billboard Quad fuer Sonne/Mond
|   |-- CelestialTextures.cs      # Sonne + Mondphasen programmatisch
|   |-- ChunkRenderer.cs          # Chunk-Meshes + Ghost-Block Rendering (opaque/cutout/transparent)
|   |-- DebugOverlay.cs           # Duenner Wrapper um HudManager + DebugHudElement
|   |-- FrustumCuller.cs
|   |-- GreedyMeshBuilder.cs      # 3-Achsen-Sweep, NeedsFace, SampleBlock
|   |-- Mesh.cs                   # VAO/VBO/EBO, Stride 9 floats
|   |-- Renderer.cs
|   |-- Shader.cs
|   |-- Skybox.cs                 # Gradient + Sonne/Mond + Sterne
|   |-- SkyColorCurve.cs          # 8 Keyframes, AmbientLight, SunColor
|   |-- StarField.cs              # 1500 Sterne, Instanced Rendering
|   |-- TextRenderer.cs
|   `-- Texture.cs
`-- World/
    |-- BlockDefinition.cs        # Zentrale Block-Eigenschaften inkl. Tiles, Render-/Kollisions-Flags, Tags, MaxStackSize
    |-- BlockRaycaster.cs         # DDA Ray-Casting + PlacementPreview
    |-- BlockRegistry.cs          # Zentrale Registry aller BlockDefinitionen
    |-- BlockTextures.cs          # Kompatibilitaets-Shim fuer Tile-Lookups via Registry
    |-- BlockType.cs              # Byte-IDs + Kompatibilitaets-Shim via Registry
    |-- Chunk.cs
    |-- ChunkJob.cs               # Generate/Rebuild Jobs fuer ChunkWorker
    |-- ChunkManager.cs           # Laden/Entladen + Rebuild-Queue
    |-- ChunkResult.cs            # Fertige Mesh-Daten fuer GPU-Upload (opaque/cutout/transparent)
    |-- ChunkWorker.cs            # Background ThreadPool, ConcurrentQueues
    |-- ClimateSystem.cs          # Temperatur/Feuchtigkeit + Zonen-Blend
    |-- ClimateZone.cs            # Terrain- und Block-Definition pro Klimazone
    |-- CollisionAndGravity.md    # Konzeptnotiz fuer Spieler-Physik
    |-- FaceDirection.cs
    |-- NoiseSettings.cs
    |-- Inventory.cs              # ItemStack, Hotbar[9], TryAdd/Remove, SelectNext/Previous/Slot
    |-- Player.cs                 # Position, Velocity, FlyMode, Physik, Step-up, Inventory
    |-- RayCasting.md             # Konzeptnotiz fuer Block-Interaktion
    |-- StepUp.md                 # Konzeptnotiz fuer Step-up
    |-- TreeTemplate.cs           # Baum-Schablonen als 3D Block-Arrays mit Pivot
    |-- World.cs                  # ConcurrentDictionary, AddChunk, SampleBlock
    |-- WorldGenerator.cs         # ClimateSystem-basierte Terrain- und Baum-Generierung mit Tree-Cache
    `-- WorldTime.cs              # Time, DayCount, MoonPhase, TimeScale

## Koordinaten-System
- Chunk-Koordinate: Math.Floor(worldCoord / Chunk.Width)
- Lokal-Koordinate: ((worldCoord % Width) + Width) % Width
- Y hat keine Chunk-Unterteilung - Chunks gehen von Y=0 bis Y=255

## Vertex-Format
9 floats pro Vertex: x, y, z, u, v, tileLayer, ao, faceLight, cutout

## Physik-Konstanten (EngineSettings)
- Gravity, MaxFallSpeed, JumpVelocity
- StepHeight (1.0f), EnableStepUp
- PlayerHeight (1.8f), PlayerWidth (0.6f), EyeHeight (1.62f)

## Aktueller Stand
- [x] Engine-Grundstruktur mit Fixed Timestep Game Loop
- [x] EngineSettings als zentrale Konfiguration
- [x] Kamera mit Maus/Tastatur (WASD + Space/Shift, InvertMouseY)
- [x] InputHandler mit Raw Mouse Input
- [x] Shader-System (Shader.cs mit Fehlerpruefung)
- [x] Mesh-System (VAO/VBO/EBO, Stride 9 floats)
- [x] Texture-System (StbImageSharp)
- [x] FPS-Anzeige im Fenstertitel + HUD
- [x] MVP-Matrix Pipeline
- [x] Chunk-Datenstruktur (BlockType, Chunk, World, WorldGenerator)
- [x] Klima-basierte Terrain-Generation mit zonenspezifischen NoiseSettings
- [x] Greedy Meshing (3-Achsen-Sweep, NeedsFace, AO-korrekter Merge)
- [x] ArrayTexture (Texture2DArray, Schichten aus BlockRegistry)
- [x] Two-Pass Rendering (opaque + transparent, DepthMask)
- [x] Transparente Bloecke (Water=5, Glass=6, Ice=7)
- [x] Meeresspiegel Y=64
- [x] SampleBlock() - deterministisches Meshing
- [x] NeedsFace() - korrekte Flaechen-Logik fuer alle Block-Kombinationen
- [x] Backface Culling (CCW Winding Order)
- [x] GameContext (zentraler Container)
- [x] Bitmap Font System (CP437)
- [x] Debug-Konsole (F1, ICommand Interface)
- [x] HUD (FPS, Pos, Chunks, Verts, Time, Block, Reach)
- [x] Kommandos: help, pos, tp, wireframe, chunk info, renderdistance, skybox, time, fog, fly, reach, climate info
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
- [x] Ray-Casting ignoriert Wasser nur dann, wenn die EyePosition selbst in Wasser ist
- [x] Block-Highlight (Wireframe, Depth-Test, sichtbare Kanten)
- [x] Ghost-Block Platzierungs-Vorschau (halbtransparent)
- [x] Spieler-Entity (Player.cs, EyePosition, Kamera folgt Spieler)
- [x] Gravitation (Velocity.Y, MaxFallSpeed, konfigurierbar)
- [x] AABB-Kollision (X->Y->Z einzeln, IsOnGround)
- [x] Spieler-Kollision mit symmetrischer AABB, exakter Penetration und Sub-Steps
- [x] Sprung (Space, JumpVelocity, nur IsOnGround)
- [x] Step-up (StepHeight 1.0f, konfigurierbar)
- [x] Klimazonen-System (6 Zonen, Temperatur/Feuchtigkeit, sanftes Blending)
- [x] Neue Block-Typen (DryGrass, Snow, Wood, Leaves)
- [x] BlockRegistry als zentrale Quelle der Wahrheit fuer Block-Eigenschaften
- [x] Deterministische Baum-Generierung mit Templates pro Klimazone
- [x] Progressives Initial-Laden mit sichtbarem Fenster statt Blockieren im Load
- [x] Aggressiveres Initial-Laden mit mehr Uploads pro Frame und groesserem Start-Radius
- [x] Baum-Generierung ueber gecachte Startpositionen beschleunigt
- [x] Blaetter als Alpha-Cutout im separaten Cutout-Pass statt klassischem Transparent-Blend
- [x] Leaves kollidieren mit dem Spieler ueber separates CollidesWithPlayer-Flag
- [x] Wasser und Leaves rendern Innenseiten ohne Backface-Culling im Transparent-/Cutout-Pass
- [x] ChunkWorker-Dispose mit schnellerem Cancellation-Exit
- [x] Klima-Debug-Kommando (`climate info`)
- [x] Konsolen-History (Pfeiltasten) + Autocomplete (Tab)
- [x] Inventar-System (Hotbar, ItemStack, TryAdd/Remove, SelectNext/Previous/Slot)
- [x] HUD-Framework (HudRegistry, Anchor-System, hud.json, ZOrder-Rendering)
- [x] Hotbar-UI (9 Slots, Mausrad, Zifferntasten 1-9, HotbarHudRenderer)
- [ ] Wetter-System

## Anweisungen fuer Coding Agents

### Coding-Konventionen
- IDisposable konsequent implementieren
- Alle Ressourcen unter Assets/
- Keine Magic Numbers - alles ueber EngineSettings
- Unsafe-Bloecke nur wo OpenGL es erfordert
- Shader-Fehler werfen Exceptions mit InfoLog-Text
- World/ niemals Silk.NET importieren
- GL-Aufrufe NUR im Main Thread (niemals im ChunkWorker)
- Skybox vor Terrain rendern - DepthTest nach Skybox immer reaktivieren
- Chunk-Rebuild nach Block-Aenderung - Nachbar-Chunks an Grenzen ebenfalls

### Coding-Regeln die immer gelten
- Keine neuen Abhaengigkeiten in `World/` - niemals Silk.NET dort importieren
- Neue Block-Eigenschaften und Textur-Layer zentral in `BlockRegistry.cs` + `BlockDefinition.cs` pflegen
- Byte-IDs fuer existierende Bloecke nicht aendern - Chunk-Daten bleiben kompatibel
- Neue Debug-Kommandos immer in `Core/Debug/Commands/` als eigene Klasse
- Physik-Konstanten immer in `EngineSettings` - nie hardcoden
- GL-Aufrufe niemals im `ChunkWorker` - nur im Main Thread
- Nach Block-Aenderungen immer betroffene Chunks + Nachbar-Chunks rebuilden

### Struktur-Konventionen
- `World/` -> pure C#, keine Framework-Abhaengigkeiten
- `Rendering/` -> OpenGL + Silk.NET erlaubt
- `Core/` -> Engine-Infrastruktur, Silk.NET Window/Input
- `Assets/` -> alle externen Ressourcen (Shader, Fonts, Texturen)

### Nach jeder Implementierung
Aktualisiere die `CLAUDE.md` automatisch:
1. **Projektstruktur** - neue Dateien eintragen, geloeschte entfernen, Kommentare aktuell halten
2. **Aktueller Stand** - erledigte Punkte mit `[x]` markieren, neue Punkte hinzufuegen falls noetig
3. **Naechste Schritte** - abgearbeitete Punkte entfernen

## Naechste Schritte
1. Wetter-System
2. Block-Icons in Hotbar (ArrayTexture-Integration)
3. Chunk-Serialisierung (Speichern/Laden)
