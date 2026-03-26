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
- ✅ Kommandos: help, pos, tp, wireframe, chunk info, renderdistance, skybox, time, fog
- ✅ Erweiterbare Command-Registry
- 🟡 Konsolen-History (Pfeiltasten blättern)
- 🟡 Autocomplete (Tab vervollständigt Kommando-Namen)
- 🟢 Konsolen-Output farbig
- 🟢 Kommandos aus Datei laden (Startup-Script)

### Kommandos die mit Features wachsen
- "weather sunny/rain/snow/storm"      → Wetter (Phase 4)
- "season spring/summer/autumn/winter" → Jahreszeit (Phase 4)
- "climate info"                       → Klimazone (Phase 3)
- "noise seed x"                       → Seed ändern (Phase 3)

---

## Phase 3 — Klimazonen & World Generation

### Klimazonen-Architektur
- 🔴 ClimateSystem (Temperatur + Feuchtigkeit pro Block-Position)
       Temperatur: primär Z-Koordinate (Breitengrad) + leichter Noise
       Feuchtigkeit: eigenständiger Noise-Wert
       Höhen-Einfluss: Berge kühlen Temperatur ab
- 🔴 ClimateZone Klasse (ersetzt BiomeDefinition)
       Felder: NoiseSettings, BlockTypes, TreeLine, SnowLine
       Lookup: (temperature, humidity) → ClimateZone
- 🔴 Übergangs-Interpolation zwischen Klimazonen (~20 Blöcke)

### Klimazonen (angelehnt an Erde)
- 🟡 Polarregion, Tundra, Taiga, Steppe
- 🟡 Gemäßigt, Mediterran, Savanne, Wüste, Tropen

### Höhenzonen
- 🟡 Küste (Y=64-68), Tiefland, Waldgrenze, Schneegrenze, Gipfelzone

### Gewässer — Generierung
- ✅ Wasser-Block (transparent, Two-Pass Rendering)
- ✅ Meeresspiegel Y=64
- 🟡 Seen (Mulden auffüllen)
- 🟡 Gletscher (Polar/Tundra)
- 🟢 Flüsse (Pfad-Algorithmus)
- 🟢 Oasen (Wüste)

### Gewässer — Simulation
- 🟡 Level-System (1-8), Cellular Automata
- 🟢 Volumen-Erhaltung, Strömung, Spieler-Interaktion

### Unterirdisches
- 🟡 Höhlen (3D Noise Density-Field)
- 🟡 Erzvorkommen pro Klimazone
- 🟢 Dungeons, Katakomben, Verlorene Städte

### Oberflächen-Strukturen
- 🟢 Bäume pro Klimazone (Schablone + L-System)
- 🟢 Ruinen, Dörfer, Burgen
- 🟢 Structure Seeds (Chunk-Grenzen übergreifend)

---

## Phase 4 — Rendering & Visuals

- ✅ ArrayTexture (Texture2DArray, 11 Schichten)
- ✅ Greedy Meshing
- ✅ Frustum Culling
- ✅ Ambient Occlusion (VertexAO, Diagonal-Flip, Z-Fighting Fix)
- ✅ Skybox (prozeduraler Gradient)
- ✅ WorldTime + SkyColorCurve (8 Keyframes)
- ✅ Sonne + Mond (Billboard, Mondphasen)
- ✅ Sterne (Instanced, Twinkle)
- ✅ Diffuse Beleuchtung (FaceLight, GlobalLight, SunColor)
- ✅ Fog (linear, FogColor aus Skybox, Tag/Nacht)
- ✅ Transparente Blöcke (Two-Pass, DepthMask)
- 🟡 Jahreszeiten (Farb-Tint, Tageslänge, DayCount vorhanden)
- 🟡 Wetter-Zustandsautomat (Sonnig→Bewölkt→Regen/Schnee)
- 🟡 Regen/Schnee Partikel
- 🟢 Wolken (prozedural)
- 🟢 Sonnen-Halo / Atmosphären-Streuung
- 🟢 Distant Horizons (LOD-Silhouetten, braucht Fog ✅)

---

## Phase 5 — Engine & Architektur

- ✅ Multithreading (Chunk-Generierung + Meshing im Background)
- 🔴 Block-Interaktion (Blöcke setzen und abbauen)
- 🔴 Dirty-Flag System (Chunk-Rebuild nur bei Änderung)
       Hinweis: SampleBlock() für prozedurale Welt korrekt —
       bei Block-Änderungen zusätzlich Dirty-Queue nötig
- ✅ Spieler-Entity (trennt Kamera von Spieler-Position)
- 🟡 Chunk-Serialisierung (Welt speichern und laden)
- 🟡 Block-Typen Registry (statt hardcodierter byte-Konstanten)
- 🟢 Asset-Management System
- 🟢 LOD (entfernte Chunks vereinfacht)

---

## Phase 6 — Gameplay & Simulation

- ✅ Spieler-Kollision mit Terrain
- ✅ Gravitation und Sprung
- 🟡 Wasser-Simulation (Cellular Automata, Level-System)
- 🟡 Inventar-System
- 🟢 Sound-System (OpenAL via Silk.NET)
- 🟢 Crafting-System
- 🟢 Mobs / NPCs

---

## Architektur-Notizen
- World/ bleibt frei von Silk.NET → Portabilität
- WorldTime ist zentrale Variable → Tageszeit, Wetter, Jahreszeit
- Klimazonen matchen nicht auf Chunk-Grenzen → Temperatur/Feuchtigkeit pro Block
- Wasser-Generierung und Simulation sind getrennte Systeme
- SampleBlock(): deterministisch für prozedurale Welt — Dirty-Queue für Block-Änderungen
- Spieler-Entity von Kamera trennen vor Phase 6
- GL-Calls nur im Main Thread — ChunkWorker produziert nur float[] Arrays