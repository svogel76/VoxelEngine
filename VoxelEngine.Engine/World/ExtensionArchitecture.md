# Erweiterbarkeits-Architektur — VoxelEngine

## Das Problem: Hardcoded vs. Data-Driven

Aktuell sind viele Inhalte hardcodiert — jede Änderung erfordert
Code-Änderungen an mehreren Stellen:

```
Neuer Block heute:
  BlockType.cs        → neue Konstante
  BlockTextures.cs    → Tile-Index eintragen
  ArrayTexture.cs     → neue Textur-Schicht
  WorldGenerator.cs   → Verwendung einbauen
  = 4 Dateien anfassen
```

Das Ziel ist **Data-Driven Design** — Inhalte werden aus Daten geladen:

```
Neuer Block morgen:
  Content/Blocks/lava.json  → fertig
  = 0 Dateien im Code anfassen
```

---

## Die drei Säulen

### Säule 1: Registry Pattern

Zentrale Registrierung aller Inhalte — Lookup per ID statt Konstanten.

```csharp
// World/Registries/BlockRegistry.cs
public static class BlockRegistry
{
    private static readonly Dictionary<string, BlockDefinition> _byName = new();
    private static readonly Dictionary<byte, BlockDefinition>   _byId   = new();

    public static void Register(BlockDefinition def) { ... }
    public static BlockDefinition Get(string name) => _byName[name];
    public static BlockDefinition Get(byte id)     => _byId[id];
    public static bool IsTransparent(byte id)      => _byId[id].Transparent;
    public static bool IsSolid(byte id)            => _byId[id].Solid;
}

// World/Definitions/BlockDefinition.cs
public class BlockDefinition
{
    public byte   Id          { get; init; }
    public string Name        { get; init; } = "";
    public string TextureTop  { get; init; } = "stone";
    public string TextureSide { get; init; } = "stone";
    public string TextureBot  { get; init; } = "stone";
    public bool   Solid       { get; init; } = true;
    public bool   Transparent { get; init; } = false;
    public bool   Replaceable { get; init; } = false;  // Luft, Wasser
    public int    Luminance   { get; init; } = 0;       // Licht-Emission
    public float  Damage      { get; init; } = 0;       // Schaden pro Sekunde
    public string[] Tags      { get; init; } = [];      // "hot", "wet", "natural"
}
```

**Registrierung beim Start:**

```csharp
// Entweder im Code (Schritt 1):
BlockRegistry.Register(new BlockDefinition {
    Id = 1, Name = "grass",
    TextureTop = "grass_top", TextureSide = "grass_side",
    TextureBot = "dirt", Solid = true
});

// Oder aus JSON (Schritt 2, später):
BlockRegistry.LoadFromDirectory("Content/Blocks/");
```

---

### Säule 2: Data-Driven Content

Inhalte als JSON-Dateien — neue Inhalte ohne Kompilierung.

**Block-Definition:**

```json
// Content/Blocks/grass.json
{
  "id": 1,
  "name": "grass",
  "textureTop": "grass_top",
  "textureSide": "grass_side",
  "textureBottom": "dirt",
  "solid": true,
  "transparent": false,
  "tags": ["natural", "soil"]
}
```

```json
// Content/Blocks/lava.json  ← neuer Block, kein Code nötig
{
  "id": 20,
  "name": "lava",
  "textureTop": "lava",
  "textureSide": "lava",
  "textureBottom": "lava",
  "solid": false,
  "transparent": true,
  "luminance": 15,
  "damage": 4.0,
  "replaceable": true,
  "tags": ["hot", "liquid", "natural"]
}
```

**Entity-Definition:**

```json
// Content/Entities/sheep.json
{
  "id": "sheep",
  "name": "Schaf",
  "sprite": "sheep",
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

```json
// Content/Entities/dragon.json  ← neuer Feind, kein Code nötig
{
  "id": "dragon",
  "name": "Drache",
  "sprite": "dragon",
  "health": 200,
  "speed": 8.0,
  "components": ["flying", "hostile_ai", "fire_breath"],
  "drops": [
    { "item": "dragon_scale", "chance": 1.0,  "count": [2, 5] },
    { "item": "dragon_egg",   "chance": 0.05, "count": [1, 1] }
  ],
  "spawn": {
    "biomes": ["mountain", "taiga"],
    "time": "night",
    "minLight": 0,
    "maxPerChunk": 1
  }
}
```

---

### Säule 3: Component System für Entities

Statt Vererbungshierarchien nutzen wir Composition:

```
Problematisch (Vererbung):
Entity
  └── LivingEntity
        ├── Animal
        │     ├── PassiveAnimal
        │     │     ├── Sheep
        │     │     └── Cow
        │     └── HostileAnimal
        │           ├── Wolf
        │           └── Bear
        └── NPC
              ├── Trader
              └── Villager

