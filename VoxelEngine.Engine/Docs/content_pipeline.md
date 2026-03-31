# Content Pipeline — VoxelEngine

## Überblick

Eine Content Pipeline beschreibt den Weg von rohen Asset-Dateien
aus Kreativ-Tools bis hin zu spielbereiten Inhalten.

```
Raw Assets          Build Pipeline         Game-Ready Assets
──────────          ─────────────          ─────────────────
sheep.blend    →    Export + Convert   →   sheep.vox
grass.psd      →    Texture Atlas      →   textures.png
dragon.wav     →    Compress           →   dragon.ogg
sheep.json     →    Validate           →   sheep.json
```

---

## Tools pro Asset-Typ

### Texturen
| Tool | Kosten | Verwendung |
|------|--------|------------|
| **Aseprite** | ~$20 | Pixel-Art, ideal für 16×16 / 32×32 Block-Texturen |
| **LibreSprite** | Kostenlos | Aseprite-Fork, gleiche Funktionen |
| **Photoshop** | Abo | Professionell, für komplexe Texturen |
| **GIMP** | Kostenlos | Alternative zu Photoshop |

Empfehlung für Voxel-Spiel: **Aseprite** — Pixel-Art ist der
natürliche Stil, 16×16 Pixel pro Tile reichen vollständig.

### Voxel-Modelle (Entities)
| Tool | Kosten | Verwendung |
|------|--------|------------|
| **MagicaVoxel** | Kostenlos | Voxel-Modelle, exportiert .vox direkt |
| **Goxel** | Kostenlos | Alternative, Open Source |

Empfehlung: **MagicaVoxel** — passt perfekt zum Voxel-Stil,
.vox Dateien können mit Greedy-Meshing gerendert werden.

### 3D-Modelle (falls nicht Voxel)
| Tool | Kosten | Verwendung |
|------|--------|------------|
| **Blender** | Kostenlos | Vollständige 3D-Suite, Export als .obj/.gltf |

### Sound & Musik
| Tool | Kosten | Verwendung |
|------|--------|------------|
| **Audacity** | Kostenlos | Sound-Effekte aufnehmen + bearbeiten |
| **LMMS** | Kostenlos | Musik komponieren |
| **BeepBox** | Kostenlos | Einfache Chiptune-Musik im Browser |
| **Bfxr** | Kostenlos | Prozeduale Game-Sound-Effekte generieren |

### JSON-Definitionen
| Tool | Kosten | Verwendung |
|------|--------|------------|
| **VS Code** | Kostenlos | JSON Schema Validierung, sofortige Fehlermeldungen |

---

## Wie JSON und Assets zusammenhängen

Das JSON beschreibt **was** ein Inhalt ist.
Die Asset-Dateien beschreiben **wie** er aussieht und klingt.

```json
// Content/Entities/sheep.json
{
  "id": "sheep",
  "name": "Schaf",
  "model":       "sheep",       // → Content/Models/sheep.vox
  "texture":     "sheep_white", // → Eintrag im Texture Atlas
  "sound_idle":  "sheep_baa",   // → Content/Sounds/sheep_baa.ogg
  "sound_hurt":  "sheep_hurt",  // → Content/Sounds/sheep_hurt.ogg
  "sound_death": "sheep_death",
  "health": 8,
  "speed": 3.0,
  "components": ["physics", "passive_ai", "flees_player"],
  "drops": [
    { "item": "wool",   "chance": 1.0, "count": [1, 3] },
    { "item": "mutton", "chance": 1.0, "count": [1, 2] }
  ],
  "spawn": {
    "biomes": ["temperate", "savanna"],
    "time": "any",
    "minLight": 7,
    "maxPerChunk": 4
  }
}
```

Die JSON-Datei ist der **Klebstoff** zwischen Inhalt und Ressourcen.

---

## Ordner-Struktur

