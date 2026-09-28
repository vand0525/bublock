# GameDependencies

The game dependencies `SelfTestService` checks, as plain data. Pure; tested
by `SelfTestTests`.

## Data

- `ConVars`: `ConVarExpectation(Name, Expected, IfMissing, Owner)` for every
  convar Bublock sets (Lobby, Rift, GameLoop shop, CleanSlate). `Expected`
  null means the value changes at runtime (`citadel_koth_enabled`,
  `citadel_allow_purchasing_anywhere`). `IfMissing` is `Warn` for convars set
  through the console (`citadel_koth_warning_time`,
  `citadel_koth_early_warning_time`, `citadel_player_override_spawn_time`,
  which `ConVar.Find` may not see), `citadel_active_lane` and
  `citadel_crate_disable_early_spawn` (may not exist), and the three pause
  convars (`citadel_allow_pausing`, `citadel_allow_pause_in_match`,
  `citadel_pause_allow_in_pregame`, expected 0; a WARN on value after
  `/pause_allow on` is expected); `Fail` otherwise.
- `RequiredEntities`: must exist (`citadel_gamerules`).
- `KeptEntities`: map entities we rely on staying (`info_super_trooper_spawn`,
  `item_crate_spawn`, the two shop triggers, `info_koth_spawn_location`).
- `CleanedEntities`: what CleanSlate removes; should be 0 on the map.
- `Events`: counter names `EventCounters` tracks. `player_used_ability`
  feeds the stream camera's big-ult top-down (`Lobby/StreamCam`); a zero
  count after abilities were cast means the server does not fire it.
  `take_damage` guards the up-top damage block
  (`GameLoop/GameLoopPlugin.OnTakeDamage`); zero after a fight means the
  hook stopped firing and waiting players can be hurt again.
- `RiftPointName` = `info_koth_spawn_location`, `RiftPointTolerance` = 100
  units (a map rift point must be this close to `RiftSides.Position`).

## Invariants

- Every convar name the source sets (`ConVar.Find`, `ServerConVars.TrySet`,
  `ExecuteCommand`) must be in `ConVars` (test
  `Dependencies_ListEveryConvarTheSourceSets`, which scans the source).
- `CleanedEntities` equals `CleanSlateService.RemovedDesignerNames` (test).
- Expected values must match what the setters use (`LobbyService`,
  `CleanSlateService`); `CleanSlate` is a separate DLL, so the values are
  repeated here.
