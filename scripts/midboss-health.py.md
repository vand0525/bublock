# midboss-health.py

Recomputes the mid-boss souls-to-game-minute table
(`Modules/MidBoss/MidBossRule.MinuteBySouls`) from real Deadlock matches.
Read-only: prints results, writes nothing.

## Usage

```bash
python3 Bublock/scripts/midboss-health.py [--matches 500] [--min-badge 80] [--step 2500] [--min-share 0.25]
```

Copy the printed `MidBossRule.MinuteBySouls` into `MidBossRule.cs` (and
its `.md` and tests) by hand, then rebuild. Run after economy or mid-boss
patches (`reference/patch-day.md`). One large request; about 35 s for
1,000 matches.

## Behavior

1. `GET /v1/matches/metadata` with player stats and mid-boss kills: the
   latest `--matches` ranked normal matches of at least 20 minutes
   (`--min-badge` filters by average badge).
2. Per match, the lobby's average net worth per player at every stats
   snapshot (every 180 s; each player interpolated between snapshots).
3. Real mid-boss kills: for the 1st, 2nd, 3rd ... kill (20+ samples), the
   25th percentile, median and 75th percentile game minute, lobby average
   and killing team average net worth.
4. For every `--step` souls: the median minute at which the lobby average
   first reaches it (interpolated), its 25th–75th percentile, and the
   health (13,000 + 195 × minute rounded) and resistance threshold
   (35 + 5 × minute) of a boss the game spawns then. Stops when fewer than
   `--min-share` of the matches reach the souls.
5. Prints the table as a C# array.

## Notes

- `mid_boss` needs `include_mid_boss=true`; `include_objectives` leaves it
  out.
- The health and resistance constants are copied from `npc_units.vdata`
  (`npc_super_neutral`); check them on patch day with `MidBossRule`.
- The 2026-10-13 run (1,000 matches, median badge 56, step 2,500): the
  table in `MidBossRule.cs`; first kills at median minute 23.9 and 23,665
  souls.
