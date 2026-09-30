# RemCreepsService

Spawns `npc_trooper` creeps for Rem in Random mode when exactly two
fighters are live. Count is set with `/rem_creeps` (default 3, 0 = off).

## State

| Field | Meaning |
|---|---|
| `Count` | How many to spawn next time (DLL load → `RemCreepsRule.DefaultCount`) |
| `Spawned` | Entity indexes of this round's creeps (for logging; cleanup is global) |

## Operations

| Op | Behavior |
|---|---|
| `SetCount(count, mode)` | Clamps 0–8, logs Information | reply |
| `TrySpawn(side, mode)` | If `RemCreepsRule.ShouldSpawn`, creates `Count` `npc_trooper` on Rem's team near Rem (or rift center), `RiftService.NoteTroopers` so capture watch ignores them | spawned count |
| `Clear(mode)` | Drops tracked indexes (troopers themselves go through `CleanupRiftTroopers`) | — |
| `Describe()` | Status line for `/rem_creeps` | string |

## Wiring

Called from `RoundFlow.MoveTeamsToRift` after fighters teleport in.
`RemHero` is `Heroes.Familiar` (display name Rem).

## Invariants

- Never spawn outside Random with exactly 2 fighters and Rem assigned.
- Must call `NoteTroopers` or a Rem creep ends the round as a false capture.
- Round end / cancel already removes every `npc_trooper`.
