# RandomHeroes module

## Purpose

Random hero assignment shared by every random-hero mode. Game agnostic: it
knows Steam IDs and `Heroes`, never teams, rifts or betting. Used by Rift
Roulette's Random mode (`RandomMode/RandomModeService`); meant for Crystal
Crackers' intermission draw too.

## Files

| File | Role |
|---|---|
| `HeroDraw.cs` | Unique random heroes, no repeat of last round, fixed heroes honored (pure, tested). |
| `RandomHeroes.projitems` | Adds `HeroDraw` to a consumer. |

## Public operations

`HeroDraw.Draw(players, pool, previous, rng, fixedHeroes)` (see `HeroDraw.md`).

## State

None. The consumer owns the pool (build catalog minus bans or failed
swaps), last round's heroes and any fixed heroes (reservations).

## Commands and lifecycle

The module registers no commands and has no plugin class, so it never
clashes with another DLL. Consumers call it from their own intermission
(Rift Roulette: `RandomModeService.PrepareRound` and `AssignLate`).

## Tests

`Tests/Modules.Tests/HeroDrawTests.cs`.
