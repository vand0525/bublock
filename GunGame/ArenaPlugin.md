# ArenaPlugin (GunGame)

Thin host for the arena.

## Hooks

- `OnLoad`: `ContainPlayers` every `ContainSeconds` (1 s).
- `player_spawn` and `player_respawned`: `GunGameService.OnSpawn` (arena
  spot on the next tick, pending build).

## Commands

| Command | Who | Calls | Reply |
|---|---|---|---|
| `/gg_arena <slot>` | admin | `ArenaService.SendToArena` (Debug) | where the player went, or that they have no hero or team |
