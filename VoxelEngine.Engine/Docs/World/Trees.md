## Konzept: Baum-Generierung

Bäume sind **Strukturen** — kleine vordefinierte Block-Schablonen die der WorldGenerator an passenden Positionen in die Welt einfügt.

## Das Grundprinzip: Schablonen

```
Eiche (Schablone):        Fichte (Schablone):
        LLL                      L
       LLLLL                    LLL
       LLLLL                   LLLLL
        LLL                     LLL
         S                       L
         S                       S
         S                       S

L = Leaves (Blätter)
S = Stem (Stamm/Holz)
```

Jede Klimazone hat eigene Schablonen — das gibt der Welt sofort Charakter.

## Das wichtigste Problem: Chunk-Grenzen

Bäume können größer als ein Chunk sein — ein Baum an Position X=15 hat Blätter die in den nächsten Chunk ragen:

```
Chunk A          Chunk B
│    S            LLL│
│    S           LLLL│
│    S  →    →  LLLLL│
│    S           LLL │
│    S               │
```

Das `SampleBlock()` System das wir bereits haben löst das elegant — beim Meshing eines Chunks werden Nachbar-Chunks deterministisch gesamplet. Wir brauchen dasselbe für Bäume:

```csharp
// Deterministisch: gleicher Seed → gleicher Baum an gleicher Position
bool ShouldPlaceTree(int worldX, int worldZ, ClimateZone zone)
{
    var rng = new Random(HashSeed(worldX, worldZ, _worldSeed));
    return rng.NextSingle() < zone.TreeDensity;
}
```

## Neue Block-Typen

```
Wood   = 10  (Baumstamm — braun, alle Seiten gleich)
Leaves = 11  (Blätter — grün, leicht transparent)
```

Blätter sind **semi-transparent** — sie lassen Licht durch aber man sieht nicht komplett hindurch. Das ist eine neue Transparenz-Kategorie zwischen solid und komplett transparent.

## Schablonen pro Klimazone

```
Gemäßigt:  Eiche     — runder Blätter-Kopf, Stamm 4-6 hoch
Taiga:     Fichte    — spitzer Kegel, Schnee auf Ästen
Wüste:     Kaktus    — einfache Säule, kein Blätter-Block
Tropen:    Palme     — langer Stamm, Blätter nur oben
Savanne:   Akazie    — flacher Schirm, schräger Stamm
Steppe:    Kleiner Strauch — nur 1-2 Blöcke hoch
```

## Integration in WorldGenerator

```csharp
// Nach Terrain-Generierung, vor Wasser-Füllung:
foreach (var (worldX, worldZ) in chunkPositions)
{
    var climate = _climateSystem.Sample(worldX, worldZ);
    if (ShouldPlaceTree(worldX, worldZ, climate))
    {
        int surfaceY = GetSurfaceY(worldX, worldZ);
        var template = climate.GetTreeTemplate();
        PlaceStructure(template, worldX, surfaceY, worldZ, chunk);
    }
}
```
