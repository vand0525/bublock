# RiftService

Core rift operations: start a rift through the game's KOTH scheduler, watch
for the outcome, end the round, cancel, and clean up. Static; state lives
here, timers come from the calling plugin class (`ITimer`). Known-good
sequence; do not reorder. The two player-moving steps are handed in by the caller
(`RiftRoundSteps`, built by `Round/RoundFlow`), so this file has no Draft
or Movement dependency.

## Logs

`Rift` feature log (`rift-YYYYMMDD.log`). Ops take
`ExecutionMode mode = Clean`; Debug lines (give-up time, cash-in VData,
trooper counts) appear only in Debug mode. Master log gets
`Rift round started Side=`, `Rift spawned Side=`,
`Rift round ended Outcome= TroopersRemoved=`, and `Rift cancelled Phase=`;
the gamerules Errors and the timeout Warning are copied to master by the
logger.

## Types

- `RiftSnapshot(Spawners, Cashins)`: the `citadel_item_koth_spawner` and
  `citadel_koth_cashin` entity indexes on the map just before a rift is
  forced.
- `RiftPhase`: `Idle`, `WaitingForSpawn`, `Live` (spawned, watching),
  `Ending` (3 s end timer).
- `RiftRoundSteps(MoveTeamsToRift, ReturnPlayersToDraft, RoundEnded)`: the
  caller's steps. `MoveTeamsToRift(side)` runs right after the scheduler is
  parked; `ReturnPlayersToDraft()` runs at round end or cancel, before
  trooper cleanup, and returns how many players moved. `RoundEnded(result)`
  runs last on every ending path (finished, tied, cancelled,
  spawn timed out), after the phase is back to `Idle`, with a
  `RiftRoundResult` (outcome, side, winner team). An exception in it is
  logged as Error and does not affect the rift state.

## State

| Member | Meaning |
|---|---|
| `NextSide` | Side of the next rift; starts Green |
| `MiddleEnabled` | When true (default), rotation includes Center; `/rift_mid` toggles it |
| `Phase` | see `RiftPhase` |
| `CurrentSide` | Side of the running rift, or null |
| `LastOutcome` | `none`, `finished`, `tied`, `spawn timed out`, or `cancelled` |
| trooper snapshot | `npc_trooper` entity indexes present when the last rift started; used only by `WatchOutcome` to spot the first new trooper |
| handles | `IHandle`s of the spawn wait, the watch, and the end timer, cancelled by `CancelRift` |
| round counter | `BublockLog.RoundId` is `r<n>` from start until the round finishes, times out, or is cancelled. `RoundNumber` (read-only) is that `n`: 0 before the first round, only goes up during a DLL load |

## Operations

