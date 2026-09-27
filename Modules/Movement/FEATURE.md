# Movement — Feature

## Purpose

Reusable, game-agnostic player movement: a registry of named locations
(position + camera angle), teleport one player / a list of players, set a
player's camera angle. Compiled into consuming DLLs as source; no Rift Roulette
dependency.

## Public operations

| Op | Doc |
|---|---|
| `MovementService.TeleportTo(player, location, mode)` | `MovementService.md` |
| `MovementService.TeleportPlayers(players, location, mode)` | `MovementService.md` |
| `MovementService.SetViewAngle(player, angle)` | `MovementService.md` |
| `MovementService.Where(player)` | `MovementService.md` |
| `MovementService.Locations` (`Register`, `Unregister`, `TryGet`, `IsSaved`, `List`) | `LocationRegistry.md` |

Type: `MovementLocation(Name, Position, Angle)`, with pure
`Offset(local, name)`: a spot moved by (forward, right, up) in the
location's own yaw frame (`MovementLocation.md`). Game code builds groups of
spots from one anchor this way, so moving the anchor moves the group.

## State

`MovementService.Locations`: code-registered locations (protected) plus
`/mv_save` locations (until reload). One registry per DLL.

## Units

| File | Role |
|---|---|
| `MovementService.cs` | Teleport / camera ops |
| `LocationRegistry.cs` | Pure name registry (unit tested) |
| `MovementLocation.cs` | Location record + `Offset` (unit tested) |
| `MovementPlugin.cs` | `/mv_*` admin commands (thin wrappers) |
| `Movement.projitems` | Registry + service (no commands) |
| `MovementCommands.projitems` | Plugin class |

## Lifecycle vs commands

- Game code passes typed `MovementLocation`s in Clean mode (Rift Roulette:
  watch-spot sends and rift-start moves, each player to their own slot
  spot offset from a `RiftRouletteLocations` anchor, `Round/SlotSpots`).
- Game code registers its locations so admins can reach them by name
  (`RiftRouletteLocations.RegisterAll()` from `SessionPlugin.OnLoad`).
- Admin commands (`/mv_list`, `/mv_where`, `/mv_tp`, `/mv_tp_team`,
  `/mv_tp_all`, `/mv_angle`, `/mv_save`, `/mv_remove`) call the same ops in
  Debug mode. See `RiftRoulette/reference/admin-commands.md`.

## Consumers

- `RiftRoulette.dll` imports both projitems.
- `Tests/Modules.Tests` imports `Movement.projitems` for `LocationRegistry`
  and `MovementLocation.Offset`.
