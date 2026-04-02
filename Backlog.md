# VoxelEngine - Backlog

## Legende
- [Hoch] blockiert andere Features
- [Mittel] wichtig fuer Spielgefuehl
- [Nice-to-have] Qualitaet und Polish
- [Erledigt]

---

## Mod/Plugin-System Meilenstein [Erledigt]

> Ziel: Das Spiel ist eine Mod. Engine und Mods kennen sich nur ueber VoxelEngine.Api.
> Alles andere baut darauf auf - Content, Behaviour, Story, externe Mods.

### Schritt 1 - VoxelEngine.Api extrahieren [Erledigt]
- [Erledigt] Neues Class Library Projekt `VoxelEngine.Api` ohne Implementierung
- [Erledigt] Bestehende Interfaces wandern hierher: `IGame` -> `IGameMod`, `IGameContext`,
  `IBlockRegistry`, `IWorldAccess`, `IInputState`
- [Erledigt] Neue Interfaces: `IGameMod`, `IModContext`, `IEntity`, `IComponent`, `IBehaviour`
- [Erledigt] Engine referenziert Api. Game referenziert Api. Beide kennen sich nur ueber Api.

### Schritt 2 - Component System [Erledigt]
- [Erledigt] `IComponent` als Basisinterface in Api
- [Erledigt] Konkrete Komponenten: `HealthComponent`, `PhysicsComponent`,
  `AIComponent`, `DropComponent`, `RenderComponent`
- [Erledigt] Entity-Definition in JSON referenziert Komponenten per Name
- [Erledigt] Ersetzt direkte Vererbungs-Hierarchie

### Schritt 3 - Behaviour Trees in JSON [Erledigt]
- [Erledigt] AI-Logik data-driven statt hardcodiertem Zustands-Automat
- [Erledigt] Bausteine: Conditions (`player_near`, `health_low`, `is_night`, `is_day`),
  Actions (`flee`, `wander`, `idle`)
- [Erledigt] Komposition in Entity-JSON
- [Erledigt] Neue Behaviours ohne Engine-Rebuild moeglich

### Schritt 4 - Mod-Loader + IGameMod [Erledigt]
- [Erledigt] Engine laedt DLLs zur Laufzeit aus `Mods/`-Ordner
- [Erledigt] `mod.json` Manifest pro Mod (id, name, version, dependencies)
- [Erledigt] `VoxelEngine.Game` ist die erste Mod-DLL (Ausgabe: `Mods/VoxelGame/`)
- [Erledigt] Mod-Reihenfolge und Abhaengigkeiten werden aufgeloest

### Schritt 5 - Launcher extrahieren [Erledigt]
- [Erledigt] Neues Executable `VoxelEngine.Launcher`
- [Erledigt] Laedt Engine, scannt Mods/, startet EngineRunner (2-Zeilen Bootstrap)
- [Erledigt] `VoxelEngine.Game` ist reine Class Library (kein Executable)
- [Erledigt] `VoxelEngine.Launcher` referenziert nur `VoxelEngine.Engine`

---

## Phase 2.5 - Debug und Entwicklungswerkzeuge

- [Nice-to-have] Konsolen-Output farbig
- [Nice-to-have] Kommandos aus Datei laden (Startup-Script)

### Kommandos die mit Features wachsen
- `weather sunny/rain/snow/storm` -> Wetter (Phase 4)
- `season spring/summer/autumn/winter` -> Jahreszeit (Phase 4)
- `noise seed x` -> Seed aendern
- `entity list / spawn / kill` -> Entity-System (Phase 7)

---

## Phase 3 - Klimazonen und World Generation

### Klimazonen-Feintuning
- [Mittel] Polarregion + Tundra (sehr kalt, Permafrost, Eis)
- [Mittel] Mediterran (warm/trocken, Kalkstein)

### Hoehenzonen
- [Mittel] Kueste (Y=64-68, Sand/Kies Uebergang)
- [Mittel] Waldgrenze (pro Klimazone unterschiedlich)
- [Mittel] Schneegrenze (pro Klimazone unterschiedlich)
- [Mittel] Gipfelzone (reiner Stein/Schnee)

