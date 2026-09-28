# AdminSeatRuleTests

Unit tests for `Lobby/AdminSeatRule`.

- `CanConnect`: a non-admin gets in below 12 playing and is refused at 12; an
  admin always gets in.
- `CanStand`: only when fewer than 12 are playing.
- `SeatOnJoin`: every admin is seated on join; a non-admin never is.
- `ShouldRoam`: only with 0 participants.
- A custom cap is respected; the default cap is 12.
