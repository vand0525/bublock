# TeamBalanceTests

Unit tests for `Lobby/TeamBalance` (pure, seeded `Random`).

- `Even` with 0, 1, 6, 7, and 12 unassigned players: everyone lands on
  Sapphire or Amber, and the team sizes differ by at most one (20 seeds each).
- `Even` leaves already balanced teams unchanged.
- `Even` on 5 Amber vs 1 Sapphire moves exactly two Amber players and keeps
  the Sapphire player.
- `Even` on 2 Sapphire vs 0 Amber (what a seated admin or a leaver left in
  the 2026-09-27 playtest) gives one each.
- `SmallerTeam` returns the team with fewer players; on a tie (including
  no players) it returns Sapphire or Amber.
