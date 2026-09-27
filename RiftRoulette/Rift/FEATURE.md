# Rift — Feature

## Purpose

The rift itself: force the game's KOTH (rift) to spawn on the next side
(green / yellow alternating), park the natural scheduler, watch for a finish
(troopers spawn) or a tie (the cash-in disappears), then after 3 seconds end
the round and remove the rift's troopers. Extracted from Legacy `/koth` in
Stage 10; the sequence is the known-good archive one. Moving players is not
done here: the caller hands in those steps (`RiftRoundSteps`), supplied by
`Round/RoundFlow` since Stage 11.

## Files

| File | Role |
|---|---|
| `RiftSide.cs` | `RiftSide` enum, side positions, name, parse, flip, side from a position (pure) |
| `RiftWatch.cs` | Per-tick outcome decision: waiting / cash-in appeared / finished / tied (pure) |
| `RiftGameRules.cs` | Gamerules pointer, KOTH schema accessors, configure and park steps |
| `RiftService.cs` | State and ops: `RunRift`, `AlternateSide`, `SetNextSide`, `CleanupRiftTroopers`, `ScheduleLateSweeps`, `EndRound`, `CancelRift`, `DescribeRift`; `RiftRoundSteps` |
| `RiftRoundResult.cs` | `RiftOutcome` strings and `RiftRoundResult(Outcome, Side, WinnerTeam)` handed to the `RoundEnded` step (pure) |
| `RiftPlugin.cs` | Admin command wrappers |

## Public operations

See `RiftService.md`. Admin commands: `/rift_start` (archive `/koth`,
removed in Stage 12), `/rift_status`, `/rift_next`, `/rift_cancel`,
`/rift_cleanup`. Catalogued in `reference/admin-commands.md`. No player
commands. `/rift_start` and `/rift_cancel` go through `Round/RoundFlow`.

## State

Owned by `RiftService` (one set per DLL load): next side, phase, current
side, last outcome, trooper snapshot, live timer handles, round counter.
Only one rift runs at a time; `/rift_start` refuses while one is running.

## Dependencies

- Game only (gamerules, KOTH convar, entities) and `Shared` (`AdminCommand`,
  logging, `BublockLog.RoundId`).
- `RiftPlugin` calls `Round/RoundFlow` for start and cancel.
- Lobby's `player_spawn` hook returns players who were dead at round end.

## Logs

`rift-YYYYMMDD.log`: every step of the sequence. Master: round started,
rift spawned, round ended, cancelled, plus Warning+ (spawn timeout,
gamerules errors). Lines during a rift carry `round=r<n>`.

## Lifecycle vs commands

- Admin commands run the composed round (`RoundFlow`) in Debug mode.
- The lifecycle entry is `RoundFlow.RunRound(timer)`; since Stage 13a the
  match loop (`GameLoop/MatchService`) calls it between intermissions.
- Round end and cancel remove every `npc_trooper`, then sweep again 5 s
  and 10 s later while no rift runs: the cash-in wave keeps spawning after
  the 3 s end timer, and those troopers used to pile up round after round.
- Every ending path reports a `RiftRoundResult` through the `RoundEnded`
  step; the match loop scores it. A finished round's winner is the team of
  the first new rift trooper (`TrooperTeam=` in the rift log).
- `/rift_cancel` ends our round only; a rift objective already on the map
  stays, and the game spawns no new rift until it gives up (about 60 s).
  The next round adopts it by its `citadel_koth_cashin`: right away when a
  cash-in was already up before the forced spawn, otherwise after the 5 s
  timeout if one is found (teams, watch spot, next side and scoring follow
  its side).
