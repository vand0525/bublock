# RandomLoadout module

## Purpose

"Give this player a new random hero with a real build" as one call, for
any game type: Gun Game (a new hero on every kill), and later zombie or
random-hero modes. Built on `Modules/Loadout` and its embedded
`hero-builds.json` asset (top builds per hero from `scripts/fetch-builds.py`).
Added in the redock fork; Rift Roulette's Random mode keeps its own
round-based draw.

## Files

| File | Role |
|---|---|
| `HeroRoll.cs` | Which hero to draw (pure, tested) |
| `RandomLoadouts.cs` | Per-player picks, swap now or pending, `Landed` callback |
| `RandomLoadout.projitems` | Compiles both into a consumer |

## Public operations

See `HeroRoll.md` and `RandomLoadouts.md`.

## State

Per `RandomLoadouts` instance (the game type holds one static).

## Commands and lifecycle

No plugin class, no commands. The game type calls `Roll` when a player
should change hero, `ApplyPending` from its spawn hooks, `Forget` on
disconnect, and sets `Landed` for its banner.

## Dependencies

`Modules/Loadout` (`HeroBuildCatalog`, `LoadoutService`, `LoadoutOptions`),
`Shared` (logging), `DeadworksManaged.Api`.
