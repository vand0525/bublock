# RiftGameRules

Access to the game's KOTH (rift) scheduler on `CCitadelGameRules`, and the
two scheduler steps of the known-good rift sequence. Static; no state.
Extracted from Legacy `Koth` in Stage 10 (field names, values, and order
unchanged).

## Operations

| Member | Behavior |
|---|---|
| `TryResolve(out gameRules)` | First `citadel_gamerules` entity, then its `CCitadelGameRulesProxy.m_pGameRules` pointer. Returns `Found`, `ProxyMissing`, or `PointerNull` (pointer is `nint.Zero` unless `Found`) |
| `NextLocation` | `SchemaAccessor<Vector3>` for `CCitadelGameRules.m_vNextKothLocation` |
| `NextWindow` | `SchemaAccessor<float>` for `m_timeNextKothSpawnWindowTime` |
| `NextSpawn` | `SchemaAccessor<float>` for `m_timeNextKothSpawn` |
| `KothGiveUp` | `SchemaAccessor<float>` for `m_timeKothGiveUp` (read for logs only) |
| `ConfigureNextRift(gameRules, position)` | `citadel_koth_enabled 0`; set location; window 0; spawn 0; `citadel_koth_enabled 1`. The game's own KOTH system then creates the rift |
| `ParkScheduler(gameRules)` | window and spawn set to `ParkedTime` (999999); `citadel_koth_enabled 0` |
| `SetKothEnabled(enabled)` | `ServerConVars.TrySet("citadel_koth_enabled", 1 or 0)`; a missing convar logs one Warning on the `rift` log |

Accessors are created on each access, as the archive created them on each
`/koth` call.

## Dangerous constraints

- Preserve the order inside `ConfigureNextRift` and `ParkScheduler`; it is
  the known-good sequence. Do not change field names or the 999999 value.
- Writes go straight to native game memory through the gamerules pointer.
  Resolve it fresh for each rift; never cache it across map changes.
- Game-thread only.
