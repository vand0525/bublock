# LobbyHeroes

Keeps participants on the right hero outside the round build: the lobby
hero (`LobbyService.LobbyHero`) when they have no round hero, and the
mode's hero guard when they do. Static; no state of its own.

## Operations

| Op | Behavior | Returns |
|---|---|---|
| `ReturnAll(timer, mode)` | `RoundHeroes.Clear()`; for each participant (`Participants.Humans()`) with a pawn: dead → Debug log and skip; alive → zero gold / AP / level, `SelectHero(LobbyHero)`, next tick `WatchSpot.SendUp` (restrain + watch spot). Teams are kept (they stay even between matches). Logs `Players returned to the lobby hero Returned=` | players returned |
| `Enforce(player, pawn, timer)` | From `LobbyPlugin`'s `player_hero_changed` hook. Non-participants (seated admin, statue): nothing. Then `RandomModeService.GuardHero` (Random mode players with an assignment: kill and rebuild on respawn), then `MirrorModeService.GuardHero` (Mirror mode fighters, same rule against the shared hero); if either handled the player, stop. Otherwise expected hero = round hero (`RoundHeroes`) or `LobbyHero`. Wrong hero and dead: Debug log, no `SelectHero`. Wrong hero and alive: `SelectHero(expected)`. Right hero and no round hero: zero gold / AP / level | — |

## Called from

- `GameLoop/MatchService.End` and `SetHeroMode` (`ReturnAll`, then
  `BoardService.Redraw`).
- `LobbyPlugin.OnPlayerHeroChanged` (`Enforce`).

## Logs

`Lobby` feature log (`lobby-YYYYMMDD.log`).

## Dangerous constraints

- Never calls `SelectHero` while the player is dead.
- Timer callbacks capture the controller; `TeleportTo` skips players
  without a pawn.
- Game-thread only.
