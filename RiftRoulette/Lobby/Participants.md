# Participants

Who takes part in the game (Stage 13f): connected players that are neither
bots nor in the admin seat (`AdminSeat`).

## Operations

| Op | Behavior | Returns |
|---|---|---|
| `Humans()` | `Players.GetAll()` (fully connected only) minus bots and seated admins | list |
| `IsParticipant(player)` | Not a bot and not seated | bool |

## Invariants

- Team counts, auto-start, Random mode teams and heroes, stats boards, the
  draft reset, and returning players to the draft all use this list, so a
  seated admin never gets a team, hero, round, or stats row.
