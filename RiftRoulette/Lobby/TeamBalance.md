# TeamBalance

Pure team placement (Stage 13b, moved to Lobby in Stage 13c). Used for new
connections (`LobbyService.AdmitPlayer`), Random mode match start, and late
joiners.

## Operations

| Op | Behavior |
|---|---|
| `Even(current, rng)` | Starts from each player's current team (Steam ID to team number). Players not on Sapphire or Amber go to the smaller team. Then moves random players off the bigger team until the sizes differ by at most one. Players already balanced keep their team. Returns Steam ID to team number. |
| `SmallerTeam(teams, rng)` | Team number with fewer entries; random on a tie. |

Team numbers come from `RiftRouletteTeams` (Sapphire 3, Amber 2).
