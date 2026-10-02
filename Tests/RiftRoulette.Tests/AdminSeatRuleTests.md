# AdminSeatRuleTests

Unit tests for `Lobby/AdminSeatRule`.

- `CanConnect`: a non-admin gets in below 12 playing and is refused at 12; an
  admin always gets in.
- `CanStand`: only when fewer than 12 are playing.
- `JoinMode`: an admin rejoins in their last mode (spectate, roam, play),
  plays with no last mode, and never plays as the 13th connection (12 others
  playing: play becomes spectate, roam stays roam); a non-admin always plays.
- `ShouldRoam`: only with 0 participants.
- A custom cap is respected; the default cap is 12.
