# ItemSlots

How many items a loadout may hold at a soul value: the slots a real hero
has at that net worth. Pure data, no Deadworks calls. The HUD is not
affected (`Lobby/FlexSlots` still opens all 12 slots); only the number of
items a loadout buys is capped.

## Operations

| Op | Behavior |
|---|---|
| `ForSouls(souls)` | `BaseSlots` (9) below the first breakpoint, else the slot count of the last breakpoint reached: 10 from 16,000, 11 from 22,000, 12 from 28,000 |
| `ExtraPasses(slots)` | true only at `MaxSlots` (12): the planner's fill-from-optional-items and upgrade passes run only when every slot is open |
| `Describe()` | One line for admin replies: `9 items, then 10 from 16,000, 11 from 22,000, 12 from 28,000` |

## Where the numbers come from

- Deadlock slots are universal. Every hero starts with 9; each enemy Walker
  the team destroys opens one more, to 12 (since the 2025-11-21 update;
  Guardians and shrines no longer unlock slots).
- Breakpoints are the median player net worth when their team's 1st, 2nd
  and 3rd enemy Walker fell, rounded to the nearest 1,000 souls: 15.7k,
  22.0k, 28.5k (28,455) across 500 ranked matches (median badge 61, fetched 2026-09-28 from
  `api.deadlock-api.com/v1/matches/metadata`). Items held in those matches
  never exceeded the slot count.
- Refresh with `scripts/walker-souls.py` after economy patches
  (`reference/patch-day.md`).

## Invariants

- `Breakpoints` are sorted by souls; slot counts only grow; the last one is
  `MaxSlots`.
- Tested by `Tests/Modules.Tests/ItemSlotsTests`.
