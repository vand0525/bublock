# Arena module

## Purpose

Contained fight areas for brawler-style game types: each team gets an
anchor, each slot an offset from it (a whole team lands in rows instead of
on one point), and an optional bounds box keeps players inside. The arena is a data asset the game type embeds (`arena.json`), so a
new arena is a new file, not new code. Added in the redock fork with Gun
Game.

## Files

| File | Role |
|---|---|
| `ArenaSpots.cs` | The arena: anchors, offsets, `For(team, slot)`, JSON asset parsing (pure, tested) |
| `ArenaService.cs` | Teleport a player to their spot (now, or next tick from a spawn hook) |
| `Arena.projitems` | Compiles both into a consumer |

## Public operations

See `ArenaSpots.md` and `ArenaService.md`.

## State

None static: the game type holds its `ArenaSpots` (usually a lazy static
loaded from its embedded asset).

## Commands and lifecycle

No plugin class, no commands. A game type calls
`ArenaService.SendToArenaNextTick` from its `player_spawn` hook and can
wrap `SendToArena` in its own admin command.

## Dependencies

`Modules/Movement` (`MovementLocation`, `MovementService.TeleportTo`),
`Modules/Teams` (`DeadlockTeams`), `Shared` (logging).

## Known arenas

| Arena | Where | Source |
|---|---|---|
| mid lane brawl | the middle lane of `dl_midtown` (lane 4): Amber on the south street at (0, -2000, 376), Sapphire on the north street at (0, 2000, 376), the fight through the center; bounds x ±1500, y ±2800 | `GunGame/Data/arena.json`; all 26 spots pass `scripts/check-arena.py` |
