# SpotCheck

Admin tooling for the per-slot spots (`SlotSpots`): list them, and walk an
admin through one group in game to catch spots inside walls or without a
floor. Static; the only state is the running walk handle.

## Logs

`Spots` feature log (`spots-YYYYMMDD.log`). One line per visited spot
(`Spot ok` Information, or `Spot moved the pawn` Warning, which is also
copied to master), plus start / finish lines with the warning count.

## Operations

| Op | Behavior | Returns |
|---|---|---|
| `TryParseGroup(name, out group)` | `watch`, `sapphire`, `amber` (any case) | bool |
| `Spots(group, side)` | All 13 slot spots of the group: watch spots around `RoundLocations.WatchFor(side)`, or fight spots around that side's Sapphire / Amber start | locations |
| `Describe(side)` | One line per slot: `<SIDE> Slot=N \| watch=(x,y,z) \| sapphire=(...) \| amber=(...)` | lines |
| `Walk(player, timer, group, side, mode)` | Refused while a rift runs or another walk is running. Otherwise a timer sequence: every `StepSeconds` (1.5 s) it gives the player `WatchGuard.Grace` and teleports them to the next spot; on the following step it compares where the pawn is with the target. More than `Tolerance` (32 units) sideways or up / down is a Warning (the game pushed the pawn out of geometry, or it fell). After the last spot, or when the pawn is gone or dead, the player is sent back up (`WatchSpot.SendUp`) and gets a console summary | reply text |

## Invariants

- Uses the same `SlotSpots` table and anchors as the real teleports, so what
  it checks is what players get.
- Only moves the calling admin. The grace stops `WatchGuard` from pulling a
  restrained admin back up while they visit rift starts.
- The walk handle dies with the plugin on hot reload.
