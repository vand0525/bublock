# MovementService

Core teleport and camera operations. Static service: no plugin-class
dependency, no timers, created on first use.

## State

- `Locations`: the shared `LocationRegistry` (one per DLL). Filled by game
  code (e.g. `RiftRouletteLocations.RegisterAll()`) and `/mv_save`.

## Operations

Ops that change players take `ExecutionMode mode = Clean` and log to the
`Movement` feature log (`movement-YYYYMMDD.log`, prefix `[<Dll>.Movement]`)
at `Debug`, so lines only appear for admin (Debug) invocations. Player lines
carry a `PlayerRef`.

| Op | Behavior | Returns |
|---|---|---|
| `TeleportTo(player, location, mode)` | `pawn.Teleport(location.Position, angles: null, velocity: Vector3.Zero)`, then `SetViewAngle(player, location.Angle)`. Skips (and logs) when the player has no hero pawn | `bool` moved |
| `TeleportPlayers(players, location, mode)` | `TeleportTo` for each player; players without a pawn are skipped. No alive check | count moved |
| `SetViewAngle(player, angle)` | Sends `CCitadelUserMsg_SetClientCameraAngles` (pitch, yaw, roll) to that player only | — |
| `Where(player)` | Hero pawn `Position` and `EyeAngles` | `null` if no pawn |

## Dangerous constraints

- `Teleport(angles: ...)` rotates the model, not the camera; the camera is set
  separately with `SetViewAngle`. `TeleportTo` passes `angles: null`.
- Teleporting a dead pawn is not guarded here. Callers that must skip dead
  players check
  `IsAlive` first, as `RiftService.SendPlayersUp` does.
- Game-thread only.
