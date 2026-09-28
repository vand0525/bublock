# GunGameService

Gun Game (Stage 13k): a match format on top of Random mode. Rounds, rifts,
the watch spot and the intermission draw stay as in Rift Roulette. Every
credited kill gives the killer one step on the kill ladder and, right away,
a new random hero with one of its stored top builds (the same budgeted
build style as the intermission draw). The first to the target wins the
match. Static, one per DLL load.

## State

- `Ladder` (`GunGameLadder`): kills per Steam ID, target, winner. The
  target is a setting (default 10) that survives matches and resets on DLL
  load.
- `Names`: Steam ID to name for every player with a kill, so the summary
  can name a winner who left.
- The `ITimer` given by `MatchService.Start`.

## Operations

| Op | Behavior |
|---|---|
| `Active` | `MatchConfig.IsGunGame` (format `gungame` in Random mode) and a match running |
| `BeginMatch(timer, mode)` | From `MatchService.Start` when `IsGunGame`: keeps the timer, resets the ladder, logs `Gun Game started Target=` |
| `EndMatch(mode)` | From `MatchService.End` when `IsGunGame`, after the "Match over" banner text was read: logs every player's final kills and `Gun Game ended Winner=`, then resets |
| `OnKill(attacker, victim, mode)` | From `StatsService.RecordDeath` for every credited kill (human attacker, enemy victim, hero lock kills skipped). Does nothing unless `Active`. `Ladder.Record`; not counted (self kill or after a win): stop. Logs `Kill counted Kills= Target= Victim=`. Winning kill: `Win`. Otherwise `RandomModeService.Reroll` with a banner callback: when the new loadout lands the killer sees `Kill 3/10: <hero>` / `<build> - 12,345 souls` (one banner, folded with the build banner). No Random assignment: Warning `Kill counted but no reroll` |
| `Reroll(player, timer, mode)` | Admin test path (`/gungame_reroll`): the same `RandomModeService.Reroll` a kill gives, no ladder step, banner `<hero>` / `<build> - souls`. Needs a running Random match; any format | 
| `SetTarget(kills, mode)` | Refused while a match runs; 1..50 (`GunGameLadder.TrySetTarget`) |
| `StartDescription(seconds)` | `First to 10 kills - round 1 in 5s` (the match start banner) |
| `Summary()` | `<Winner> wins - 10 kills`, else `Leader: <name> - 4/10 kills`, else `No kills yet - first to 10`. `MatchService` uses it as the Gun Game score |
| `Describe()` / `DescribePlayer(player)` | Admin status lines / the `/ladder` reply |
| `Forget(steamId)` | Drops a player's kills (not called yet: a player who leaves keeps their place until the match ends) |

### Win

Logs `Gun Game won` (feature and master, with the winner's `PlayerRef`).
On the next tick, outside the death event: `MatchService.End` (its "Match
over" banner shows `Summary()`, everyone goes back up top), then after
`RestartDelaySeconds` (10 s) `AutoStartService.Check`, which starts the
next match when auto-start is on and 2 players remain.

## Invariants

- Only credited kills count; `StatsService` decides, exactly as for the
  stats boards, auto-balance and betting chips.
- The reroll goes through Random mode's own assignment, so the hero lock
  never punishes it (`HeroLock`: our swaps in flight are not applied yet).
- A dead killer (trade) gets the new hero on their next spawn (pending),
  which in Rift Roulette is up top until the next round.

## Deadworks constraints

- `SelectHero` on a living pawn swaps the hero in place (the Deadworks
  Deathmatch example swaps living players every minute the same way);
  the build lands `LoadoutService.SwapDelaySeconds` (1 s) later, and the
  loadout heals to full. Untested in a live round on our server.

## Logs

`gungame-YYYYMMDD.log`; the win also goes to the master log. Hero and
build details are in `random-*.log` (`Rerolled`) and `loadout-*.log`.
