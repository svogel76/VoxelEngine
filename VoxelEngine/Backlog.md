# VoxelEngine — Backlog

## Legende
- 🔴 Hoch — blockiert andere Features
- 🟡 Mittel — wichtig für Spielgefühl
- 🟢 Nice-to-have — Qualität & Polish
- ✅ Erledigt

---

## Phase 2 — Welt & Terrain (aktuell)

- ✅ Chunk-Datenstruktur (16×256×16, Dictionary)
- ✅ Naive Culling Meshing
- ✅ Backface Culling
- ✅ Perlin Noise Terrain-Generation
- 🔴 Chunk-Manager — dynamisches Laden/Entladen um Spielerposition
- 🟡 Chunk-Mesh Rebuild bei Block-Änderung
- 🟡 Sichtweite konfigurierbar (render distance)

---

## Phase 2.5 — Debug & Entwicklungswerkzeuge
> Früh implementieren — vereinfacht alle weiteren Phasen erheblich

- 🔴 GameContext (zentraler Container für alle Systeme)
       Voraussetzung für mächtige Kommandos, räumt Engine.cs auf
- 🔴 Debug-Konsole (Overlay, Command-Parser, Registry)
       Öffnen mit § oder F1
- 🔴 Basis-Kommandos:
       "help"              → alle Kommandos auflisten
       "pos"               → Spieler-Position anzeigen
       "tp x y z"          → Teleportieren
       "fly"               → Fly-Modus umschalten
       "wireframe"         → Wireframe-Rendering toggle
       "chunk info x z"    → Chunk-Daten anzeigen
       "chunk rebuild"     → Alle Meshes neu generieren
       "render distance x" → Sichtweite ändern
- 🟡 Erweiterbare Command-Registry (ICommand Interface)
- 🟡 Konsolen-History (Pfeiltasten blättern durch letzte Kommandos)
- 🟡 Autocomplete (Tab vervollständigt Kommando-Namen)
- 🟢 Konsolen-Output (farbige Fehlermeldungen, Erfolgs-Meldungen)
- 🟢 Kommandos aus Datei laden (Startup-Script)

### Kommandos die mit Features wachsen
> Werden bei Implementierung des jeweiligen Features ergänzt
- "time 0-24"                      → Tageszeit (Phase 4)
- "weather sunny/rain/snow/storm"  → Wetter (Phase 4)
- "season spring/summer/autumn/winter" → Jahreszeit (Phase 4)
- "biome info"                     → Biom-Info (Phase 3)
- "noise seed x"                   → Seed ändern + Welt neu generieren (Phase 3)

---

## Phase 3 — Biome & World Generation

### Terrain-Grundformen
- 🔴 BiomeDefinition Klasse (NoiseSettings pro Biom)
- 🔴 Biome-Noise (niedrige Frequenz, bestimmt Biom-Verteilung)
- 🔴 Bilineare Interpolation zwischen Biomen an Grenzen
- 🟡 Meeresspiegel (fester Y-Wert, Wasser-Block unter dieser Höhe)

### Biome
- 🟡 Biom: Ebene (flach, Gras)
- 🟡 Biom: Hügel (mittlere Amplitude)
- 🟡 Biom: Berge (hohe Amplitude, Stein + Schnee an Gipfeln)
- 🟡 Biom: Wüste (flach, Sand statt Gras/Erde)
- 🟡 Biom: Ozean (unter Meeresspiegel, Sand/Kies am Boden)
- 🟢 Biom: Strand (Übergang Ozean/Land, Sand)
- 🟢 Biom: Sumpf (flach, nass, dunkles Gras)

### Gewässer — Generierung
- 🟡 Wasser-Block (transparent, eigene Rendering-Logik)
- 🟡 Meere (Terrain unter Meeresspiegel = Wasser aufgefüllt)
- 🟡 Seen (Mulden im Terrain werden aufgefüllt)
- 🟢 Flüsse (Pfad-Algorithmus von Bergen zum Meer)
- 🟢 Strände (Übergangs-Biom zwischen Ozean und Land)

### Gewässer — Simulation (Laufzeit)
> Erst sinnvoll wenn Block-Interaktion und transparentes Rendering stehen
- 🟡 Wasser-Block mit Level-System (1-8, simuliert Gefälle)
- 🟡 Cellular Automata Simulation
       Regeln: runter → seitlich → Gefälle folgen
- 🟢 Volumen-Erhaltung (Wasser verschwindet nicht)
- 🟢 Spieler-Interaktion (Block entfernen leitet Wasser um)
- 🟢 Strömung (Wasser-Blöcke haben Fließrichtung)

### Unterirdisches
- 🟡 Höhlen via 3D Noise (Density-Field Ansatz)
- 🟡 Erzvorkommen (Noise-Einschlüsse in Stein-Schichten)
- 🟢 Dungeons (prozedurale Räume, BSP-Baum Algorithmus)
- 🟢 Katakomben (unter Ruinen/Dörfern, verbunden mit Höhlen)
- 🟢 Antike Tunnel (lineare unterirdische Strukturen)
- 🟢 Verlorene Städte (große unterirdische Komplexe)
- 🟢 Minenschächte (lineare Strukturen tief im Gestein)

### Oberflächen-Strukturen

#### Natürlich
- 🟢 Bäume — Schablone pro Biom
       (Eiche, Fichte, Kaktus, Palme)
- 🟢 Bäume — L-System für organische Verzweigung
- 🟢 Felsen / Steinformationen (3D Noise)
- 🟢 Überhänge / Klippen (entstehen mit 3D Terrain-Noise)
- 🟢 Gletscher / Eisformationen (Bergbiome)