### Gewaesser - Generierung
- [Mittel] Seen (Mulden auffuellen)
- [Mittel] Gletscher (Polar/Tundra Klimazonen)
- [Nice-to-have] Fluesse (Pfad-Algorithmus von Bergen zum Meer)
- [Nice-to-have] Oasen (Wasser in Wuesten-Klimazone)

### Gewaesser - Simulation
- [Mittel] Level-System (1-8), Cellular Automata
- [Nice-to-have] Volumen-Erhaltung, Stroemung
- [Nice-to-have] Spieler-Interaktion (Block entfernen leitet Wasser um)

### Unterirdisches
- [Mittel] Hoehlen (3D Noise Density-Field)
- [Mittel] Erzvorkommen pro Klimazone
- [Nice-to-have] Dungeons, Katakomben, Verlorene Staedte

### Oberflaechen-Strukturen
- [Nice-to-have] Ruinen, Doerfer, Burgen pro Klimazone
- [Nice-to-have] Schablonen-Format (externe Datei)

---

## Phase 4 - Rendering und Visuals

- [Erledigt] Atmosphaerischer Fog (Distanz-Softfade + Klima-/Tageszeit-/Hoehenabhaengigkeit, initial auf gemaessigte Zone getuned)

- [Mittel] Jahreszeiten (Farb-Tint, Tageslaenge - DayCount vorhanden)
- [Mittel] Wetter-Zustandsautomat (Sonnig -> Bewoelkt -> Regen/Schnee)
- [Mittel] Regen/Schnee Partikel
- [Nice-to-have] Wolken (prozedural, ziehen mit Wind)
- [Nice-to-have] Sonnen-Halo / Atmosphaeren-Streuung
- [Nice-to-have] Distant Horizons (LOD-Silhouetten)
- [Erledigt] Sky-Light-Propagation Teil 1 (Chunk-Light-Array + top-down/Flood-Fill + datengetriebene Block-Daempfung + Chunk-Randwerte)
- [Erledigt] Sky-Light-Propagation Teil 2 (Meshing nutzt Chunk-SkyLight * Richtungsfaktor; Mindest-Ambient via EngineSettings)
- [Mittel] Licht und Schatten (stimmigere Tageszeit-Kontraste, Schattenwirkung im Wald)
- [Mittel] Foliage-Polish (dichte Gras-/Blumen-/Buesch-Schicht fuer glaubhafte Waelder)
- [Nice-to-have] Billboard-Sprites fuer Vegetation (Gras, Blumen, Wippen im Wind)
- [Nice-to-have] Dynamische Sonnenschatten (Shadow Map Pass, Cascaded Shadow Maps) — abhängig vom Sonnenstand; aufwändiges Rendering-Feature, eigener Architektur-Entscheid erforderlich

---

## Phase 5 - Engine und Architektur [Erledigt]

- [Erledigt] Chunk-Serialisierung - `IWorldPersistence`, `LocalFilePersistence`,
  `InMemoryPersistence`, VXP5-Format
- [Erledigt] Projekttrennung in `VoxelEngine.Engine` (DLL) und `VoxelEngine.Game` (Exe)
- [Erledigt] `IGame`-Lifecycle eingefuehrt (`EngineRunner`, `IGame`, `IGameContext`)
- [Erledigt] Block-Definitionen data-driven (`Assets/Blocks/*.json`, `blocks.manifest.json`)
- [Erledigt] EngineSettings data-driven (`Assets/engine.json`)
- [Erledigt] Key Bindings data-driven (`Assets/keybindings.json`)
- [Erledigt] Component System (`IComponent`, Entity als Komposition)
- [Erledigt] Behaviour Trees (`IBehaviourNode`, `BehaviourRegistry`, JSON-driven)
- [Erledigt] Mod-Loader + `IGameMod` (`Mods/`-Ordner, `mod.json`, Lifecycle)
- [Erledigt] `VoxelEngine.Launcher` als eigenstaendiger Bootstrap
- [Erledigt] `VoxelEngine.Api` als oeffentliche Vertragsschicht
- [Nice-to-have] Asset-Management System
- [Nice-to-have] LOD (entfernte Chunks vereinfacht)

---

## Phase 6 - Gameplay und Simulation [Erledigt]

