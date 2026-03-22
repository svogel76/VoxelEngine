# Konzept: Fog
Das technische Problem: Chunks die am Rand der Render Distance laden und entladen erzeugen einen harten sichtbaren Abbruch der Welt — die Welt "poppt" plötzlich auf.
Das ästhetische Problem: Eine Welt die einfach aufhört zu existieren sieht unrealistisch aus.
Die Lösung: Fog blendet die Welt sanft aus bevor sie abbricht — der Spieler sieht nie den harten Rand.
```
Nah:     volle Farbe
Mittel:  Farbe mischt sich langsam mit Fog-Farbe
Weit:    fast vollständig Fog-Farbe
Rand:    Chunk-Abbruch unsichtbar hinter Fog
```
## Die Formel
```glsl
float fogFactor = exp(-fogDensity * distance * distance);
// oder linear:
float fogFactor = 1.0 - clamp((distance - fogStart) / (fogEnd - fogStart), 0.0, 1.0);

vec3 finalColor = mix(fogColor, terrainColor, fogFactor);
```

Wir nutzen **lineare Fog** — einfacher zu konfigurieren:
```
fogStart = RenderDistance * 0.6  → Fog beginnt bei 60% der Sichtweite
fogEnd   = RenderDistance * 0.9  → vollständig eingenebelt bei 90%
```
Die elegante Verbindung zur Skybox
Die Fog-Farbe sollte identisch mit der Horizont-Farbe der Skybox sein — dann verschmilzt der Terrain-Rand nahtlos mit dem Himmel:
```csharp
fogColor = skybox.HorizonColor; // bereits vorhanden!
```

Das ist der Grund warum Fog in Voxel-Spielen so gut aussieht — Terrain geht nahtlos in Himmel über.

## Tageszeit-Einfluss

Da `HorizonColor` sich mit `WorldTime` ändert, ändert sich der Fog automatisch mit:
```
Mittag:       Fog hellblau   → klarer Tag
Sonnenunter:  Fog orange     → dramatischer Abenddunst
Nacht:        Fog dunkelblau → Nachtdunkel am Horizont
Morgenrot:    Fog rosa/rot   → Morgenstimmung
Kein extra Code nötig — es funktioniert automatisch.
```