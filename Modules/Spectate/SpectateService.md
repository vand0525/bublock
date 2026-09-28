# SpectateService

Drives a spectating player's camera: follow a player's view or park a free
camera at a position. Game agnostic: the caller decides whom to watch and
where to park (Rift Roulette: `RiftRoulette/Lobby/StreamCam`).

## Operations

| Operation | Effect |
|---|---|
| `Observer(player)` | The player's pawn if it is the `observer` pawn (`DesignerName == "observer"`), else null. |
| `IsObserving(player)` | `Observer(player) != null`. |
| `Current(player)` | The observer's `ObserverTarget`, or null. |
| `Mode(player)` | The observer's `ObserverMode`, `None` when not observing. |
| `IsWatching(player, target)` | True when the current target is `target` (compared by `EntityHandle`). |
| `Follow(player, target, mode)` | Sets `ObserverMode_t.InEye` (the game's PlayerView) and `SetObserverTarget(target hero pawn)`. True if the server accepted it; refused logs Info `Server target refused` and returns false. Logs Debug; false without a change when either pawn is missing. |
| `Park(player, position, angle, timer, mode)` | Timed sequence. Now: `ObserverMode_t.Roaming` on the server. +`TeleportDelaySeconds` (0.25 s): teleports the observer pawn to `position` with no angles and zero velocity. +`AngleDelaySeconds` (0.5 s) and +`AngleRepeatSeconds` (1.0 s): `MovementService.SetViewAngle(player, angle)`. Each step re-finds the player by Steam ID and does nothing if they left or are no longer observing. Logs Debug `Park started` and, after the last step, `Parked Position= Angle= After= Mode=`. Returns false (nothing sent) when not observing. |
| `IsParkedAt(player, position, tolerance)` | `SpectateRule.ParkCheck` on the observer: roaming, no `ObserverTarget`, within `tolerance` (default `ParkTolerance`, 1500 units) of `position`. False when not observing. |
| `IsManualMove(player, position, tolerance)` | `SpectateRule.IsManualMove` on the observer: roaming, no `ObserverTarget`, farther than `tolerance` from `position` (the viewer flew away). False when not observing. |

## Side effects

- Changes only the spectating player's camera. Never touches the watched
  player.

## Invariants

- Entities are compared by `EntityHandle`, never `EntityIndex`.
- `Follow` always sets the mode before the target.
- `Park` order is fixed: teleport, then the angle.
- Sends no client commands.
- `ParkTolerance` is wide on purpose, so a small manual nudge in fly cam
  does not count as a failed park.

## Dangerous Deadworks constraints

- Never use `IsValidObserverTarget`: it rejects `TeamNum == 3`, which is a
  playing team (Sapphire) in Deadlock.
- The server cannot switch the client's spectator camera by command:
  `spec_mode`, `spec_player`, `spec_next`, `spec_prev` are
  `clientcmd_can_execute`, not `server_can_execute`, and the client
  refuses them from `Server.ClientCommand` (`Cannot execute concommand
  spec_mode: missing required FCVAR flag`, 2026-09-28).
- `SetObserverMode(Roaming)` on the server does not switch the client's
  camera either. A teleport of the observer pawn only moves the camera
  when the viewer is already in fly cam (C in game, confirmed in game).
- An angle sent in the same tick as the teleport is ignored in fly cam, so
  the angle goes out after the teleport, twice. If pitch 89 still does not
  hold, the fallback is writing the observer's view angle field through
  `SchemaAccessor` (field name to verify first).
- `spec_goto` cannot be used either, and it ignores pitch and yaw.
