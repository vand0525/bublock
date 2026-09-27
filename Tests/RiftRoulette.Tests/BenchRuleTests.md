# BenchRuleTests

Unit tests for `RiftRoulette/RandomMode/BenchRule`.

- `Next` sits nobody out with an even count (2, 4, 6) or fewer than 3
  players.
- With 5 players, five rounds sit out each player once, then the first
  one again.
- A player who left is dropped from the rotation; new players join the
  back (in Steam ID order), and the front player sits out and goes last.
- `FightingTeams` leaves out the bench player and splits 5 into 2v2
  without moving anyone who was already even.
- The returning player (last round's bench) fills the side the new bench
  player left; everyone else keeps their team.
