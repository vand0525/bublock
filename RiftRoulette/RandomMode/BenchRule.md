# BenchRule

Pure decisions for Random mode's rotating bench: with an odd number of
players, one sits out each round so the fighting teams stay even. No game
calls, so it is unit tested (`Tests/RiftRoulette.Tests/BenchRuleTests`).

## Operations

| Operation | Result |
|---|---|
| `Next(rotation, players)` | Syncs the `PlayerQueue` rotation with `players` (drops the ones gone, adds new ones at the back in Steam ID order). With an odd count of at least `MinPlayers` (3): returns the front player (the one who sat out least recently) and moves them to the back. Otherwise null (nobody sits out). |
| `FightingTeams(teams, benched, returning, rng)` | The fighting teams: `teams` without `benched`, with `returning` (last round's bench player) set to team 0 (unassigned), then `TeamBalance.Even`. |

## Invariants

- The rotation order is "next to sit out first". A new player joins the
  back, so they play before they sit out.
- Everyone sits out once before anyone sits out twice, while the player
  set stays the same.
- `FightingTeams` differs by at most one player per team (`TeamBalance.Even`);
  with an even number of fighters the teams are equal.
- Because the returner is unassigned, `TeamBalance.Even` puts them on the
  smaller team: the side the bench player left. Other players keep their
  team unless the teams were already uneven.
