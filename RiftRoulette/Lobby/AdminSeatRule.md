# AdminSeatRule

Pure rules for the reserved admin seat. Unit tested. `playing`
is the number of connected participants (humans not in the seat), not
counting the player being decided about.

## Operations

| Op | Behavior | Returns |
|---|---|---|
| `CanConnect(isAdmin, playing, cap = 12)` | Admins always; anyone else only while `playing < cap` | bool |
| `CanStand(playing, cap = 12)` | Leaving the seat to play needs `playing < cap` | bool |
| `JoinMode(isAdmin, othersPlaying, last, cap = 12)` | Non-admin: `Play`. Admin: `last` (the mode they were in: `Play`, `Spectate`, `Roam`), `Play` when unknown; `Play` becomes `Spectate` when `othersPlaying >= cap` (the 13th connection never takes a team slot) | `AdminMode` |
| `ShouldRoam(participants)` | Unused by seat mode (roam is manual). Kept for tests: true only when `participants == 0` | bool |

`AdminMode` (`Play`, `Spectate`, `Roam`) is declared here.

`PlayerCap` = 12 (6 per team, `citadel_team_size 6`).
