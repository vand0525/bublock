# AdminSeatRule

Pure rules for the reserved admin seat (Stage 13f). Unit tested. `playing`
is the number of connected participants (humans not in the seat), not
counting the player being decided about.

## Operations

| Op | Behavior | Returns |
|---|---|---|
| `CanConnect(isAdmin, playing, cap = 12)` | Admins always; anyone else only while `playing < cap` | bool |
| `CanStand(playing, cap = 12)` | Leaving the seat to play needs `playing < cap` | bool |
| `SeatOnJoin(isAdmin)` | Every admin starts in the seat (spectating) on connect, so an admin joining never changes the teams; `dw_seat_play` joins a team | bool |

`PlayerCap` = 12 (6 per team, `citadel_team_size 6`).
