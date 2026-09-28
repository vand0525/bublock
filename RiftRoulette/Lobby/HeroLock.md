# HeroLock

Reusable "you may not change hero" guard. Random mode and 1v1 (Duel) mode each own one instance; the owner
decides which hero a player should have and how to rebuild them.

## State (per instance)

- `applied`: players whose assigned loadout has been applied. Only these can
  be punished, so our own hero changes in flight never trigger the guard.
- `pending`: players whose loadout waits for a spawn (dead at prepare time,
  joiners, or just killed by the guard). `PendingCount`, `IsPending`.
- `kills`: players the guard just killed; `StatsService` consumes the entry
  (`ConsumeKill`) and skips that death.

## Operations

| Op | Behavior |
|---|---|
| `MarkApplied` / `Unapply` | Add / remove from `applied` |
| `MarkPending` / `ClearPending` / `ClearAllPending` | Pending set edits |
| `ConsumeKill(steamId)` | Removes and returns the kill flag |
| `Enforce(player, pawn, assigned, assignedName, log, rebuildInPlace)` | See below |
| `Forget(steamId)` | Removes the player from all three sets |
| `Clear()` | Empties all three sets |

### Enforce

- Right hero, not applied, pending, or dead: does nothing.
- Otherwise: removes from `applied`, marks pending, flags a kill, then
  `pawn.Hurt(1_000_000f)`. Logs `Hero swap punished` (Information) and tells
  the player `Changing hero is not allowed - you respawn as <hero>.`; the
  owner re-applies on respawn through its pending path.
- If the pawn survives the damage: undoes the flags, logs a Warning, and
  calls `rebuildInPlace`.

## Deadworks constraints

- `Hurt(1_000_000f)` is the upstream `Kill()` helper's damage; it counts as a
  normal death, hence the kill flag.