#### Zivilisation
- 🟢 Ruinen (Schablone + Verfall-Algorithmus)
       Typen: Turm, Mauer, Tempel, Haus
- 🟢 Dörfer (BSP-Baum, Straßennetz, Häuser pro Parzelle)
- 🟢 Burgen / Festungen (auf Berggipfeln)
- 🟢 Tempel / Pyramiden (geometrisch, Wüsten-Biom)
- 🟢 Alte Straßen / Pfade (Pfad-Algorithmus zwischen Punkten)
- 🟢 Hafen-Strukturen (nur wo Land auf Wasser trifft)

### Struktur-System Architektur
- 🟡 Structure Seeds (deterministisch, Chunk-Grenzen übergreifend)
- 🟡 Struktur-Registry (welche Struktur in welchem Biom?)
- 🟡 Terrain-Validierung vor Platzierung
       z.B. Dorf nur auf flachem Gras-Biom
- 🟢 Schablonen-Format (externe Datei statt hardcodierter Blöcke)

---

## Phase 4 — Rendering & Visuals

- 🔴 Textur-Atlas (verschiedene Texturen pro Block-Typ)
- 🟡 Greedy Meshing (Optimierung — große Quads statt viele kleine)
- 🟡 Frustum Culling (Chunks außerhalb Sichtfeld nicht rendern)
- 🟡 Transparente Blöcke (Wasser, Glas — eigener Render-Pass)
- 🟡 Ambient Occlusion (weiche Schatten an Block-Kanten)

### Tageszeit
- 🟡 Sonnen-Zyklus (Winkel → Lichtfarbe + Helligkeit)
- 🟡 Dynamische Skybox (Nacht/Tag/Sonnenauf-untergang)
- 🟡 Ambient Light System (Shader-Uniform)
- 🟢 Sterne (Nachthimmel)
- 🟢 Mond (Mondphasen)
- 🟢 Schatten-Richtung (mit Sonnenwinkel)

### Wetter
- 🟡 Wetter-Zustandsautomat
       (Sonnig → Bewölkt → Regen/Schnee → Aufklaren)
- 🟡 Regen-Partikel (Partikel-System)
- 🟡 Schnee-Partikel (Partikel-System)
- 🟡 Wetter-Wahrscheinlichkeit pro Biom
- 🟡 Dynamischer Fog (dichter bei Regen/Nebel)
- 🟢 Gewitter (Blitz-Effekte, Donner)
- 🟢 Nebel (morgens in Tälern und an Gewässern)
- 🟢 Schnee-Akkumulation (Schnee-Blöcke auf Oberflächen)
- 🟢 Wasser-Gefrieren (Seen im Winter)
- 🟢 Nasse Oberflächen (Shader-Effekt bei Regen)

### Jahreszeiten
- 🟡 Jahreszeiten-Zyklus (alle 90 Tage eine Saison)
- 🟡 Gras/Laub Farb-Tint pro Jahreszeit
       (Frühling: hellgrün, Sommer: sattgrün,
        Herbst: orange/rot, Winter: grau/weiß)
- 🟡 Tageslänge variiert mit Jahreszeit
- 🟢 Bäume verlieren Laub im Herbst/Winter
- 🟢 Blüten im Frühling
- 🟢 Wetter-Wahrscheinlichkeit ändert sich mit Jahreszeit

### Zeit-Architektur
- 🟡 WorldTime als zentrale Variable
       (TimeOfDay, DayCount, Year, Season)
- 🟡 Konfigurierbare Tages-/Jahres-Geschwindigkeit
       in EngineSettings
- 🟢 Zeitsteuerung per Debug-Konsole
       (Zeit beschleunigen, Tag/Nacht erzwingen)

### Distant Horizons
- 🟢 Heightmap-only Berechnung für entfernte Bereiche
       (Noise-Funktion aufrufen ohne Chunk zu laden)
- 🟢 LOD-Stufen (3 Schichten: voll / vereinfacht / Silhouette)
- 🟢 Übergangs-Nebel zwischen LOD-Stufen
- 🟢 Silhouetten-Mesh Generator
- 🟢 Nahtloser Übergang in Skybox
> Hinweis: Erst sinnvoll nach Chunk-Manager, Skybox und Fog

---

## Phase 5 — Engine & Architektur

- 🔴 Multithreading — Chunk-Generierung im Background-Thread
- 🔴 Block-Interaktion (Blöcke setzen und abbauen)
- 🟡 Chunk-Serialisierung (Welt speichern und laden)
- 🟡 Block-Typen Registry (statt hardcodierter byte-Konstanten)
- 🟢 LOD (Level of Detail — entfernte Chunks vereinfacht)
- 🟢 Asset-Management System
- 🟢 Multi-Backend Überlegung (Abstraktion für Vulkan)

---

## Phase 6 — Gameplay & Simulation

- 🔴 Spieler-Kollision mit Terrain
- 🔴 Gravitation und Sprung
- 🟡 Wasser-Simulation (Cellular Automata, Level-System)
- 🟡 Inventar-System
- 🟢 Crafting-System
- 🟢 Mobs / NPCs (aufwändig, eigenes System)

---

## Architektur-Notizen
- World/ bleibt frei von Silk.NET → Portabilität auf andere C# Frameworks
- Biome matchen nicht auf Chunk-Grenzen → BiomeWeights pro Block-Position
- NoiseSettings als eigene Klasse → später eine Instanz pro Biom
- Wasser-Generierung und Wasser-Simulation sind getrennte Systeme
- Flüsse: Pfad-Algorithmus zur Generierung, Cellular Automata zur Laufzeit
- Struktur-Seeds: deterministisch, Chunk-Grenzen übergreifend
- WorldTime: eine einzige Variable treibt Tageszeit, Wetter, Jahreszeit