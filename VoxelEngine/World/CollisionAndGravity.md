## Konzept: Kollision + Gravitation

Das ist physikalisch der interessanteste Schritt. Lass mich die Kernkonzepte erklären bevor wir den Prompt schreiben.

## Gravitation

Simpel — Velocity.Y wird jeden Frame um einen Gravitations-Wert reduziert:

```csharp
const float Gravity    = -24.0f;  // Blöcke/s²  (Minecraft: ~-28)
const float MaxFallSpeed = -50.0f;

// In Update():
if (!FlyMode && !IsOnGround)
    Velocity.Y = Math.Max(Velocity.Y + Gravity * deltaTime, MaxFallSpeed);

Position += Velocity * deltaTime;
```

## Kollision: AABB vs. Welt

Der Spieler hat eine Bounding Box (0.6 × 1.8 × 0.6). Wir prüfen ob diese Box mit soliden Blöcken überlappt.

Der wichtigste Trick: **Achsen einzeln auflösen**:

```
1. Bewege Spieler in X
2. Prüfe Kollision in X → korrigiere Position
3. Bewege Spieler in Y
4. Prüfe Kollision in Y → korrigiere Position + setze IsOnGround
5. Bewege Spieler in Z
6. Prüfe Kollision in Z → korrigiere Position
```

Warum einzeln? Wenn wir alle drei Achsen gleichzeitig auflösen entstehen Ecken-Bugs — der Spieler bleibt an Kanten hängen.## 

![alt text](aabb_kollision.svg)

Welche Blöcke sind solid?

```csharp
// Kollision nur gegen solide Blöcke
bool IsSolidForCollision(byte block) =>
    block != BlockType.Air &&
    block != BlockType.Water &&
    block != BlockType.Glass;  // Glas später optional
```

## Welche Blöcke prüfen?

Nicht alle Blöcke der Welt — nur die die die Bounding Box berühren könnten:

```csharp
// Alle Blöcke im AABB-Bereich prüfen:
int minX = (int)Math.Floor(aabb.Min.X);
int maxX = (int)Math.Floor(aabb.Max.X);
int minY = (int)Math.Floor(aabb.Min.Y);
// usw.

for (int x = minX; x <= maxX; x++)
for (int y = minY; y <= maxY; y++)
for (int z = minZ; z <= maxZ; z++)
    if (IsSolidForCollision(world.GetBlock(x,y,z)))
        ResolveCollision(aabb, x, y, z);
```

## Sprung

```csharp
// Nur wenn IsOnGround:
if (keyboard.IsKeyPressed(Key.Space) && IsOnGround)
{
    Velocity = Velocity with { Y = JumpVelocity };  // 8.0f
    IsOnGround = false;
}
```

## Head-Bob und Stepping

Zwei optionale aber schöne Details:
- **Step-up**: Spieler kann automatisch 1 Block hohe Stufen erklimmen ohne zu springen
- **Head-Bob**: leichte Kamera-Bewegung beim Laufen — kommt später
