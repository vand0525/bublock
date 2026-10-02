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
| `IsFlyCam(player)` | Observer mode `Roaming` with no target: the viewer pressed C (the server cannot put the client there). |
| `ViewAngle` | `SchemaAccessor<Vector3>` for `CBasePlayerPawn.v_angle` (the observer pawn has no `EyeAngles`; that is on the hero pawn class). In the self-test schema checks. |
| `Pose(player)` | The observer's position and view angle (`ViewAngle`; null when the field is not in the schema), or null when not observing. |
| `Follow(player, target, mode)` | Sets `ObserverMode_t.InEye` (the game's PlayerView) and `SetObserverTarget(target hero pawn)`. True if the server accepted it; refused logs Info `Server target refused` and returns false. Logs Debug; false without a change when either pawn is missing. |
| `Park(player, position, angle, timer, mode)` | Timed sequence. Now: `ObserverMode_t.Roaming` on the server. +`TeleportDelaySeconds` (0.25 s): teleports the observer pawn to `position` with `angle` and zero velocity. +`AngleDelaySeconds` (0.5 s) and +`AngleRepeatSeconds` (1.0 s): `MovementService.SetViewAngle(player, angle)` (kept as a fallback). Each step re-finds the player by Steam ID and does nothing if they left or are no longer observing. Logs Debug `Park started`. After the last step, in fly cam with the view angle (`Pose`) more than 3 degrees off (`SpectateRule.AngleClose`): Information `Park angle did not take Wanted= Got=`; otherwise Debug `Parked Position= Angle= After= AngleAfter= Mode=`. Returns false (nothing sent) when not observing. |
## Side effects

- Changes only the spectating player's camera. Never touches the watched
  player.

## Invariants

- Entities are compared by `EntityHandle`, never `EntityIndex`.
- `Follow` always sets the mode before the target.
- `Park` order is fixed: teleport (with the angle), then the angle message.
- Sends no client commands.

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
- The fly cam ignores `CCitadelUserMsg_SetClientCameraAngles` (the
  message behind `SetViewAngle` and the API's `SetCameraAngles`): the
  view kept the angle the admin joined with (2026-10-02). The angle is
  passed to the observer teleport instead; whether that turns the fly cam
  is to confirm in game (`Park angle did not take` in `spectate-*.log`
  says it did not).
- `spec_goto` cannot be used either, and it ignores pitch and yaw.
- `v_angle` follows the fly cam view on the observer pawn (the saved
  framing logged the admin's real pitch, 2026-10-02).
