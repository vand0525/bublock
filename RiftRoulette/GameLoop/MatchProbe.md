# MatchProbe

Writes a snapshot of the match to its own log (`probe-YYYYMMDD.log`,
`[RiftRoulette.Probe]`) so playtests can be checked from the logs.
Information level, so it is written without
Debug mode; a few lines per event, no per-frame work.

## Operations

| Op | Effect |
|---|---|
| `Snapshot(reason)` | Writes the lines below now |
| `SnapshotLater(timer, reason)` | `Snapshot(reason)` after `DelaySeconds` (1 s), on the caller's timer |

## Lines

- **Header** `Probe Reason=... MatchRound= Mode= Phase= WatchSide= BoardSide= NextSide= Rift= RiftSide= BuyAnywhereConVar= Restrained= Rescues= Map=...`:
  `Map` counts live `npc_trooper_boss` (guardians), `npc_boss_tier2`
  (walkers), `npc_barrack_boss` and `citadel_shop_prop_dynamic` (shop
  kiosks); all should be 0 after CleanSlate. `BuyAnywhereConVar` is the live
  `citadel_allow_purchasing_anywhere` value (should be 0).
- **One per participant** (with `PlayerRef`): team, hero, alive, position and
  eye angles (`MovementService.Where`), `Half` (Sapphire when y > 0, else
  Amber), restrained, and which `RestraintService.States` are on.
- **One per tracked board**: id, position, angle (`WorldTextService.List`).

## Called from

- `MatchService.Start`: `Snapshot("match-start")` (both `/match_start` and
  auto-start).
- `RoundFlow.Steps(mode, timer)`: `moved-in` 1 s after the teams move into
  the rift, `sent-up` 1 s after players are sent back up. Only for rounds
  started through `RunRound` (the match loop and `/rift_start`).

## Invariants

- Read-only: never changes game state.