| Op | Behavior | Returns |
|---|---|---|
| `RunRift(timer, steps, mode)` | Refuses if a rift is running. Else, in this order (known-good; do not reorder): log `Forcing Rift`; resolve gamerules (Error and stop if missing or null); take `NextSide`; snapshot spawners, cash-ins and troopers; start the round; log `Spawning rift` (with `Existing=`, the spawners already on the map, and `Cashins=`, the cash-ins already on the map); `ConfigureNextRift`; then each tick `WaitForSpawner` | reply line |
| `WaitForSpawner` (private, per tick) | A `citadel_item_koth_spawner` not in the snapshot appears: its side is `RiftSides.TryMatch(position)` (the requested side if it matches neither); log spawned (expected vs actual position, index), then `GoLive`. Else, when the snapshot had a cash-in (a leftover rift, adopted on the first tick) or at `step.Run >= 320`, a rift already on the map (`FindRiftOnMap`): Warning `No new rift spawned, using the one on the map` (side, requested, entity designer name, index, position, runs waited), then `GoLive` with that rift's side. Otherwise at `step.Run >= 320`: `ParkScheduler`, Warning `Rift spawn timed out` (side not flipped), round finishes as `spawn timed out` | — |
| `FindRiftOnMap(requested)` (private) | First a `citadel_item_koth_spawner` within `MatchDistance` of a rift position; if none, a `citadel_koth_cashin` (side by `TryMatch`, else `RiftSides.Nearest`). The requested side is preferred within each kind | entity, side, designer name, or null |
| `GoLive` (private) | `ParkScheduler`; `CurrentSide` = the side; `steps.MoveTeamsToRift(side)`, log moved, log give-up (Debug), `AlternateSide(side)`, log next side / window / spawn, phase `Live`, start `WatchOutcome`. The watch spot (`WatchSpotRule`) follows `CurrentSide` | — |
| `WatchOutcome` (private, per tick) | `RiftWatch.Observe(new trooper?, cash-in exists?)`. `CashinAppeared`: Debug VData name / handle / give-up. `Finished`: log with `TrooperIndex` and `TrooperTeam` (the new trooper's `TeamNum`, kept as the winner team), phase `Ending`, `EndRound` after 3 s. `Tied`: same without a winner | — |
| `SnapshotRiftEntities()` | Stores the trooper snapshot; returns the spawner and cash-in snapshot | `RiftSnapshot` |
| `NoteTroopers(indexes, mode)` | Adds trooper entity indexes to the pre-rift snapshot so `WatchOutcome` ignores them (Rem assist creeps). Safe when no snapshot is open yet (creates an empty set) | — |
| `AlternateSide(spawnedSide)` | `NextSide` = `RiftSides.NextInRotation(spawnedSide, MiddleEnabled)` (only after a successful spawn) | — |
| `SetMiddleEnabled(enabled, mode)` | Sets `MiddleEnabled`; if mid turns off and idle `NextSide` is Center, advances to Green. Logs `Mid rift Enabled=` | — |
| `SetNextSide(side, mode)` | Refused while running, or Center while mid is off | `bool` |
| `CleanupRiftTroopers(mode)` | Removes every `npc_trooper` on the map (lane troopers are off, CleanSlate `citadel_trooper_spawn_enabled 0`, so every one is a rift trooper). Debug line `Rift troopers removed` | count |
| `ScheduleLateSweeps(timer, mode)` | `CleanupRiftTroopers` again 5 s and 10 s later (`LateSweepSeconds`), each skipped (Debug) while a rift is running; Information `Late rift troopers removed Removed= Delay=` when it removed any. The cash-in wave keeps spawning after the 3 s end timer, and those troopers used to stay forever | — |
| `EndRound(outcome, steps, timer, mode, winnerTeam = null)` | Log ending; `steps.ReturnPlayersToDraft()`; `CleanupRiftTroopers`; `ScheduleLateSweeps`; log round ended; round finishes (`RoundEnded` with the winner team) | — |
| `CancelRift(steps, timer, mode)` | Idle: refuse. Else cancel the three handles; `ParkScheduler` (or just KOTH off if gamerules cannot be resolved); `steps.ReturnPlayersToDraft()`; clean up troopers and `ScheduleLateSweeps`; round finishes as `cancelled`. Side is unflipped if the rift never spawned. A rift objective that already spawned stays on the map (verified in game); only our round ends. The game spawns no new rift while it is up; the next round's `WaitForSpawner` adopts it by its `citadel_koth_cashin` | reply line |
| `DescribeRift()` | Phase, current side, next side, mid on/off, last outcome, round id, snapshot size | 2 lines |

## Dangerous constraints

- Preserve the known-good order: configure → wait for spawner → park →
  move teams → flip → watch → 3 s → return alive → remove troopers (plus
  late sweeps). Do not insert steps between the KOTH convar and accessor
  writes.
- Never sweep troopers at rift start: `Remove()` is deferred to the end of
  the frame, and a reused entity index could hide the first new trooper
  from `WatchOutcome`.
- The steps must not teleport or `SelectHero` a dead pawn (`RoundFlow`
  skips dead players; Lobby's `player_spawn` returns them).
- Entity indexes are compared only within one designer name
  (`citadel_item_koth_spawner`, `citadel_koth_cashin`, `npc_trooper`)
  captured moments earlier; do not use them as global
  keys.
- Never remove `info_super_trooper_spawn`.
- The spawner is short-lived: it spawns the cash-in about 1 s later and is
  gone by the next round (`Existing=0` every round in the 2026-09-27
  logs). The live objective is `citadel_koth_cashin`; a normal one appears
  only after its spawner, so any cash-in in the start snapshot is a
  leftover. A leftover gives up about 60 s after its own spawn, so an
  adopted round can end soon as `tied`.
- Not yet verified: where the cash-in sits relative to the rift position
  (hence the `Nearest` fallback). The adoption Warning logs its position.
- Timer callbacks run on the plugin's timer and die with the plugin.
  Game-thread only.
