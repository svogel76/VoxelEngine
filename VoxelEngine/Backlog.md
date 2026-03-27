# VoxelEngine — Backlog

## Legende
- 🔴 Hoch — blockiert andere Features
- 🟡 Mittel — wichtig für Spielgefühl
- 🟢 Nice-to-have — Qualität & Polish
- ✅ Erledigt

---

## Phase 2 — Welt & Terrain ✅ Abgeschlossen

- ✅ Chunk-Datenstruktur (16×256×16, ConcurrentDictionary)
- ✅ Greedy Meshing (3-Achsen-Sweep, NeedsFace, AO-korrekter Merge)
- ✅ Backface Culling (CCW Winding Order)
- ✅ Perlin Noise Terrain-Generation
- ✅ Chunk-Manager (dynamisches Laden/Entladen, Hysterese)
- ✅ Multithreading (ChunkWorker, Background ThreadPool, GL Main Thread)
- ✅ SampleBlock() (deterministisches Meshing ohne Nachbar-Rebuilds)
- ✅ Sichtweite konfigurierbar (RenderDistance)

---

## Phase 2.5 — Debug & Entwicklungswerkzeuge

- ✅ GameContext (zentraler Container)
- ✅ Debug-Konsole (F1, ICommand Interface)
- ✅ Kommandos: help, pos, tp, wireframe, chunk info, renderdistance,
       skybox, time, fog, fly, reach, climate
- ✅ Erweiterbare Command-Registry
- 🟡 Konsolen-History (Pfeiltasten blättern)
- 🟡 Autocomplete (Tab vervollständigt Kommando-Namen)
- 🟢 Konsolen-Output farbig
- 🟢 Kommandos aus Datei laden (Startup-Script)

### Kommandos die mit Features wachsen
- "weather sunny/rain/snow/storm"       → Wetter (Phase 4)
- "season spring/summer/autumn/winter"  → Jahreszeit (Phase 4)
- "noise seed x"                        → Seed ändern (Phase 3)
- "entity list / spawn / kill"          → Entity-System (Phase 7)

---

## Phase 3 — Klimazonen & World Generation

### Klimazonen-Architektur
- ✅ ClimateSystem (Temperatur + Feuchtigkeit pro Block-Position)
- ✅ ClimateZone Klasse (6 Zonen: Wüste, Savanne, Steppe, Gemäßigt, Taiga, Tropen)
- ✅ Übergangs-Interpolation (cubic Smoothstep, ~80-100 Blöcke)
- ✅ DryGrass + Snow Block-Typen
- ✅ climate info Debug-Kommando

### Klimazonen-Feintuning (laufend)
- ✅ Breitere Übergänge (96 Blöcke, konfigurierbar)
- ✅ Zonen-Differenzierung (Amplitude, Frequenz, BaseHeight pro Zone)
- 🟡 Polarregion + Tundra (sehr kalt, Permafrost, Eis)
- 🟡 Mediterran (warm/trocken, Kalkstein)

### Höhenzonen
- 🟡 Küste (Y=64-68, Sand/Kies Übergang)
- 🟡 Waldgrenze (pro Klimazone unterschiedlich)
- 🟡 Schneegrenze (pro Klimazone unterschiedlich)
- 🟡 Gipfelzone (reiner Stein/Schnee)

### Gewässer — Generierung
- ✅ Wasser-Block (transparent, Two-Pass Rendering)
- ✅ Meeresspiegel Y=64
- 🟡 Seen (Mulden auffüllen)
- 🟡 Gletscher (Polar/Tundra Klimazonen)
- 🟢 Flüsse (Pfad-Algorithmus von Bergen zum Meer)
- 🟢 Oasen (Wasser in Wüsten-Klimazone)

### Gewässer — Simulation
- 🟡 Level-System (1-8), Cellular Automata
- 🟢 Volumen-Erhaltung, Strömung
- 🟢 Spieler-Interaktion (Block entfernen leitet Wasser um)

