# walker-souls.py

Recomputes the loadout item-limit breakpoints (`Modules/Loadout/ItemSlots`)
from real Deadlock matches. Read-only: prints results, writes nothing.

## Usage

```bash
python3 Bublock/scripts/walker-souls.py [--matches 500] [--min-badge 80]
```

Copy the printed `Suggested ItemSlots.Breakpoints` into `ItemSlots.cs`
(and its `.md` and tests) by hand, then rebuild. Run after economy or
objective patches (`reference/patch-day.md`). One large request; about 30 s.

## Behavior

1. `GET /v1/assets/items`: IDs of `upgrade` items (shop items; abilities are
   also in a player's item list and are left out).
2. `GET /v1/matches/metadata` with player items, player stats and
   objectives: the latest `--matches` ranked normal matches of at least
   20 minutes (`--min-badge` filters by average badge).
3. Per player: the times their team destroyed enemy Walkers (objectives
   named `Tier2Lane*` whose `team` is the other team, `destroyed_time_s`
   above 0), and net worth at each time (interpolated between the
   3-minute `stats` snapshots).
4. Prints, for the 1st, 2nd and 3rd Walker, the 25th percentile, median and
   75th percentile net worth, and suggested breakpoints: each median
   rounded to the nearest 1,000 souls with `9 + n` slots.
5. Prints items held (bought, not yet sold) per 2,000 net worth band, and
   the share of samples with no Walker down, as a check that held items
   never exceed the slot count.

## Notes

- Enum query values are lowercase (`game_mode=normal`, `match_mode=ranked`);
  `Normal` returns HTTP 400.
- `assets.deadlock-api.com` does not resolve; assets are under
  `api.deadlock-api.com/v1/assets/...`.
- The 2026-09-28 run (500 matches, median badge 61): Walkers at 15.7k,
  22.0k and 28.5k median net worth, giving `(16000, 10), (22000, 11),
  (28000, 12)`. A rerun an hour later gave 15.8k / 22.1k / 28.6k (third
  suggestion 29,000): the medians move a few hundred souls between runs,
  so only change `ItemSlots` for a shift of about 1,000 or more.
