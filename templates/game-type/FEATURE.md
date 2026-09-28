# __NAME__ — Feature (game type)

## Purpose

__TITLE__, a game type of its own (`__NAME__.dll`), built on the engine
(`Shared/` + `Modules/*`) from `scripts/new-game-type.sh`. It starts as
timed team deathmatch in the mid lane brawl arena: 2-minute matches, a
point per enemy kill, most points wins, players pick their own heroes.
Describe what makes __TITLE__ different here, then change
`__NAME__Rules.cs` and `__NAME__Service.cs` to match.

## Files

| File | Role |
|---|---|
| `__NAME__Rules.cs` | __TITLE__'s rules: what scores, session settings, banner text (pure, tested) |
| `__NAME__Service.cs` | Wires the engine modules to the rules |
| `LobbyPlugin.cs` | Convars, joins, leaves, team-change block, currency hook |
| `ArenaPlugin.cs` | Spawns into the arena, containment |
| `MatchPlugin.cs` | Kills, `/points`, `/__PREFIX___status`, `/__PREFIX___start`, `/__PREFIX___end`, `/__PREFIX___time` |
| `Data/arena.json` | The arena asset (starts as a copy of the mid lane brawl; check changes with `scripts/check-arena.py`) |

## Engine modules used

`Session`, `Arena`, `Movement`, `Teams`, `Economy`, `Hud`, `Loadout`,
`Shared`. Add `RandomLoadout`, `WorldText`, `Restraint`, `Queue` or
`Spectate` in `__NAME__.csproj` as the rules need them.

## Commands

| Command | Who | Calls | Mode | Side effects |
|---|---|---|---|---|
| `/points` | players | `__NAME__Service.DescribeFor` | Clean, read-only | chat: match, time left, your points and place, top 3 |
| `/__PREFIX___status` | admin | `__NAME__Service.Describe` | Debug, read-only | session and arena lines |
| `/__PREFIX___start` | admin | `TimedSession.Start` | Debug | starts a match now |
| `/__PREFIX___end` | admin | `TimedSession.End` | Debug | ends the match now, result banner |
| `/__PREFIX___time <30-1800>` | admin | `TimedSession.TrySetMatchSeconds` | Debug | match length from the next match |

## Deploy

`DW_PLUGINS="__NAME__ DevTools CleanSlate"` and `DW_PARKED` = the game
type it replaces in `scripts/server.env`, `scripts/deploy.sh --confirm`,
then restart the server (a removed DLL stays loaded until then).

## Logs

`__NAME__` log folder: `master`, `__NAMELOWER__`, `session`, `arena`.