### UI und Menue
- [Erledigt] UI-Zustandsautomat - stack-basierter `UIStateManager`, `IUIPanel`, Escape-Logik
- [Erledigt] Spielmenue - Pause, Speichern, Beenden

### Inventar-System
- [Erledigt] Item-Icons in Hotbar (`IconRenderer`, batched Draw-Call)
- [Erledigt] Vollstaendiges Inventar-Fenster (4x9 Slots + Ausruestungs-Slots, Drag and Drop)
- [Nice-to-have] Crafting-System (Rezepte, Crafting-Tisch)
- [Nice-to-have] Werkzeug-Haltbarkeit

### Spieler-Erweiterungen
- [Erledigt] Gesundheits-System (HP, Schaden, Regeneration, Fallschaden)
- [Erledigt] Hunger-System (beeinflusst Regeneration, Verhungern)
- [Erledigt] Entity-Basisklasse (`Entity/`, Player erbt davon)
- [Nice-to-have] Erfahrungspunkte + Level
- [Nice-to-have] Spieler-Tod + Respawn

### Sound
- [Nice-to-have] Sound-System (OpenAL via Silk.NET)
- [Nice-to-have] Umgebungsgeraeusche pro Klimazone
- [Nice-to-have] Block-Sounds (abbauen, platzieren)
- [Nice-to-have] Spieler-Sounds (Schritte, Sprung, Landen)

---

## Phase 7 - Prozedurale Strukturen (Hybrid-Voxel-Ansatz)

> Ziel: Hochauflösende .vox-Strukturen werden prozedural generiert und als statische
> Geometrie in Chunks eingebettet. Terrain bleibt 1m-Voxel, Details (Gebäude, Ruinen,
> Felsen, Vegetation) werden als Sub-Voxel-Modelle (0.25m) platziert.

### Schritt 1 - Statische Vox-Platzierung in Chunks
- [ ] [Mittel] `IStaticStructure`-Interface in Api: `VoxModel Generate(StructureParams p)`
- [ ] [Mittel] `StaticStructureRenderer`: VoxModel → Mesh, in Chunk-Geometrie eingebacken
- [ ] [Mittel] Chunk-Serialisierung: platzierte Strukturen in VXP6-Format persistieren
- [ ] [Mittel] `StructureSpawnManager`: pro Chunk bei Generierung aufgerufen, Ergebnis gecacht

### Schritt 2 - RuinGenerator
- [ ] [Mittel] Basis-Formen: `Box()`, `Cylinder()`, `Arch()` als VoxModel-Builder
- [ ] [Mittel] Erosion-Pass: Simplex-Noise knabbert obere Schichten weg
- [ ] [Mittel] Loch-Pass: Kugel-Carving für Breschen in Wänden
- [ ] [Mittel] Scatter-Pass: Trümmer-Voxel rund um die Basis
- [ ] [Mittel] Vegetation-Pass: Moos auf Oberflächen, Efeu an Wänden
- [ ] [Nice-to-have] Style-Grammatik: Tower, Wall, Chapel, Archway als kombinierbare Typen

### Schritt 3 - Klimazonen-Parametrisierung
- [ ] [Mittel] `StructureParams` / `RuinParams`: Seed, Größe, Steinfarbe, ErosionLevel,
  MossChance, DebrisRadius, Style
- [ ] [Mittel] BiomeDefinition.json: `structures`-Array mit type/style/weight pro Klimazone
- [ ] [Nice-to-have] Wüste: Sandstein, wenig Moos, starke Erosion
- [ ] [Nice-to-have] Wald: Granit, viel Moos, Efeu, moderate Erosion  
- [ ] [Nice-to-have] Polar: Kalkstein, Schneeschicht oben, vereiste Risse

### Schritt 4 - Weitere Generatoren
- [ ] [Nice-to-have] `BoulderGenerator`: natürliche Felsformationen pro Klimazone
- [ ] [Nice-to-have] `TreeGenerator`: Bäume als Sub-Voxel-Modelle (filigraner als Block-Bäume)
- [ ] [Nice-to-have] `VegetationClusterGenerator`: Büsche, Farne, Pilze als Voxel-Cluster
- [ ] [Nice-to-have] `StoneWallGenerator`: Feldstein-Mauern, Wegbegrenzungen