```
VoxelEngine/
├── Content/                    ← Game-Ready Assets (in Git)
│   ├── Blocks/
│   │   ├── grass.json
│   │   ├── stone.json
│   │   └── lava.json
│   ├── Entities/
│   │   ├── sheep.json
│   │   ├── wolf.json
│   │   └── dragon.json
│   ├── Biomes/
│   │   ├── temperate.json
│   │   └── desert.json
│   ├── Items/
│   │   ├── wool.json
│   │   └── mutton.json
│   ├── Recipes/
│   │   └── crafting_table.json
│   ├── Models/
│   │   ├── sheep.vox
│   │   └── wolf.vox
│   ├── Textures/
│   │   ├── grass_top.png
│   │   ├── sheep_white.png
│   │   └── (werden zu Atlas zusammengefasst)
│   └── Sounds/
│       ├── sheep_baa.ogg
│       └── hit.ogg
│
├── RawAssets/                  ← Quelldateien der Künstler (in Git)
│   ├── Models/
│   │   └── sheep.blend         ← Blender-Quelldatei
│   ├── Textures/
│   │   └── grass_top.aseprite  ← Aseprite-Quelldatei
│   └── Sounds/
│       └── sheep_baa.wav       ← unkomprimierte Quelldatei
│
└── Tools/                      ← Build-Scripts
    └── build_assets.ps1        ← PowerShell Build-Script
```

---

## Build-Stufen

### Stufe 1 — Jetzt (minimal, kein Build-Step)

Assets direkt in `Content/` ablegen, Spiel lädt sie direkt.

```
Aseprite → PNG exportieren → Content/Textures/ ablegen
MagicaVoxel → .vox exportieren → Content/Models/ ablegen
Audacity → .ogg exportieren → Content/Sounds/ ablegen
JSON schreiben → Content/Entities/ ablegen
→ Spiel starten → Inhalt sofort verfügbar
```

**Vorteil:** Kein Tooling nötig, sofort einsatzbereit.
**Nachteil:** Texturen noch kein Atlas — jede Textur einzelne Datei.

### Stufe 2 — Später (einfaches Build-Script)

Ein PowerShell oder Python Script läuft vor dem Spiel-Start:

```powershell
# Tools/build_assets.ps1

# 1. Texturen zu Atlas zusammenfassen
Write-Host "Packing textures..."
& "Tools/TexturePacker.exe" `
    --input "Content/Textures/" `
    --output "Content/textures_atlas.png" `
    --size 256

# 2. JSON Dateien gegen Schema validieren
Write-Host "Validating JSON..."
Get-ChildItem "Content/Entities/*.json" | ForEach-Object {
    & "Tools/jsonschema.exe" --schema "Schemas/entity.schema.json" $_.FullName
}

# 3. Audio konvertieren (falls .wav vorhanden)
Write-Host "Converting audio..."
Get-ChildItem "RawAssets/Sounds/*.wav" | ForEach-Object {
    $out = "Content/Sounds/" + $_.BaseName + ".ogg"
    & "ffmpeg" -i $_.FullName -codec:a libvorbis $out
}

Write-Host "Build complete!"
```

### Stufe 3 — Viel später (vollständige Pipeline)

```
Blender-Dateien automatisch exportieren
Asset-Hashing (nur geänderte Assets neu verarbeiten)
Binäre .pak Pakete (schnelleres Laden)
Hot-Reload (Assets während Laufzeit neu laden)
```

---

## MagicaVoxel → VoxelEngine (speziell)

Da die Engine Voxel-basiert ist passt MagicaVoxel perfekt:

```
MagicaVoxel:
  sheep.vox erstellen (voxeliges Schaf, z.B. 8×8×16 Voxel)
       ↓
Export: sheep.vox (Format direkt verwendbar)
       ↓
Entity JSON:
  "model": "sheep" → lädt sheep.vox
       ↓
VoxelModelRenderer:
  .vox Datei parsen → 3D Array von Block-IDs
  → dasselbe Greedy Meshing wie Chunks!
  → Model = Mini-Chunk
```

Das bedeutet: das bestehende Greedy-Meshing-System
kann **direkt für Entity-Modelle** wiederverwendet werden.

### .vox Dateiformat (vereinfacht)

```csharp
// World/Assets/VoxModel.cs
public class VoxModel
{
    public int SizeX { get; }
    public int SizeY { get; }
    public int SizeZ { get; }
    public byte[,,] Voxels { get; }  // Block-IDs, 0 = leer

    public static VoxModel Load(string path)
    {
        // MagicaVoxel .vox Format parsen
        // Bibliothek: https://github.com/nickvdyck/voxel
        // oder eigener Parser (~100 Zeilen)
    }
}
```

---

## ContentLoader — Laden beim Spielstart

```csharp
// Core/ContentLoader.cs
public class ContentLoader
{
    private readonly string _contentPath;

