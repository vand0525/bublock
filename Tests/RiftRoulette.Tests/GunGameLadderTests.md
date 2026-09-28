# GunGameLadderTests

Unit tests for `RiftRoulette/GunGame/GunGameLadder`.

- Target 3: kills count per attacker and the third kill wins (`Won`,
  `Winner`).
- Target 1: after the win no kill counts; after `Reset` a self kill does
  not count and there is no winner.
- Target range 1..50: 0 and 51 are refused (default 10 stays); accepted
  targets survive `Reset`.
- `Standings` sorts by kills then Steam ID; `PlaceOf` shares places on
  ties, an unknown player is last; `Forget` drops a player's kills.
