# Participants

Who takes part in the game: connected players that are not
bots, not leaving, not in the admin seat (`AdminSeat`), and not a
banned-player statue (`BanStatueService`).

## Operations

| Op | Behavior | Returns |
|---|---|---|
| `Humans()` | `Players.GetAll()` (fully connected only) filtered by `IsParticipant` | list |
| `IsParticipant(player)` | Not a bot, not leaving, not seated, not a statue | bool |
| `MarkLeaving(steamId)` | Marks a player whose disconnect is being handled (`LobbyService.RemovePlayer`, first line) | — |
| `ClearLeaving(steamId)` | Drops the mark; called on every `OnClientConnect` so a reconnect is a participant again | — |

## State

- `Leaving`: Steam IDs marked by `MarkLeaving`, kept until that Steam ID
  connects again (or the next load).

## Invariants

- Team counts, auto-start, Random and Mirror mode teams and heroes, stats
  boards, the lobby hero reset, and sending players up top all use this
  list, so a
  seated admin or a statue never gets a team slot, hero, round, or stats
  row.
- A leaving player is never in the list. During `OnClientDisconnect`
  `Players.GetAll()` still returns the leaving controller, and a leave can
  end the match (auto-start), which resets every participant's hero
  (`LobbyHeroes.ReturnAll`). Swapping the leaver's hero is skipped by Deadworks
  (`SelectHeroInternal skipped ... m_hController is stale`) and the
  next-tick send up on the removed controller throws.
