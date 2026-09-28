# Spectate — Feature

## Purpose

Reusable, game-agnostic spectator camera control: follow a player's view
(in-eye, what that player sees) or park a free camera at a position and
angle. Built for the admin's stream camera.

## Public operations

| Op | Doc |
|---|---|
| `SpectateService.Observer / IsObserving / Current / Mode / IsWatching` | `SpectateService.md` |
| `SpectateService.Follow(player, target, mode)` | `SpectateService.md` |
| `SpectateService.Park(player, position, angle, timer, mode)` | `SpectateService.md` |
| `SpectateService.IsParkedAt(player, position, tolerance)` | `SpectateService.md` |
| `SpectateService.IsManualMove(player, position, tolerance)` | `SpectateService.md` |
| `SpectateRule.Choose(currentId, killerId, candidates)` | `SpectateRule.md` |
| `SpectateRule.LookDown(yaw)` | `SpectateRule.md` |
| `SpectateRule.ParkCheck(roaming, hasTarget, distance, tolerance)` | `SpectateRule.md` |
| `SpectateRule.IsManualMove` / `ManualActive` / `FollowReady` | `SpectateRule.md` |

## State

None. The consumer keeps whom it follows and where it parked. `Park`
schedules its later steps on the caller's `ITimer`.

## Camera modes

The server's `SetObserverMode` does not switch the client's camera, and the
client refuses `spec_mode` / `spec_player` sent by the server (they are not
`server_can_execute`). The viewer must press C for fly cam before a teleport
of the observer pawn (`Park`) moves it.

## Units

| File | Role |
|---|---|
| `SpectateRule.cs` | Pure choice (keep, killer, any, park), the straight-down angle, the parked check |
| `SpectateService.cs` | Observer-services calls and the timed park |
| `Spectate.projitems` | Service and rule (no commands) |

## Lifecycle vs commands

- No lifecycle of its own. Rift Roulette's `Lobby/StreamCam` calls it every
  2 s, on deaths and on big ultimates; the `spec_*` admin commands in
  `Lobby/LobbyPlugin` call the same ops in Debug mode. See
  `RiftRoulette/reference/admin-commands.md`.

## Consumers

- `RiftRoulette.dll` imports `Spectate.projitems`.
