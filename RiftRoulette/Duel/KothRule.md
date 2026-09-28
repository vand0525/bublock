# KothRule

Winner-stays-on decisions for 1v1 mode. Pure; unit tested in
`Tests/RiftRoulette.Tests`.

## Operations

- `Loser(fighterTeams, winnerTeam)`: the fighter on the other team from
  `winnerTeam`, or null when `winnerTeam` is null (tie), no fighter is on
  the winner team, or there isn't exactly one fighter on each side.
- `Winner(fighterTeams, winnerTeam)`: the fighter on `winnerTeam`, null in
  the same cases as `Loser` (the two always agree).
- `Crown(king, streak, winner)`: `(winner, streak + 1)` when the king won
  again, otherwise `(winner, 1)`.

`fighterTeams` maps Steam ID to team number for the two players who fought
the round (`DuelService.Teams`).
