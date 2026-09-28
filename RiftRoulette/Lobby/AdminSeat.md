# AdminSeat

The reserved 13th connection for admins (Stage 13f). The server allows 13
connections (`maxplayers 13`, browser shows 12); only an admin may take the
13th. A seated admin is on the spectator side and is not a participant
(`Participants`), so they get no team, hero, round, stats, or auto-start
count. Static state, one per DLL load.

## State

- `Seated`: Steam IDs in the admin seat. `SeatedCount`, `IsSeated(steamId)`.
- `SpectatorTeam = 1` (the Source convention; to confirm in game).

## Operations

| Op | Behavior | Returns |
|---|---|---|
| `AllowConnect(steamId, name)` | `AdminSeatRule.CanConnect(isAdmin, playing)`. Refusal logs a Warning (`Connection refused, player slots full`); an admin connecting while 12 play logs an Information line | bool |
| `SeatOnJoin(player)` | The player is an admin (`AdminSeatRule.SeatOnJoin`): every admin starts spectating on connect | bool |
| `Sit(player, timer, mode)` | See below | reply text |
| `Stand(player, timer, mode)` | Refuses if not seated or 12 already playing (`AdminSeatRule.CanStand`). Otherwise removes the seat, resets the stream camera state (`StreamCam.Forget`), and runs `LobbyService.AdmitPlayer` (smaller team, Skyrunner, watch spot + restraint, Random joiner / 1v1 setup souls, auto-start). `HeroCheckSeconds` (2 s) later logs `Admin hero after leaving the seat TeamNum= Pawn=`, or a Warning `Admin has no hero after leaving the seat` if no hero pawn spawned from the observer | reply text |
| `Forget(steamId)` | Drops the seat and the stream camera state (disconnect) | — |
| `Restore(mode)` | After a hot reload (which empties `Seated`): every connected `AdminAuth` player whose pawn is the `observer` pawn (`SpectateService.IsObserving`) is added back to `Seated`, logged `Admin seat restored after reload TeamNum=`. Called from `LobbyPlugin.OnLoad(isReload: true)` | count restored |
| `Describe()` | Playing / cap, seated count, `maxplayers` and `sv_visiblemaxplayers`, then per seated player: slot, name, `TeamNum`, pawn present | lines |

### Sit

Works at any time, including during a rift round.

1. Adds to `Seated` (refuses if already seated).
2. Releases the draft pick (`DraftState.Release`) and redraws the boards.
3. `RandomModeService.Forget`, `DuelService.Forget` (leaves the 1v1
   queue), `RestraintService.Release` (a spectator isn't restrained) and
   `WatchGuard.Forget`.
4. Logs `Admin seat taken, spectating next tick Phase=` and a master
   line.
5. On the next tick (`BecomeObserver`, outside the connect or command
   callback): finds the player again by Steam ID (skips, logged, if gone
   or no longer seated), then `ChangeTeam(SpectatorTeam, false)` and
   `MakeObserver()` (removes the hero pawn, clears it from the controller,
   spawns an observer pawn). Starts the stream camera's seat grace
   (`StreamCam.Seated`). Logs `Admin spectating TeamNum= HeroPawn=
   Observer=` (the controller's pawn designer name). The client starts in
   the directed view, where the stream camera cannot move it; the admin
   presses C for fly cam (the server cannot send `spec_mode`).
6. Refreshes the stats boards and runs `AutoStartService.Check` (the match
   may auto-end if fewer than 2 participants remain). The teams left
   behind are evened at the next Random intermission
   (`RandomModeService.PrepareRound`).

## Invariants

- Only Steam IDs accepted by `AdminAuth` can be seated (commands check it).
- Seat changes never touch gameplay for other players except through the
  normal lobby admit and auto-start paths.

## Deadworks constraints

- `OnClientConnect` returning `false` refuses the connection (to confirm in
  game that the client sees a clean refusal).
- Spectators cannot type in game chat; seat commands are meant for the
  server console or client console (`dw_seat_play`, `dw_seat_spec`).
- `ChangeTeam(1, false)` alone leaves the hero pawn alive on team 1
  (`PawnLeft=True` every time). That dropped the admin's client 12-23 s
  later, mid-rift (05:26) and between rounds (09:17, 09:18), and left the
  other team empty. `MakeObserver()` is the API's spectate call; the
  mid-round refusal was removed with it. Confirmed at 09:30: the client
  stays connected (`HeroPawn=False Observer=observer`). Still to confirm
  that `Stand` spawns a hero from the observer pawn (the hero check logs
  it); the API has no respawn call. `seat_spec` stays console-only.
