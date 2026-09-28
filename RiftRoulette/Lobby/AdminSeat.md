# AdminSeat

The reserved 13th connection for admins. The server allows 13
connections (`maxplayers 13`, browser shows 12); only an admin may take the
13th. A seated admin is on the spectator side and is not a participant
(`Participants`), so they get no team, hero, round, stats, or auto-start
count. Static state, one per DLL load.

A seated admin is in one of two states:

- **Spectating** (observer pawn, stream camera) while any participant is
  connected.
- **Roaming** while no participants are connected
  (`AdminSeatRule.ShouldRoam`): Abrams (`RoamHero = Heroes.Atlas`) on
  Amber (`RoamTeam`), placed straight in front of the welcome sign facing
  it, not restrained (free to move, abilities and items on), and
  invisible (`RoamModifier = modifier_invis`). Still seated, so still not
  a participant: teams, auto-start, stats, balance, the waiting reminder,
  the draft, hero enforcement and `WatchGuard` all ignore them, and the
  stream camera only runs for observers.

`dw_seat_play` (`Stand`) leaves the seat and plays as a normal participant.
`/seat_roam` (`RoamNow`) starts roaming right away, or puts a roaming admin
back in front of the sign, while nobody plays.

## State

- `Seated`: Steam IDs in the admin seat. `SeatedCount`, `IsSeated(steamId)`.
- `Roaming`: seated admins meant to be roaming. `IsRoaming(steamId)`.
- `CloakGeneration`: per roaming admin, the latest cloak; an older reapply
  timer does nothing.
- `SpectatorTeam = 1` (the Source convention; to confirm in game).
- `RoamModifierSeconds = 3600`: the cloak's duration.

## Operations

| Op | Behavior | Returns |
|---|---|---|
| `AllowConnect(steamId, name)` | `AdminSeatRule.CanConnect(isAdmin, playing)`. Refusal logs a Warning (`Connection refused, player slots full`); an admin connecting while 12 play logs an Information line | bool |
| `SeatOnJoin(player)` | The player is an admin (`AdminSeatRule.SeatOnJoin`): every admin starts spectating on connect | bool |
| `Sit(player, timer, mode)` | See below. A roaming admin switches to spectating instead (`Admin roam ended`); the next join / leave sync may roam them again if the server is still empty | reply text |
| `SyncSoon(timer, mode)` | `Sync` after `HeroCheckSeconds` (2 s). Hero swaps made inside `OnClientFullConnect` are lost, so switches always run from a timer. Called on admin join, every `AdmitPlayer` / `RemovePlayer`, a new statue, and after `Restore` | — |
| `Sync(timer, mode)` | For each connected seated admin: roam when `ShouldRoam(Participants.Humans().Count)` and not roaming yet; spectate when roaming and a participant is connected; otherwise nothing | — |
| `RoamNow(player, timer, mode)` | `/seat_roam`. Refuses when not seated (`Use dw_seat_spec first`) or when anyone plays (`ShouldRoam` false). Already roaming: `PlaceAndCloak` (teleport back in front of the sign, cloak again). Otherwise `Roam` | reply text |
| Roam (private) | Adds to `Roaming`, resets the camera state, releases restraint and `WatchGuard`, `SelectHero(Atlas)` then `ChangeTeam(Amber, true)`; logs `Admin roaming, server empty` and a master line. 2 s later `PlaceAndCloak(retry: true)` | — |
| Spectate (private) | Removes from `Roaming`, removes the cloak (`modifier_invis`) from the hero pawn, logs `Admin roam ended, spectating shortly Playing= Uncloaked=`, then `BecomeObserver` after `UncloakSeconds` (0.5 s; the same path as `Sit`). Deleting the pawn with the cloak still on left the client's red invisibility tint on screen while spectating, so the removal has to reach the client before the pawn goes | — |
| `PlaceAndCloak(steamId, timer, mode, retry)` | Only while roaming. No living hero: with `retry`, Warning `Roaming admin has no hero yet, selecting again`, `SelectHero(Atlas)` and one more try 2 s later; without, Warning `Roaming admin has no hero, not placed`. The retry call skips an admin already cloaked (the spawn placed them; not pulled back). Otherwise teleports, without restraint, straight in front of the welcome sign facing it (`WatchLayout.WelcomeFront` of the anchor at `WatchSpot.BoardSide`, where the boards are) and `Cloak`s. 2 s later (`FloorCheckSeconds`) checks the fall: a roaming pawn more than 300 units below the anchor (`WatchGuardRule.IsBelow`) gets Warning `Roaming admin fell from the welcome spot, back to the watch slot` and is teleported to `SlotSpots.Watch(anchor, slot)`. Also run by `player_spawn` next tick | — |
| `Cloak(player, timer, mode)` | Only while roaming with a living hero. `RestraintService.AddModifier(pawn, modifier_invis, 3600)` (Information `Roaming admin cloaked`, or Warning `Roaming admin cloak refused`), bumps `CloakGeneration`, and 3600 s later cloaks again if still the latest | — |
| `Stand(player, timer, mode)` | Refuses if not seated or 12 already playing (`AdminSeatRule.CanStand`). Otherwise removes the seat and the roam state (and the cloak modifier), resets the stream camera state (`StreamCam.Forget`), and runs `LobbyService.AdmitPlayer` (smaller team, Skyrunner, watch spot + restraint, Random joiner / 1v1 setup souls, auto-start). `HeroCheckSeconds` (2 s) later logs `Admin hero after leaving the seat TeamNum= Pawn=`, or a Warning `Admin has no hero after leaving the seat` if no hero pawn spawned from the observer | reply text |
| `Forget(steamId)` | Drops the seat, the roam state and the stream camera state (disconnect; also a map-change reconnect, before `Sit`) | — |
| `ResetForMap()` | From `OnStartupServer` on every map start: Information `Admin seat reset for the new map Seated= Roaming=`, forgets each seated admin's stream camera state and clears `Seated`, `Roaming` and `CloakGeneration` (pawns died with the old map; admins are seated again on reconnect) | — |
| `Restore(timer, mode)` | After a hot reload (which empties `Seated` and `Roaming`): every connected `AdminAuth` player goes back into `Seated`. One on a hero pawn (not observing) is roaming again and is re-cloaked next tick in place (not teleported). Logged `Admin seat restored after reload TeamNum= Roaming=`. Then `SyncSoon` settles roam vs spectate. Called from `LobbyPlugin.OnLoad(isReload: true)` | count restored |
| `Describe()` | Playing / cap, seated and roaming counts, `maxplayers` and `sv_visiblemaxplayers`, then per seated player: slot, name, `TeamNum`, pawn present, roaming | lines |

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
- Roaming admins stay in `Seated`; never make a roaming admin a
  participant. Once any participant connects the admin spectates.
- The cloak is one long modifier put back when it runs out (and on every
  respawn and reload), never re-added every frame. Invisibility may drop
  when the admin attacks, until the next reapply.
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
  that `Stand` and roaming spawn a hero from the observer pawn (the hero
  check and `PlaceAndCloak` log it); the API has no respawn call.
  `seat_spec` stays console-only.