## Phase 8 - Entity-System und Welt beleben

> Voraussetzung: Inventar-System [Erledigt]

### Entity-System Architektur
- [Erledigt] EntityManager (Frustum-Culling, Spatial Hashing)
- [Erledigt] Entity-Rendering (Voxel-Modelle + Entity-Atlas, batched)
- [Mittel] Entity-Kollision mit Terrain (AABB wie Spieler)
- [Erledigt] Entity-Spawning pro Klimazone
- [Nice-to-have] Entity-Persistenz
- [Nice-to-have] LOD fuer Entities

### Offene Gameplay-Items
- [Mittel] Block-Pickup (Abbauen -> Inventar)
- [Mittel] Dekrement beim Platzieren
- [Mittel] Fog-Command Inversion Bug

### Vegetation
- [Mittel] Gras + Blumen als Billboard-Sprites
- [Nice-to-have] Buesche, Pilze, Farne pro Klimazone

### Tiere
- [Mittel] Ressourcen droppen
- [Nice-to-have] Herden, Zucht

### NPCs und Feinde
- [Nice-to-have] NPC-Entity (Haendler, Dialog, Handel)
- [Nice-to-have] Feind-Entity (Aggro, Pathfinding, Kampf)
- [Nice-to-have] Dorf-Generierung

---

## Phase 9 - AI-gestützte Asset-Pipeline (Langfristig / Experimentell)

> Ziel: Referenzbilder von Strukturen (Ruinen, Gebäude, Felsen) werden per Vision-KI
> analysiert und in StructureParams übersetzt, die der prozedurale Generator direkt
> verwenden kann. Kein manuelles Parametrisieren mehr.

### Schritt 1 - Claude-Artifact: Image-to-Params Tool
- [ ] [Nice-to-have] Standalone-Tool (Claude Artifact): Bild hochladen → Vision-Analyse
  → `RuinParams`-JSON als Output
- [ ] [Nice-to-have] Analysierte Felder: Style, Proportionen, Materialfarben, ErosionLevel,
  MossChance, Dachtyp, Anbauten
- [ ] [Nice-to-have] Output direkt als JSON kopierbar für `BiomeDefinition.json`

### Schritt 2 - Integration in Build-Pipeline
- [ ] [Nice-to-have] CLI-Tool `vox-analyze`: Bild-Pfad rein, `StructureParams`-JSON raus
- [ ] [Nice-to-have] Batch-Modus: Ordner mit Referenzbildern → mehrere Params-JSONs
- [ ] [Nice-to-have] Optionaler Preview: Generator läuft direkt durch, Screenshot des
  erzeugten VoxModels als Vorschau

### Schritt 3 - Feedback-Loop
- [ ] [Nice-to-have] Params-JSON manuell nachbearbeiten, Generator neu ausführen
- [ ] [Nice-to-have] A/B-Vergleich: Referenzbild neben generiertem Voxel-Preview
- [ ] [Nice-to-have] Seed-Variation: ein Params-JSON → N verschiedene Variationen rendern

### Abhängigkeiten
- Setzt Phase 7 - Prozedurale Strukturen (Schritt 1+2) voraus
- Claude Artifact (Schritt 1) ist unabhängig entwickelbar — guter Einstiegspunkt

---

## Phase 10 - Multiplayer (optional, langfristig)

- [Nice-to-have] Option A: SpacetimeDB (C# Module, weniger Boilerplate, Free Tier)
- [Nice-to-have] Option B: ASP.NET Core + SignalR + MessagePack (volle Kontrolle)
- [Nice-to-have] Gemeinsam: Client-Side Prediction, Chunk-Streaming, Nametags, Chat

---

## Empfohlene Reihenfolge
```
Erledigt:    Mod/Plugin-System Meilenstein (Schritte 1-5)
Erledigt:    Phase 4 - Atmosphaerischer Fog
Jetzt:       Phase 4 - Atmosphaerische Ergaenzungen
               - Licht und Schatten
               - Foliage-Polish
Dann:        Phase 8 - offene Gameplay-Items
               - Block-Pickup (Abbauen -> Inventar)
               - Dekrement beim Platzieren
               - Fog-Command Inversion Bug
Langfristig: Phase 9 - Multiplayer
```



