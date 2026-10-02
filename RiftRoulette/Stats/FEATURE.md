# Stats — Feature

## Purpose

Counts every player's kills, deaths and assists from match start
and shows them on two boards up top: Sapphire's on the Sapphire side and
Amber's on the Amber side, each with the team's rounds won, the team
total, and one row per player.

## Files

| File | Role |
|---|---|
| `StatsLedger.cs` | Per-player counts and the kill / assist rules (pure, tested) |
| `DamageLedger.cs` | Per-life damage totals; an assist needs 20% of the victim's max health dealt (pure, tested) |
| `StatsBoardText.cs` | Board text: team K/D/A (pure, tested) |
| `StatsService.cs` | Death recording, board refresh, descriptions |
| `StatsPlugin.cs` | `player_death` and `player_spawn` hooks, `/stats`, `/stats_board`, `/stats_reset` |

An assist is decided by damage, not by the game's assist list (which
credits nearby players, including dead fighters waiting up top): an enemy
participant who dealt at least 20% of the victim's max health since the
victim's last spawn, other than the killer. Damage comes from
`GameLoopPlugin.OnTakeDamage` (hits on restrained players are blocked and
never counted).

## Public operations

See `StatsService.md`. Commands catalogued in `reference/user-commands.md`
(`/stats`) and `reference/admin-commands.md` (`/stats_board`, `/stats_reset`).

## State

`StatsService.Ledger`, `StatsService.Damage` and round counts (one per DLL
load). Reset at `/match_start` and by `/stats_reset`; damage totals are
also cleared per victim on spawn and death, and per player on disconnect.

## Dependencies

- `Modules/WorldText` (boards), `Boards/BoardLayout` (positions).
- `GameLoop/MatchService` (running check, round counts).
- `RandomMode/RandomModeService.ConsumeEnforcementKill` and
  `Mirror/MirrorModeService.ConsumeEnforcementKill` (hero swap deaths
  are not counted), `Balance/BalanceService.RecordKill` (kill counts for
  auto-balance).

## Lifecycle vs commands

- The `player_death` hook and the match loop (reset, round counts) run in
  Clean mode; Lobby connect / disconnect, `RandomModeService.PrepareRound`
  and `Boards/BoardService.Redraw` refresh the boards.
- Admin commands run the same ops in Debug mode. `/stats` is a player
  command.

## Logs

`stats-YYYYMMDD.log`.
