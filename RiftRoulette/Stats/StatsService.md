# StatsService

Match kill / death / assist tracking and the stats boards shown when the
draft is off (Random and 1v1 modes). Static; owns one `StatsLedger`, one
`DamageLedger` (`Damage`, who hurt whom this life) and the last known round
counts.

## Operations

| Op | Behavior |
|---|---|
| `Reset(mode)` | Clears the ledger and the damage totals, takes round counts from `MatchService.State`, logs, refreshes boards. Called by `MatchService.Start` and `/stats_reset` |
| `SetRounds(sapphire, amber, mode)` | Stores round counts, refreshes boards. Called by `MatchService.OnRoundEnded` outside 1v1 mode |
| `RecordDeath(args, mode)` | From `player_death`. Only while a match runs (intermission or round). Victim must be a human controller. If `RandomModeService.ConsumeEnforcementKill`, `MirrorModeService.ConsumeEnforcementKill` or `DuelService.ConsumeEnforcementKill` is true (hero swap guard), the death is skipped. Otherwise the assisters are `Damage.Assisters(victim, attacker, victim pawn MaxHealth)`: every connected participant who dealt at least 20% of the victim's max health this life, not the killer (the event's `Assister1..5controller` are ignored: the game credits players nearby, including dead fighters up top). The victim's damage totals are then cleared. Attacker and assisters go to `StatsLedger.RecordDeath` (which keeps only assisters on the killer's team); on a credited kill (`DeathCredit`) the team feeds `BalanceService.RecordKill`, the killer gets betting souls (`BettingService.OnKill(killer, victim)`, Random mode; also pays the killer's mark on that victim), and the credited assisters get theirs (`BettingService.OnAssists`). Debug log (`CreditedAssists= MaxHealth= Damage=name:amount,...`), then refresh boards |
| `RecordDamage(args)` | From `GameLoopPlugin.OnTakeDamage` for every hit not blocked by restraint. Only while a match runs. Victim: the hero pawn's controller; attacker: `Info.Attacker`, else `Info.Originator`, as a hero pawn's controller (troopers, turrets without a hero owner and world damage are skipped). Both must be participants. Adds `min(Info.Damage, victim health left)` to `Damage` | — |
| `OnSpawn(player)` | From `player_spawn`: clears that player's damage totals (new life) | — |
| `Forget(steamId)` | From `LobbyService.RemovePlayer`: drops the player from the damage totals as victim and attacker | — |
| `RefreshBoards(mode)` | When the draft is off (Random and 1v1 mode; not Draft). 1v1 mode: both boards get the same `StatsBoardText.StreakBoard(DuelService.StreakRows())` leaderboard. Random mode: for Sapphire and Amber, rows for participants (no bots, no seated admin) by current `TeamNum`, text from `StatsBoardText.TeamBoard`. Either way `WorldTextService.Update` if the board exists, else `Create` at `BoardLayout.Side(team)` (same spots and colors) |
| `Describe(player)` | `/stats`: the caller's `K / D / A`, then both team totals |
| `DescribeAll()` | Admin: match running, tracked count, rounds, then one line per human (team, K/D/A, score) |

Boards: `stats.sapphire`, `stats.amber` (same spots as the Draft pool
boards). `DraftService.RedrawBoards` clears all boards and calls
`RefreshBoards` when the draft is off (Random and 1v1 mode), so refresh
always recreates missing boards.

## State

Ledger (per match), last round counts. Stats stay after `/match_end` until
the next `/match_start`.

## Logs

`Stats` feature log (`stats-YYYYMMDD.log`): resets (Information), each
recorded death (Debug).

## Constraints

- Bots are ignored as victims, attackers and assisters; damage is only
  counted between participants.
- `OnTakeDamage` runs before the hit lands, so `Info.Damage` may be the
  amount before resistances; the cap at the victim's health left limits
  overcounting. Check `Damage=` on `Death recorded` lines in
  `stats-*.log`; if totals look inflated, switch to `Info.TotalledDamage`
  or the `player_hurt` event's `DmgHealth` (not verified to fire).
