# WatchSpot

The spot up top where inactive players wait and watch. It sits
above the rift being fought (or the next one), on the skybox floor at the
old draft height, and replaced every teleport to the fixed `draft` spot.

## State

- `_boardSide`: the side the boards were last drawn at. Starts
  as `Side` on first use.

## Operations

| Operation | Effect |
|---|---|
| `Side` | `WatchSpotRule.SideFor(RiftService.IsRunning, CurrentSide, NextSide)`. |
| `BoardSide` | Side the boards are drawn at (`BoardLayout.Origin` uses it). |
| `Location(side)` | `RoundLocations.WatchFor(side)` (`watch_green` / `watch_yellow`). |
| `SendUp(player, mode, side = null)` | `RestraintService.Restrain(player)`, then teleports to the player's own slot spot `SlotSpots.Watch(Location(side ?? Side), player.Slot)` (same camera angle as the anchor, facing the board) and gives the player `WatchGuard.Grace`. No banner. Returns the teleport result. |
| `MoveAllUp(mode)` | When no rift runs: sends every live participant up to `Side` and moves the boards there. Returns how many moved; 0 while a rift runs. Logs Info. |
| `RefreshBoards(side, mode)` | If `side` differs from `BoardSide`, stores it and calls `Boards/BoardService.Redraw` (welcome, stats and betting boards). False when unchanged. |

## Callers

- `RoundFlow.SendPlayersUp` (round end): `SendUp(..., NextSide)` for
  live players, `Restrain` for dead ones, then `RefreshBoards(NextSide)`.
  `NextSide` is passed explicitly because the rift is still marked running
  at that moment.
- `LobbyPlugin` spawn hook (every spawn / respawn), `LobbyService.AdmitPlayer`,
  `Lobby/LobbyHeroes.ReturnAll`: `SendUp(player)` (one player at a time).
- `RiftPlugin` `/rift_next`: `MoveAllUp` (only moves anyone when idle).
- `WatchGuard.Check`: `SendUp(player, mode, side)`.

## Timing invariant

The spot only changes when players are sent back up after a round (or by
`/rift_next` while idle). Mid-round deaths go above the rift still being
fought; the boards stay put until the next intermission.

## Dangerous constraints

- Watch spots are at z = 1536.06, the same height as the old `draft` spot
  (the skybox floor). That the floor holds above both rifts is still to be
  confirmed in game. Every slot spot is at the same height (watch offsets
  have up = 0), so `WatchGuard` keeps using the anchor Z.
- Never teleport a group to the anchor itself; every up-top teleport goes
  through `SendUp` and so through the slot table.
