# Balance — Feature

## Purpose

Keeps Random mode matches from becoming one-sided. At the start
of each intermission, if one team is stomping or on a long streak, its best
player swaps with the other team's weakest player (or just moves, when the
winning team has more players).

## Rules

Counted since the last swap (or match start):

- **Stomp:** a round lead of 2+, and the leader has 8+ more kills and at
  least 1.5x the other team's kills.
- **Streak:** otherwise, 5 scoring rounds in a row for one team (ties,
  cancels, and timeouts don't count or break it).
- **Who:** score = kills + assists - deaths this match.
- **Needs 3+ fighting players;** the bench does not count, so a 1v1 (with
  or without a benched third player) never swaps. Counters reset while
  fewer fight, so 1v1 rounds never cause a swap once more players join.
- After a swap all counters reset.

## Files

| File | Role |
|---|---|
| `BalanceTracker.cs` | Counters and the trigger (pure, tested) |
| `BalancePicker.cs` | Who moves (pure, tested) |
| `BalanceService.cs` | Tracker owner, applies moves to Random mode teams, announces |
| `BalancePlugin.cs` | `/balance_status`, `/balance_auto`, `/balance_now` |

## Public operations

See `BalanceService.md`. Admin commands in `reference/admin-commands.md`.

## State

`BalanceService.Tracker` and `Enabled` (one per DLL load).

## Dependencies

- Fed by `GameLoop/MatchService.OnRoundEnded` (rounds) and
  `Stats/StatsService.RecordDeath` (kills); reads `StatsService.Ledger` scores.
- Applied from `RandomMode/RandomModeService.PrepareRound`.
- `Lobby/RiftRouletteTeams`, `Shared` (chat, logging, auth).

## Lifecycle vs commands

- The match loop runs it in the match's mode through `PrepareRound`.
- `/balance_now` forces a check and a swap through the same path in Debug
  mode.

## Logs

`balance-YYYYMMDD.log`; swaps also in master.
