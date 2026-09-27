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
| `SpectateService.ClientFollow(player, target, mode)` | `SpectateService.md` |
| `SpectateService.Park(player, position, angle, mode)` | `SpectateService.md` |
| `SpectateRule.Choose(currentId, killerId, candidates)` | `SpectateRule.md` |
| `SpectateRule.LookDown(yaw)` | `SpectateRule.md` |

## State

None. The consumer keeps whom it follows and where it parked.

## Units

| File | Role |
|---|---|
| `SpectateRule.cs` | Pure choice (keep, killer, any, park) and the straight-down angle |
| `SpectateService.cs` | Observer-services calls, client-command fallback, park teleport |
| `Spectate.projitems` | Service and rule (no commands) |

## Lifecycle vs commands

- No lifecycle of its own. Rift Roulette's `Lobby/StreamCam` calls it every
  2 s, on deaths and on big ultimates; the `spec_*` admin commands in
  `Lobby/LobbyPlugin` call the same ops in Debug mode. See
  `RiftRoulette/reference/admin-commands.md`.

## Consumers

- `RiftRoulette.dll` imports `Spectate.projitems`.
