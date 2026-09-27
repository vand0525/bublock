# Progression

Deadlock's level table: how many boons, ability unlocks and ability points
a hero has at a given soul count. Pure data, no Deadworks calls.

## Types

- `ProgressionLevel(Souls, Level, Boons, Unlocks, AbilityPoints)`: one
  row, with every count accumulated up to it.

## Operations

| Op | Behavior |
|---|---|
| `ForSouls(souls)` | The last row whose `Souls` threshold is at most `souls`. Below the first threshold (600) it returns the first row: every hero starts there. |
| `Max` | The last row (level 36, 35 boons, 4 unlocks, 32 points) |
| `MaxLevel` | 36 |

## Table

36 rows, from the [Deadlock wiki soul-unlock data](https://deadlock.wiki/index.php?title=Data:SoulUnlockData.json&action=raw)
(fetched 2026-09-27). The wiki stores souls above the 600 starting gold;
the rows here are the displayed values (data + 600): 600, 800, 1,100,
1,500, 2,000, ... 45,500, 49,200.

- `Level` is the 1-based row. It is what `LoadoutService` writes to
  `pawn.Level` (36 at the top, as the Deadworks Deathmatch example uses).
- `Boons` is `Level - 1`: every row after the first is a power increase.
- `Unlocks`: rows 600, 1,100, 2,000 and 3,800 each grant one (4 total).
- `AbilityPoints`: every other row grants one (32 total, enough to max all
  four abilities at 1 + 2 + 5 each).

## Invariants

- Rows are sorted by `Souls`; counts never decrease.
- A game patch can move thresholds. Recheck the wiki data on patch day
  (`reference/patch-day.md`).
