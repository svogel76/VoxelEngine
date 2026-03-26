## Konzept: Ray-Casting

Block-Interaktion basiert auf einem **Ray-Cast** — wir schießen einen unsichtbaren Strahl von der Kamera in Blickrichtung und schauen welchen Block er zuerst trifft.

```
Kamera ──────────────────► [Block getroffen!]
       Blickrichtung
       max. Reichweite
```

Der eleganteste Algorithmus dafür ist der **DDA-Algorithmus** (Digital Differential Analyzer) — derselbe Algorithmus der in Wolfenstein 3D für Raycasting genutzt wurde:

```
Idee: Statt den Strahl in kleinen Schritten zu bewegen,
      berechnen wir exakt wann er die nächste
      Block-Grenze in X, Y oder Z kreuzt.

→ Sehr schnell, keine Floating-Point Fehler,
  trifft garantiert jeden Block
```

## Was wir brauchen

**1. RaycastResult** — was wurde getroffen?

```csharp
public record RaycastResult(
    Vector3Int BlockPosition,    // getroffener Block
    Vector3Int NormalPosition,   // Block davor (für setzen)
    float Distance               // wie weit entfernt
);
```

**2. Block-Highlight** — visuelles Feedback

Ein Wireframe-Würfel um den anvisierten Block — eigener Shader, kein Depth-Write damit er immer sichtbar ist:

```
┌─────────┐
│         │  ← dünne weiße Linien
│  Block  │     um den anvisierten Block
│         │
└─────────┘
```

**3. Chunk-Rebuild nach Änderung**

Wenn ein Block gesetzt oder entfernt wird:
```
world.SetBlock(x, y, z, blockType)
    → betroffenen Chunk als dirty markieren
    → ggf. Nachbar-Chunks an Grenzen auch rebuilden
    → ChunkWorker baut Mesh neu
```

## Die NormalPosition

Beim Setzen eines Blocks brauchen wir die Position **vor** dem getroffenen Block — dorthin kommt der neue Block:

```
Strahl trifft Block von oben:
Normal = (0, 1, 0)
NormalPosition = BlockPosition + Normal = Block darüber

→ neuer Block wird darüber gesetzt
```

## Welchen Block setzen?

Für den Anfang: eine einfache `SelectedBlock` Property am Spieler — per Mausrad oder Tastenkürzel wechselbar. Später kommt das Inventar.