### Unterirdisches
- 🟡 Höhlen (3D Noise Density-Field)
- 🟡 Erzvorkommen pro Klimazone
- 🟢 Dungeons, Katakomben, Verlorene Städte

### Oberflächen-Strukturen
- 🟢 Ruinen, Dörfer, Burgen pro Klimazone
- 🟢 Structure Seeds (Chunk-Grenzen übergreifend)
- 🟢 Schablonen-Format (externe Datei)

---

## Phase 4 — Rendering & Visuals

- ✅ ArrayTexture (Texture2DArray, 13 Schichten inkl. DryGrass/Snow)
- ✅ Greedy Meshing
- ✅ Frustum Culling
- ✅ Ambient Occlusion (VertexAO, Diagonal-Flip)
- ✅ Skybox (prozeduraler Gradient)
- ✅ WorldTime + SkyColorCurve (8 Keyframes)
- ✅ Sonne + Mond (Billboard, Mondphasen)
- ✅ Sterne (Instanced, Twinkle)
- ✅ Diffuse Beleuchtung (FaceLight, GlobalLight, SunColor)
- ✅ Fog (linear, FogColor aus Skybox, Tag/Nacht)
- ✅ Transparente Blöcke (Two-Pass, DepthMask)
- 🟡 Jahreszeiten (Farb-Tint, Tageslänge — DayCount vorhanden)
- 🟡 Wetter-Zustandsautomat (Sonnig→Bewölkt→Regen/Schnee)
- 🟡 Regen/Schnee Partikel
- 🟢 Wolken (prozedural, ziehen mit Wind)
- 🟢 Sonnen-Halo / Atmosphären-Streuung
- 🟢 Distant Horizons (LOD-Silhouetten, braucht Fog ✅)
- 🟢 Billboard-Sprites für Vegetation (Gras, Blumen)
       Shader-Effekt: leichtes Wippen im Wind

---

## Phase 5 — Engine & Architektur

- ✅ Multithreading (Chunk-Generierung + Meshing im Background)
- ✅ Block-Interaktion (Blöcke setzen und abbauen)
- ✅ Spieler-Entity (trennt Kamera von Spieler-Position)
- ✅ AABB-Kollision + Gravitation + Sprung + Step-up
- 🔴 Dirty-Flag System (Chunk-Rebuild bei Block-Änderungen)
       SampleBlock() korrekt für prozedurale Welt —
       bei Spieler-Änderungen zusätzlich Dirty-Queue nötig
- 🟡 Chunk-Serialisierung (Welt speichern und laden)
- 🟡 Block-Typen Registry (statt hardcodierter byte-Konstanten)
- 🟢 Asset-Management System
- 🟢 LOD (entfernte Chunks vereinfacht)

---

## Phase 6 — Gameplay & Simulation

### Inventar-System
> Voraussetzung für Crafting, Ressourcen und spätere Entities

- 🔴 Inventar-Datenstruktur (Slots, Stack-Größe, Item-Typen)
- 🔴 Inventar-UI (Hotbar unten, Inventar-Fenster mit Tab)
       Hotbar: 9 Slots, aktuell ausgewählter Block hervorgehoben
       Inventar: 4×9 Slots
- 🔴 Item-Registry (alle platzierbaren Block-Typen als Items)
- 🟡 Aufsammeln von Blöcken ins Inventar beim Abbauen
- 🟡 Blöcke aus Inventar platzieren
- 🟡 Item-Icons (aus ArrayTexture generiert)
- 🟢 Crafting-System (Rezepte, Crafting-Tisch)
- 🟢 Ausrüstungs-Slots (Rüstung, Werkzeug)
- 🟢 Werkzeug-Haltbarkeit

### Spieler-Erweiterungen
- 🟡 Gesundheits-System (HP, Schaden, Regeneration)
- 🟡 Hunger-System (beeinflusst Regeneration)
- 🟢 Erfahrungspunkte + Level
- 🟢 Spieler-Tod + Respawn

