# Teams module

## Purpose

Deadlock team basics any game type needs: the team numbers and names,
placing a new player on the smaller team, and refusing client hero or team
changes when the game type decides them. Game-agnostic (redock fork, added
with Gun Game).

## Files

| File | Role |
|---|---|
| `DeadlockTeams.cs` | Team numbers, names, `SmallerTeam` (pure, tested) |
| `ChoiceGuard.cs` | Which client commands to stop (pure, tested) |
| `Teams.projitems` | Compiles both into a consumer |

## Public operations

See `DeadlockTeams.md` and `ChoiceGuard.md`.

## State

None.

## Commands and lifecycle

No plugin class, no commands. The game type calls `SmallerTeam` when a
player joins and `ChoiceGuard.Blocks` from its `OnClientConCommand` hook.

## Dependencies

None (not even `DeadworksManaged.Api`).