    public ContentLoader(string contentPath)
        => _contentPath = contentPath;

    public void LoadAll()
    {
        LoadBlocks();
        LoadItems();
        LoadEntities();
        LoadBiomes();
        LoadTextures();
        LoadModels();
        LoadSounds();
    }

    private void LoadBlocks()
    {
        foreach (var file in GetJsonFiles("Blocks"))
        {
            var def = JsonSerializer.Deserialize<BlockDefinition>(
                File.ReadAllText(file));
            BlockRegistry.Register(def);
        }
    }

    private void LoadEntities()
    {
        foreach (var file in GetJsonFiles("Entities"))
        {
            var def = JsonSerializer.Deserialize<EntityDefinition>(
                File.ReadAllText(file));
            EntityRegistry.Register(def);

            // Referenzierte Assets laden:
            if (def.Model != null)
                ModelRegistry.Load(def.Model,
                    Path.Combine(_contentPath, "Models"));
        }
    }

    private IEnumerable<string> GetJsonFiles(string subfolder)
        => Directory.GetFiles(
            Path.Combine(_contentPath, subfolder), "*.json");
}
```

---

## JSON Schema Validierung

Mit VS Code + JSON Schema bekommt der Content-Ersteller
sofortige Fehlermeldungen beim Tippen:

```json
// Schemas/entity.schema.json
{
  "$schema": "http://json-schema.org/draft-07/schema",
  "type": "object",
  "required": ["id", "name", "health"],
  "properties": {
    "id":     { "type": "string" },
    "name":   { "type": "string" },
    "model":  { "type": "string" },
    "health": { "type": "number", "minimum": 1 },
    "speed":  { "type": "number", "minimum": 0 },
    "components": {
      "type": "array",
      "items": {
        "type": "string",
        "enum": ["physics", "passive_ai", "hostile_ai",
                 "flying", "drops", "flees_player"]
      }
    }
  }
}
```

```json
// In jedem Entity JSON — VS Code erkennt das Schema:
{
  "$schema": "../../Schemas/entity.schema.json",
  "id": "sheep",
  ...
}
```

---

## Mod-Support als natürliche Konsequenz

Wenn die Content Pipeline data-driven ist, folgt Mod-Support fast automatisch:

```
Content/          ← Basis-Inhalte des Spiels
Mods/
  MyMod/
    Blocks/       ← neue Blöcke
    Entities/     ← neue Entities
    Textures/     ← neue Texturen
    mod.json      ← Mod-Manifest
```

```json
// Mods/MyMod/mod.json
{
  "id": "my_mod",
  "name": "My Awesome Mod",
  "version": "1.0.0",
  "requires": ["base_game >= 1.0"]
}
```

```csharp
// ContentLoader beim Start:
LoadAll("Content/");          // Basis-Inhalte
foreach (var mod in GetMods("Mods/"))
    LoadAll(mod.ContentPath); // Mod-Inhalte zusätzlich laden
```

Der Spieler kopiert einen Mod-Ordner hinein — fertig.
Kein Kompilieren, kein Code anfassen.

---

## Workflow für Content-Ersteller

```
Tag 1: Neue Entity erstellen
  1. MagicaVoxel öffnen → dragon.vox bauen → exportieren
  2. Aseprite öffnen → dragon_red.png Textur malen → exportieren
  3. Audacity → dragon_roar.wav aufnehmen → als .ogg exportieren
  4. VS Code → Content/Entities/dragon.json schreiben
  5. Spiel starten → Drache erscheint in der Welt

Kein C# Code, kein Kompilieren, keine Engine-Kenntnisse nötig.
```

---

## Empfohlene Einführungsreihenfolge

```
Jetzt:        Manuell — PNGs direkt in Content/Textures/
              JSON Dateien direkt in Content/Blocks/ etc.

Wenn Entities: MagicaVoxel für Voxel-Modelle
               ContentLoader für automatisches Laden

Wenn viele Texturen: Texture Packer Script (Stufe 2)

Viel später:  Vollständige Pipeline + Mod-Support
```

---

*Dokumentation zur VoxelEngine — Content Pipeline & Asset Management*