### Wasser-Simulation
- 🟡 Level-System (1-8), Cellular Automata
- 🟢 Volumen-Erhaltung, Strömung
- 🟢 Spieler-Interaktion

### Sound
- 🟢 Sound-System (OpenAL via Silk.NET)
- 🟢 Umgebungsgeräusche pro Klimazone
- 🟢 Block-Sounds (abbauen, platzieren)
- 🟢 Spieler-Sounds (Schritte, Sprung, Landen)

---

## Phase 7 — Entity-System & Welt beleben

> Voraussetzung: Inventar-System sollte vorher stehen

### Entity-System Architektur
> Fundament für alles was sich bewegt — Bäume bis Feinde

- 🔴 Entity Basisklasse
       Position, Velocity, BoundingBox, Update(), Render()
       World/ pure C# — kein Silk.NET
- 🔴 EntityManager in GameContext
       Frustum-basiertes Culling (nur sichtbare Entities)
       Spatial Hashing für effiziente Nachbar-Suche
- 🔴 Entity-Rendering
       Billboard-Sprites (pixelig, passt zum Voxel-Stil)
       oder Voxel-Modelle (konsistenter aber aufwändiger)
- 🟡 Entity-Kollision mit Terrain (AABB wie Spieler)
- 🟡 Entity-Spawning pro Klimazone
- 🟢 Entity-Persistenz (Welt speichern behält Entities)
- 🟢 LOD für Entities (weit entfernte seltener updaten)

### Vegetation (Stufe 1 — statisch, keine KI)
> Größter visueller Effekt, geringste Komplexität

- 🔴 Baum-Generierung im WorldGenerator
       Schablonen pro Klimazone:
       Gemäßigt:  Eiche (runder Blätter-Kopf)
       Taiga:     Fichte (spitz, Schnee auf Ästen)
       Wüste:     Kaktus (einfache Geometrie)
       Tropen:    Palme (langer Stamm, Fächer-Blätter)
       Savanne:   Akazie (flacher Schirm)
- 🟡 Structure Seeds (Bäume Chunk-Grenzen übergreifend)
- 🟡 Gras + Blumen als Billboard-Sprites
- 🟢 Büsche, Pilze, Farne pro Klimazone
- 🟢 Gefällte Bäume droppen Holz-Items

### Tiere (Stufe 2 — einfache KI)
- 🟡 Tier-Entity Basisklasse
       Idle-Animation, zufällige Bewegung
       Flucht wenn Spieler zu nah
- 🟡 Tiere pro Klimazone
       Gemäßigt:  Schafe, Kühe, Hühner
       Taiga:     Wölfe, Hirsche, Bären
       Wüste:     Kamele, Schlangen
       Tropen:    Papageien, Affen
       Ozean:     Fische, Delfine
- 🟡 Ressourcen droppen (Fleisch, Wolle, Leder)
- 🟢 Tag/Nacht Verhalten (nachtaktive Tiere)
- 🟢 Herden-Verhalten (Schafe bleiben zusammen)
- 🟢 Tiere züchten (mit Futter)

### NPCs (Stufe 3 — komplexere KI)
- 🟢 NPC-Entity (Händler, Dorfbewohner)
- 🟢 Einfaches Dialogs-System
- 🟢 Handel (Items kaufen/verkaufen)
- 🟢 Dorf-Generierung (Häuser, Straßen, NPCs)
- 🟢 Tagesrhythmus (NPCs gehen nachts schlafen)

### Feinde (Stufe 3 — Kampf-KI)
- 🟢 Feind-Entity Basisklasse
       Aggro-Radius, Angriff, Schaden
- 🟢 Pathfinding (A* oder einfaches Steering)
- 🟢 Feinde nachts spawnen
- 🟢 Verschiedene Feind-Typen pro Klimazone
- 🟢 Kampf-System (Nahkampf, Fernkampf)
- 🟢 Beute droppen

