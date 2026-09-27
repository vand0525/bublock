# RandomPlugin

Thin plugin class for Random mode (`Name` = "Rift Roulette Random").

## Hooks

- `player_respawned` and `player_spawn`: when players are pending, on the
  next tick it finds the player again by Steam ID and calls
  `RandomModeService.ApplyPending` (Clean). Skips bots. `player_spawn`
  covers a joiner's first spawn; both can fire for one respawn, and the
  second call finds nothing pending. Runs alongside Lobby's `player_spawn`
  teleport. `player_respawned` first calls
  `SelfTest/EventCounters.Hit("player_respawned")` (self-test hook check).

## Commands

Admin: `AdminCommand.Authorize` with the `Random` log (server console
trusted), Debug mode, `[Random]` replies.

| Command | Calls | Reply / errors |
|---|---|---|
| `/random_status` | `RandomModeService.Describe` | config line, then one line per player (team, hero, build, pending) |
| `/random_reroll` | `RandomModeService.PrepareRound(Timer, Debug)` | `Rerolled: N swapped, M pending`; error unless Random mode and the match is in intermission |