Besser (Composition):
Entity = Id + List<IComponent>
  Sheep  = [Physics] + [Movement] + [Health] + [PassiveAI] + [Drops]
  Wolf   = [Physics] + [Movement] + [Health] + [HostileAI] + [Drops]
  Trader = [Physics] + [Movement] + [Health] + [FriendlyAI] + [Trade]
  Dragon = [Flying]  + [Movement] + [Health] + [HostileAI]  + [FireBreath] + [Drops]
```

**Interface:**

```csharp
// World/Components/IComponent.cs
public interface IComponent
{
    void Init(Entity entity, World world);
    void Update(Entity entity, World world, double deltaTime);
    void Dispose();
}
```

**Beispiel-Komponenten:**

```csharp
// World/Components/HealthComponent.cs
public class HealthComponent : IComponent
{
    public float MaxHealth { get; init; } = 20f;
    public float CurrentHealth { get; private set; }

    public void Init(Entity entity, World world)
        => CurrentHealth = MaxHealth;

    public void TakeDamage(float amount)
    {
        CurrentHealth -= amount;
        if (CurrentHealth <= 0) OnDeath();
    }

    public void Update(Entity entity, World world, double deltaTime) { }
    private void OnDeath() { /* Event feuern */ }
    public void Dispose() { }
}

// World/Components/PassiveAIComponent.cs
public class PassiveAIComponent : IComponent
{
    private float _wanderTimer;
    private Vector3 _targetDirection;

    public void Update(Entity entity, World world, double deltaTime)
    {
        _wanderTimer -= (float)deltaTime;
        if (_wanderTimer <= 0)
        {
            _targetDirection = RandomDirection();
            _wanderTimer = Random.Shared.NextSingle() * 5f + 2f;
        }
        entity.Position += _targetDirection * 2f * (float)deltaTime;
    }

    public void Init(Entity e, World w) { }
    public void Dispose() { }
}
```

**Entity als Container:**

```csharp
// World/Entity.cs
public class Entity
{
    public string   TypeId   { get; init; } = "";
    public Vector3  Position { get; set; }
    public Vector3  Velocity { get; set; }
    public List<IComponent> Components { get; } = new();

    public T? GetComponent<T>() where T : IComponent
        => Components.OfType<T>().FirstOrDefault();

    public void Update(World world, double deltaTime)
    {
        foreach (var component in Components)
            component.Update(this, world, deltaTime);
    }
}
```

---

## Einführungsreihenfolge

```
Phase 1 (jetzt):
  BlockRegistry einführen
  → ersetzt byte-Konstanten
  → Grundlage für Inventar
  → BlockType.cs bleibt als Kompatibilitäts-Shim

Phase 2 (mit Inventar):
  ItemRegistry
  → Items als eigene Definitionen
  → nicht jeder Item ist ein Block

Phase 3 (mit Entity-System):
  Component System
  EntityRegistry
  → neue Entities durch Kombination

Phase 4 (später):
  JSON-Loading für alle Registries
  → Content/Blocks/, Content/Entities/
  → System.Text.Json

Phase 5 (optional):
  Mod-Support
  → Mods/[Name]/Blocks/ automatisch laden
  → natürliche Konsequenz von Data-Driven
```

---

## Konkrete erste Änderung: BlockRegistry

Der erste Schritt ist die BlockRegistry — sie hat sofortigen Nutzen
und bricht keine bestehende Funktionalität:

```csharp
// Vorher in GreedyMeshBuilder:
if (block == BlockType.Air) continue;
bool transparent = BlockType.IsTransparent(block);

// Nachher:
if (block == BlockRegistry.Air.Id) continue;
bool transparent = BlockRegistry.Get(block).Transparent;
```

`BlockType.cs` bleibt als Kompatibilitäts-Shim erhalten:

```csharp
// World/BlockType.cs — bleibt aber delegiert an Registry
public static class BlockType
{
    public static byte Air   => BlockRegistry.Get("air").Id;
    public static byte Grass => BlockRegistry.Get("grass").Id;
    // ...
    public static bool IsTransparent(byte id)
        => BlockRegistry.Get(id).Transparent;
}
```

So funktioniert der bestehende Code weiter — und neuer Code
nutzt direkt die Registry.

---

## Warum das Mod-Fähigkeit ergibt

Wenn Inhalte Data-Driven sind entsteht Mod-Fähigkeit fast von selbst:

```
Normaler Content:   Content/Blocks/grass.json
Mod-Content:        Mods/MyMod/Blocks/magic_ore.json

Beim Start:
  1. Content/ laden → alle Basis-Blöcke registriert
  2. Mods/ scannen → alle Mod-Blöcke zusätzlich registriert
  3. Spiel startet mit allen Inhalten
```

Der Spieler kopiert einen Mod-Ordner hinein — fertig.
Keine Engine-Änderung, kein Kompilieren.

---

*Dokumentation zur VoxelEngine — Erweiterbarkeits-Architektur*