### KI-Architektur
- 🟢 State Machine für einfache Tiere
- 🟢 Behavior Tree für NPCs/Feinde
- 🟢 Spatial Hashing für Nachbar-Suche

---

## Phase 8 — Multiplayer (optional, langfristig)

> Voraussetzungen bereits erfüllt:
> Spieler-Entity ✅ — Block-Interaktion ✅ — World/ frei von Silk.NET ✅

### Option A: SpacetimeDB
> Datenbank + Server in einem — C# Module passen zur Codebasis

**Vorteile:**
- Kein WebSocket-Boilerplate
- C# Reducer — World/ Logik direkt wiederverwendbar
- Automatische Client-Synchronisation
- Free Tier für Entwicklung, Self-Hosting möglich
- Bewährt in BitCraft (MMORPG)

**Nachteile:**
- Kein fertiges Voxel/Silk.NET Tutorial — Pionierarbeit
- Chunk-Daten groß → nur Diffs synchronisieren
- Noch nicht jahrzehntelang battle-tested
- Free Tier: DB pausiert nach 1 Woche Inaktivität
  (Pro $25/Monat löst das)

**Implementierung:**
- 🟢 SpacetimeDB C# Modul aufsetzen (lokal + Maincloud)
- 🟢 Spieler-Positionen synchronisieren
- 🟢 Block-Änderungen als Diffs propagieren
- 🟢 Andere Spieler als Entities rendern
- 🟢 Chat-System
- 🟢 Welt-Persistenz in SpacetimeDB

### Option B: Klassischer Server
> Volle Kontrolle — bewährte Patterns für Voxel-Spiele

**Vorteile:**
- Volle Kontrolle über Chunk-Streaming
- Jahrzehntelang erprobte Patterns
- Viele Referenz-Implementierungen

**Nachteile:**
- Mehr Boilerplate (WebSocket, Serialisierung)
- Separater Server-Prozess
- Hosting-Kosten (~$5-20/Monat VPS)

**Tech-Stack:**
- ASP.NET Core + SignalR (WebSocket)
- MessagePack (binäres Protokoll)
- SQLite/PostgreSQL für Persistenz

**Implementierung:**
- 🟢 ASP.NET Core Server mit SignalR
- 🟢 Spieler-Positionen synchronisieren
- 🟢 Chunk-Streaming (nur sichtbare Chunks)
- 🟢 Block-Änderungen propagieren
- 🟢 Andere Spieler als Entities rendern
- 🟢 Chat + Welt-Persistenz

### Gemeinsame Features (beide Optionen)
- 🟢 Client-Side Prediction (flüssige Bewegung trotz Latenz)
- 🟢 Kollisions-Authorität beim Server
- 🟢 Chunk-Sichtbarkeit pro Spieler
- 🟢 Nametags via BitmapFont

### Empfehlung
```
Kleine Gruppe (2-8 Spieler):
→ SpacetimeDB — weniger Aufwand, C# passt perfekt

Größere Community / volle Kontrolle:
→ ASP.NET Core + SignalR

Erst testen:
→ SpacetimeDB lokal — Self-Hosting als Fallback
```

---

## Empfohlene Gesamtreihenfolge
```
Aktuell:     Phase 3 abschließen (Klimazonen feintunen)
Nächste:     Phase 6 — Inventar (Hotbar + Aufsammeln)
Danach:      Phase 7 — Bäume + Vegetation (größter
             visueller Effekt bei geringstem Aufwand)
Dann:        Phase 5 — Chunk-Serialisierung (Welt speichern)
             Phase 7 — Tiere mit einfacher KI
Langfristig: Phase 8 — Multiplayer (optional)
```

> Faustregel: Inventar → Bäume → Tiere.
> Das sind die drei Schritte die die Welt von
> "leere Engine" zu "echtem Spiel" machen.

---

