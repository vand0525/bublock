# RandomLoadouts

A random hero plus one of that hero's stored top builds per player, given
through `Modules/Loadout` (`LoadoutService.Swap`: `SelectHero`, then the
budgeted build 1 s later). Game-agnostic: the game type owns one instance
and decides when players draw (on join, on a kill, on a timer).

## Types

`RandomPick(Hero, Build)`.

## State (per instance)

- picks: Steam ID → current `RandomPick`;
- pending: players whose pick waits for a spawn (dead when it was given);
- `Catalog` (`HeroBuildCatalog`, usually `Default`: the embedded
  `hero-builds.json`), `Options` (`LoadoutOptions`, e.g. `Gold: 0`, cap
  20,000 souls, as Rift Roulette's Random mode uses).

## Operations

| Op | Behavior | Returns |
|---|---|---|
| `Draw(steamId)` | `HeroRoll.Pick` (not the current hero, others' heroes avoided) and a random build of it; stored as the player's pick | the pick, or null |
| `Give(player, timer, mode)` | Swaps to the player's pick now (`LoadoutService.Swap`); dead or no pawn: pending. Logs `Loadout started` / `Loadout pending spawn` | started |
| `Roll(player, timer, mode)` | `Draw` + `Give` | the pick, or null |
| `Hold(steamId)` | Marks the player's pick pending without a swap (a joiner whose hero the game type already selected on connect); false with no pick | bool |
| `ApplyPending(player, timer, mode)` | A pending player's `Give`; call from `player_spawn` / `player_respawned` on the next tick | started |
| `Landed` | Callback when a loadout lands and is still the player's pick: `(player, pick, LoadoutResult)`; the game type's banner (`result.Value` is the soul value) | — |
| `TryGet`, `IsPending`, `PendingCount`, `Forget`, `Clear`, `Describe(nameOf)` | Lookups and cleanup | — |

## Invariants

- Never `SelectHero` while dead: `LoadoutService.Swap` refuses and the pick
  goes pending.
- A later `Draw` replaces an earlier pick; a loadout that lands after a
  newer draw does not fire `Landed`.

## Deadworks constraints

- Swapping a living player mid-fight works (owner playtest, 2026-09-28;
  also the Deadworks Deathmatch example). The build lands 1 s after
  `SelectHero` and heals to full.
- The game type must stop client hero changes (`Modules/Teams`
  `ChoiceGuard`) or the player's menu pick would override the draw.

## Logs

`randomloadout-YYYYMMDD.log`; build details in `loadout-*.log`.
