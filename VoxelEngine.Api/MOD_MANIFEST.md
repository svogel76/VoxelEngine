# Mod Manifest (`mod.json`)

Each mod directory under `Mods/` must contain a `mod.json` file.

Example:

```json
{
  "id": "voxelgame",
  "name": "VoxelGame",
  "version": "1.0.0",
  "entry_class": "VoxelEngine.Game.VoxelGame",
  "dependencies": []
}
```

Fields:
- `id`: unique mod identifier (string)
- `name`: human-readable name (string)
- `version`: semantic version string
- `entry_class`: fully qualified type name implementing `IGameMod`
- `dependencies`: list of mod `id` values that must load before this mod