## Architektur-Notizen
- World/ bleibt frei von Silk.NET → Portabilität + Multiplayer-fähig
- WorldTime ist zentrale Variable → Tageszeit, Wetter, Jahreszeit, Entities
- Klimazonen matchen nicht auf Chunk-Grenzen → Temperatur/Feuchtigkeit pro Block
- Wasser-Generierung und Simulation sind getrennte Systeme
- SampleBlock(): deterministisch für prozedurale Welt
  → Dirty-Queue für Spieler-Block-Änderungen nötig
- Entity-System in World/ (pure C#) → später Multiplayer-fähig
- GL-Calls nur im Main Thread — ChunkWorker produziert nur float[] Arrays
- Inventar vor Entity-System — Items sind Voraussetzung für Drops

---

## Erweiterbarkeits-Architektur (Querschnittsthema)
> Betrifft alle Phasen — schrittweise einführen, nicht alles auf einmal

### Säule 1: Registry Pattern
> Zentrale Registrierung aller Inhalte — Lookup per ID statt Konstanten

- 🔴 BlockRegistry (ersetzt hardcodierte byte-Konstanten)
       BlockDefinition: Id, Name, Textures, Solid, Transparent,
       Luminance, Tags, Damage, Replaceable
       Lookup: string "grass" ↔ byte ID (intern weiterhin byte für Performance)
       Voraussetzung für: Inventar, Data-Driven Blocks, Modding
- 🟡 ItemRegistry (Items als eigene Definitionen, nicht nur Blöcke)
- 🟡 EntityRegistry (Entity-Typen zentral registriert)
- 🟡 BiomeRegistry (Klimazonen als registrierte Definitionen)
- 🟢 RecipeRegistry (Crafting-Rezepte zentral)

### Säule 2: Data-Driven Content
> Inhalte aus JSON-Dateien statt aus Code — neue Inhalte ohne Kompilierung

- 🟡 Content/Blocks/*.json (BlockDefinition aus JSON laden)
       Neuer Block = neue JSON-Datei, kein Code nötig
- 🟡 Content/Entities/*.json (EntityDefinition aus JSON)
- 🟡 Content/Biomes/*.json (ClimateZone aus JSON)
- 🟢 Content/Items/*.json
- 🟢 Content/Recipes/*.json
- 🟢 Hot-Reload (Dateien während Laufzeit neu laden)

### Säule 3: Component System für Entities
> Verhalten als austauschbare Teile — neue Entity-Typen durch Kombination

- 🟡 IComponent Interface (Update, Init, Dispose)
- 🟡 Basis-Komponenten:
       MovementComponent  → WASD-ähnliche Bewegung
       HealthComponent    → HP, Schaden, Tod
       PhysicsComponent   → Gravitation, Kollision
       AIComponent        → State Machine (Idle/Move/Flee/Attack)
       DropComponent      → Items beim Tod fallen lassen
       SpriteComponent    → Billboard-Sprite Rendering
- 🟡 Entity = Id + List<IComponent>
       Wolf:  [Physics] + [Movement] + [Health] + [AI_Hostile] + [Drops]
       Schaf: [Physics] + [Movement] + [Health] + [AI_Passive] + [Drops]
       Baum:  [Static]  + [Health]   + [Drops]  + [Choppable]
- 🟢 Komponenten aus JSON definierbar
       "components": ["physics", "hostile_ai", "drops"]

### Bonus: Mod-Fähigkeit
> Natürliche Konsequenz von Data-Driven + Registry

- 🟢 Mods/[ModName]/Blocks/ → automatisch beim Start geladen
- 🟢 Mods/[ModName]/Entities/ → neue Entities ohne Engine-Zugriff
- 🟢 Mods/[ModName]/Biomes/ → neue Klimazonen
- 🟢 Mod-Manifest (mod.json: Name, Version, Abhängigkeiten)

### Empfohlene Einführungsreihenfolge
```
Jetzt:        BlockRegistry (ersetzt byte-Konstanten)
              → direkt nützlich für Inventar
Mit Inventar: ItemRegistry
Mit Entity-System: Component System + EntityRegistry
Später:       Data-Driven JSON Loading
Viel später:  Mod-Support
```

---

## Content Pipeline & Asset Management
> Wie Artefakte aus Tools (Blender, Aseprite, Audacity) ins Spiel kommen

### Workflow
```
Tool → Raw Asset → (Build Script) → Game Asset → JSON referenziert Asset
```

Das JSON ist der Klebstoff zwischen Inhalt und Ressourcen:
```json
// Content/Entities/sheep.json
{
  "model":      "sheep",       // → Content/Models/sheep.vox
  "texture":    "sheep_white", // → Texture Atlas
  "sound_idle": "sheep_baa"    // → Content/Sounds/sheep_baa.ogg
}
```

### Empfohlene Tools
```
Texturen:      Aseprite (~$20, Pixel-Art) oder LibreSprite (kostenlos)
Voxel-Modelle: MagicaVoxel (kostenlos) → .vox direkt verwendbar
               → passt perfekt: dasselbe Greedy-Meshing wie Chunks!
3D-Modelle:    Blender (kostenlos) → .obj/.gltf Export
Sound-Effekte: Audacity (kostenlos) + Bfxr (prozedurale Sounds)
Musik:         LMMS oder BeepBox (beide kostenlos)
JSON-Editing:  VS Code + JSON Schema (sofortige Fehlerprüfung)
```

### Ordner-Struktur (Ziel)
```
VoxelEngine/
├── Content/              ← Game-Ready Assets (in Git)
│   ├── Blocks/           ← *.json Block-Definitionen
│   ├── Entities/         ← *.json Entity-Definitionen
│   ├── Biomes/           ← *.json Klimazonen-Definitionen
│   ├── Items/            ← *.json Item-Definitionen
│   ├── Recipes/          ← *.json Crafting-Rezepte
│   ├── Models/           ← *.vox Voxel-Modelle
│   ├── Textures/         ← *.png Texturen (→ Atlas)
│   └── Sounds/           ← *.ogg Audio
├── RawAssets/            ← Quelldateien der Künstler (in Git)
│   ├── Models/           ← *.blend Blender-Quelldateien
│   ├── Textures/         ← *.aseprite Quelldateien
│   └── Sounds/           ← *.wav unkomprimierte Quelldateien
└── Schemas/              ← JSON Schema für VS Code Validierung
    ├── block.schema.json
    ├── entity.schema.json
    └── biome.schema.json
```

### Stufe 1 — Jetzt (manuell, kein Build-Step)
- 🟡 Content/ Ordnerstruktur anlegen
- 🟡 ContentLoader (liest JSON + Assets beim Spielstart)
- 🟡 JSON Schema Dateien (VS Code Validierung)

### Stufe 2 — Später (einfaches Build-Script)
- 🟢 Texture Packer Script (einzelne PNGs → Texture Atlas)
- 🟢 JSON Validator Script (Schema-Check vor Build)
- 🟢 Audio Converter (.wav → .ogg via ffmpeg)

### Stufe 3 — Viel später (vollständige Pipeline)
- 🟢 MagicaVoxel .vox Parser
       VoxModel → Greedy Meshing (Wiederverwendung des Chunk-Systems)
- 🟢 Asset-Hashing (nur geänderte Assets neu verarbeiten)
- 🟢 Hot-Reload (Assets während Laufzeit neu laden)
- 🟢 Binäre .pak Pakete (schnelleres Laden)

### Mod-Support (Konsequenz aus Data-Driven)
- 🟢 Mods/ Ordner automatisch beim Start laden
- 🟢 Mod-Manifest (mod.json: Name, Version, Abhängigkeiten)
- 🟢 Load-Order (Mods überschreiben Basis-Inhalte kontrolliert)