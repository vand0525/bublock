# GunGameRulesTests

Unit tests for `GunGame/GunGameRules` and the game type's embedded arena
asset.

- `Credits`: an enemy kill by a player on a team scores; self kills, team
  kills, Steam ID 0 (world, bots) and spectator attackers do not.
- The session is 2-minute matches from 2 players; the start banner.
- Kill and hero banners: `1 kill` / `3 kills`, build and soul value
  (invariant culture).
- Result banner: one winner, a tie, nobody scored.
- `GunGame/Data/arena.json` loads through `Modules/Arena` as "mid lane
  brawl": lane 4, 13 offsets, bounds, and every slot's spot for both teams
  inside the bounds. (Floor and wall checks are `scripts/check-arena.py`.)
