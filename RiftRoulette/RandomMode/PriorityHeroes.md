# PriorityHeroes

Pure list of priority heroes for Random mode (a new hero everyone should
get to play). Fixed in code: change `List` and deploy. Now: Rat King
(`Heroes.RatKing`, released 2026-10-02), Baba (`Heroes.Baba`, released
2026-10-06) and Solomon (`Heroes.Chessmaster`, `hero_chessmaster`).

A priority hero:

- is given to a fighter every round in Random mode when the round has
  `MinFighters` (8) or more fighters (`HeroDraw.Draw` `mustInclude`,
  `RandomModeService.PrepareRound` / `AssignLate`); with fewer fighters it
  is drawn like any other hero, so a 1v1 is not always two priority heroes;
- can't be reserved (`/reserve`) or banned (`/heroban`) at any fighter
  count: both reply `RefusedLine` before any souls are spent;
- has no effect on Mirror mode (`MirrorPick` never reads this list);
- when the server can't spawn it (the swap never takes), is off the draw
  for the rest of the match and its player is rerolled at once
  (`RandomModeService.OnSwapFailed`).

## Operations

| Operation | Result |
|---|---|
| `List` | The priority heroes (`IReadOnlySet<Heroes>`). |
| `MinFighters` | 8: the fewest fighters in a round for priority heroes to be guaranteed. |
| `Contains(hero)` | True when the hero is in `List`. |
| `Guaranteed(fighters)` | True when `fighters >= MinFighters` (tested). |
| `InPool(pool)` | The heroes of `pool` that are in `List`, in pool order. A listed hero with no stored builds is not in the pool, so it is left out. |
| `InPool(list, pool)` | Same with any list (tested). |
| `RerollPool(pool, failed, taken)` | Heroes for a player whose priority hero did not spawn: `pool` without `failed`; of those, the ones not in `taken` (held by other fighters), else all of them; empty when only failed heroes are left (tested). |
| `RefusedLine(name, action)` | `<name> is a priority hero: with 8 or more fighters someone gets it every round, so it can't be <action>.` (`reserved` / `banned`). |

## Invariants

- No game calls; unit tested (`Tests/RiftRoulette.Tests/PriorityHeroesTests`).
- A hero needs an entry in the server's `Heroes` enum (`lib/`) and stored
  builds (`scripts/fetch-builds.py`) before it can be listed usefully.
