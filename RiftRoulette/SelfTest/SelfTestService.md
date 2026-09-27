# SelfTestService

Checks every live game dependency Bublock relies on and reports PASS / WARN /
FAIL per check. Built for the day a Deadlock patch lands
(`reference/patch-day.md`).

## Operations

### `Run(mode)` (read-only)

Runs each area inside a guard (an exception becomes one `check threw` FAIL
and an Error with stack; the other areas still run):

| Area | Check | Status |
|---|---|---|
| Convars | each `GameDependencies.ConVars` entry: found, and value = expected | missing: `IfMissing`; wrong value: WARN |
| Schema | the internal `SchemaAccessor.Offset` (read by reflection) of `CCitadelGameRulesProxy.m_pGameRules` and the four `CCitadelGameRules` KOTH fields; then `RiftGameRules.TryResolve`; then reads `NextSpawn` / `KothGiveUp` | offset <= 0: FAIL; offset unreadable: WARN; resolve not Found: FAIL; non-finite timers: FAIL |
| Entities | `ByDesignerName` counts: required > 0, kept > 0, cleaned = 0 | required missing: FAIL; kept missing / cleaned left: WARN |
| RiftPoints | nearest `info_koth_spawn_location` to each `RiftSides` position within 100 | none found / too far: WARN |
| Heroes | `HeroBuildCatalog.SkippedHeroIds` empty; build data fetched within 14 days | WARN |
| Items | `ItemInfo.Exists` for every item in `hero-builds.json` (build items, category items, imbue items, components); banned items still exist | missing build items: FAIL (first 8 listed); banned gone: WARN |
| Floor | `Trace.Ray` from 32 above to 96 below each per-slot spot (`SlotSpots`) for both watch anchors and the four fight anchors, mask `Solid | PlayerClip | WorldGeometry` | any spot without floor: WARN |
| Players | per playing human: SteamID non-zero, pawn present, `HeroID` defined | SteamID 0: FAIL; else WARN |
| Events | `EventCounters.Count` > 0 for each `GameDependencies.Events` name | WARN (normal right after an upload) |

Logs every result to `selftest-*.log` (PASS Information, WARN Warning, FAIL
Error; Warning+ is copied to master) and `Self-test finished PASS n | WARN n |
FAIL n` to master.

### `Live(caller, player, timer, mode)`

Refused while a rift round runs, or when the player has no living hero.
Otherwise:

1. Loadout: `LoadoutService.Capture(pawn)` (read-only); no abilities read is
   a FAIL.
2. Hud: sends a `Self-test` banner to the player (PASS means sent; confirm on
   screen).
3. `WatchSpot.SendUp(player)` (teleport to the player's own watch spot +
   restrain), remembering whether the player was already restrained.
4. After 1 s: Teleport (pawn within 32 units of the spot), Restraint
   (`modifier_citadel_silenced` present, each `RestraintService.States` state
   set). Releases the restraint again if the player was not restrained
   before. Logs the results like `Run` and replies every line to the caller.

## Invariants and constraints

- `Run` changes nothing in the game. `Live` moves and restrains one player,
  like a normal send-up.
- The floor trace is unverified against the invisible skybox floor: compare
  with the pre-patch baseline run instead of treating a WARN as damage.
- Schema offset <= 0 as "not found" is an inference from
  `SchemaAccessor.Resolve` (it stores the native result's offset; real
  fields on these classes are never at 0).
- A cleaned entity that was renamed also reads 0 (PASS); `patch-check.py`
  compares the map dump counts to catch that.
