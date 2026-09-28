# MovementPlugin

Thin Deadworks plugin class (`Name` = `Movement`) exposing the admin `/mv_*`
commands. No hooks and no state of its own; locations live in
`MovementService.Locations`.

## Commands

All admin-only (`AdminCommand.Authorize`; server console trusted), run their
ops in `ExecutionMode.Debug`, and reply through `AdminCommand.Reply`. Errors
are thrown as `CommandException` (shown to the caller). Accepted and rejected
calls are logged in the `Movement` feature log.

`[slot]` defaults to `-1`, meaning the caller; from the server console a slot
is required.

| Command | Op | Result |
|---|---|---|
| `/mv_list` | `Locations.List` | Count, then `name | Position | Angle` (plus `saved`) per location |
| `/mv_where [slot]` | `MovementService.Where` | Player name, slot, pawn position, eye angles |
| `/mv_tp <location> [slot]` | `MovementService.TeleportTo` | Teleports one player; error for unknown location, empty slot, or no hero |
| `/mv_tp_team <team> <location>` | `MovementService.TeleportPlayers` | Teleports every player whose controller `TeamNum` equals the number (Rift Roulette: Amber 2, Sapphire 3); replies moved/total |
| `/mv_tp_all <location>` | `MovementService.TeleportPlayers` | Teleports every player; replies moved/total |
| `/mv_angle <pitch> <yaw> <roll> [slot]` | `MovementService.SetViewAngle` | Sets the camera angle; logged at Debug |
| `/mv_save <name>` | `Locations.Register(saved: true)` | Saves the caller's pawn position and view (pitch, yaw, roll 0) until the DLL reloads. In-game only. Refuses invalid names and built-in (code) names; replacing an earlier saved name is allowed. Logged at Information |
| `/mv_remove <name>` | `Locations.Unregister` | Removes a saved location; built-in locations are refused |

## Invariants

- Module commands take only generic arguments: team **numbers**, not Rift
  Roulette team names.
- Handlers take `CCitadelPlayerController?` so the server console can run
  them (`dw_mv_*`).
- `/mv_tp_team` relies on the controller's `TeamNum`; confirmed in game.
