# Participants

Who takes part in the game (Stage 13f): connected players that are not
bots, not in the admin seat (`AdminSeat`), and not a banned-player statue
(`BanStatueService`).

## Operations

| Op | Behavior | Returns |
|---|---|---|
| `Humans()` | `Players.GetAll()` (fully connected only) filtered by `IsParticipant` | list |
| `IsParticipant(player)` | Not a bot, not seated, not a statue | bool |

## Invariants

- Team counts, auto-start, Random mode teams and heroes, stats boards, the
  draft reset, and returning players to the draft all use this list, so a
  seated admin or a statue never gets a team slot, hero, round, or stats
  row.
