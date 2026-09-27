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
| `Follow(player, target, mode)` | Sets `ObserverMode_t.InEye` (the game's PlayerView) and `SetObserverTarget(target hero pawn)`. True if the server accepted it. If refused, sends `ClientFollow` and returns false. Logs Debug; false without a change when either pawn is missing. |
| `ClientFollow(player, target, mode)` | `Server.ClientCommand(player.Slot, "spec_player <target slot>")`. Logs Info. |
| `Park(player, position, angle, mode)` | Sets `ObserverMode_t.Roaming`, teleports the observer pawn to `position` with `angle` and zero velocity, then `MovementService.SetViewAngle(player, angle)`. Logs Debug with the requested and resulting position. False when not observing. |

## Side effects

- Changes only the spectating player's camera. Never touches the watched
  player.

## Invariants

- Entities are compared by `EntityHandle`, never `EntityIndex`.
- `Follow` always sets the mode before the target.

## Dangerous Deadworks constraints

- Never use `IsValidObserverTarget`: it rejects `TeamNum == 3`, which is a
  playing team (Sapphire) in Deadlock.
- `spec_player` is a client command marked `clientcmd_can_execute`
  ("Spectate a player by name or slot"); it only helps when the server
  target is refused.
- Whether teleporting a `Roaming` observer pawn moves the client camera is
  still to be confirmed in game; the `After=` field in the Park log shows
  where the pawn ended up.
