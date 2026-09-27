# WatchGuard

Keeps waiting (restrained) players at the watch spot. The game's escape-menu
Unstuck cannot be blocked (no known command or convar), so a player who
drops below the watch spot, by falling or by Unstuck, is sent back up.
Also logs what console commands restrained players send, to find what
Unstuck sends.

## State

- `GraceUntil`: Steam ID -> UTC time until which the player is not checked.
- `LastCommand`: (Steam ID, command) -> last logged time, for the repeat
  filter.
- `_frame`: frame counter for `Tick`.
- `Rescues`: rescues since load (shown by `MatchProbe`).

## Operations

| Op | Effect |
|---|---|
| `Tick(mode)` | Called every simulating frame by `GameLoopPlugin.OnGameFrame`; runs `Check` every `CheckEveryFrames` (16) frames |
| `Check(mode)` | No-op when nobody is restrained. Otherwise, for every alive participant (`Participants.Humans()`) that `RestraintService.IsRestrained` and is out of grace: if `WatchGuardRule.IsBelow(z, spot z)` for the current `WatchSpot.Side`, sends them back with `WatchSpot.SendUp(player, mode, side)` (no banner), logs `Rescued below the watch spot Z= Line= Side= Rescues=` (Information, `watch` log). Returns how many were rescued |
| `Grace(steamId)` | No checks for `GraceSeconds` (2 s). Set by every `WatchSpot.SendUp` and by the Lobby `player_spawn` hook, so a respawn at base or a teleport still landing is not a rescue |
| `Forget(steamId)` | Drops the player's grace and command history (disconnect) |
| `LogCommand(player, command, args)` | `Restrained player console command Command= Args=` (Information, `watch` log), at most once per `CommandRepeatSeconds` (2 s) per player and command |

## Invariants

- Fighters are released when they move into the rift, so they are never
  checked.
- Sends players to the current watch spot, never the fixed `draft` spot.
- `Check` is cheap: a restrained-count check first, then one pass over the
  participants every 16 frames.
