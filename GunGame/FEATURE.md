# GunGame — Feature (game type)

## Purpose

Gun Game, a game type of its own (`GunGame.dll`, redock fork). Built on the
engine (`Shared/` + `Modules/*`), never on another game type's code. Runs on
a server instead of Rift Roulette (`DW_PLUGINS="GunGame DevTools
CleanSlate"`, `DW_PARKED="RiftRoulette"`).

- **Arena:** the middle lane of `dl_midtown` (lane 4). Amber spawns on the
  south street, Sapphire on the north street, each player on their own
  slot's spot; the fight runs through the center. Anyone who leaves the
  bounds box goes back to their spot. No rift is used. Asset:
  `Data/arena.json`, checked with `scripts/check-arena.py`.
- **Session:** one continuous session, no rounds. From 2 players, a
  2-minute match runs; the player with the most kills wins; the result
  shows for 5 s; points reset; the next match starts.
- **Heroes:** a random hero on join; every kill swaps the killer, right
  away, to a new random hero with one of its stored top builds (same
  budgeted build style as Rift Roulette's Random mode). Menu hero picks and
  team changes are refused. Earned souls are blocked: power comes only from
  the build.

## Files

| File | Role |
|---|---|
| `GunGameRules.cs` | Gun Game's own rules: kill credit, session settings, banner text (pure, tested) |
| `GunGameService.cs` | Wires the engine modules to the rules: admit, remove, spawn, kill, containment, status |
| `LobbyPlugin.cs` | Load / startup convars, joins and leaves, hero / team command block, soul block, waiting reminder |
| `ArenaPlugin.cs` | Spawns into the arena, containment every second, `/gg_arena` |
| `MatchPlugin.cs` | Kills, `/points`, `/gg_status`, `/gg_start`, `/gg_end`, `/gg_time`, `/gg_reroll` |
| `Data/arena.json` | The mid lane brawl arena (embedded as `GunGame.arena.json`) |
| `GunGame.csproj` | Imports `Shared` and the modules below; services only, no module commands |

## Engine modules used

| Module | For |
|---|---|
| `Modules/Session` | `TimedSession` (2-minute matches, break, restart), `Scoreboard` |
| `Modules/RandomLoadout` | random hero + stored build, pending until spawn |
| `Modules/Loadout` | the build itself (`LoadoutService.Swap`, `hero-builds.json`) |
| `Modules/Arena` | spots, bounds, sending players to the arena |
| `Modules/Movement` | teleports and camera angle |
| `Modules/Teams` | team numbers, smaller-team placement, `ChoiceGuard` |
| `Modules/Economy` | `SoulRule` |
| `Modules/Hud` | banners |
| `Shared` | logging, admin auth, chat, convars |

## Composition

```text
join (OnClientFullConnect) ─> Admit: smaller team, RandomLoadouts.Draw + SelectHero, Hold ─> spawn ─> build lands
spawn / respawn ─> ArenaService.SendToArenaNextTick + RandomLoadouts.ApplyPending (next tick)
every 1 s ─> ArenaService.ReturnStrays (bounds)
player_death ─> GunGameRules.Credits ─> Scoreboard.Add ─> RandomLoadouts.Roll(killer) ─> banner "3 kills: <hero>"
players >= 2 ─> TimedSession: Started banner ─ 1:50 ─> "10 seconds left" ─ 2:00 ─> Ended banner (winner) ─ 5 s ─> next match
players < 2 ─> Stopped: chat "Gun Game starts at 2 players"
selecthero / changeteam / jointeam ─> refused (ChoiceGuard); earned souls ─> refused (SoulRule)
```

## Commands

| Command | Who | Calls | Mode | Side effects |
|---|---|---|---|---|
| `/points` (`dw_points`) | players | `GunGameService.DescribeFor` | Clean, read-only | chat: match number and time left, your kills and place, top 3; or that no match runs and why |
| `/gg_status` | admin | `GunGameService.Describe` | Debug, read-only | session line, scorers, arena and pending count, every player's hero and build |
| `/gg_start` | admin | `TimedSession.Start` | Debug | starts a match now even with one player (scores reset) |
| `/gg_end` | admin | `TimedSession.End` | Debug | ends the match now: result banner, break, next match if 2+ players |
| `/gg_time <30-1800>` | admin | `TimedSession.TrySetMatchSeconds` | Debug | match length from the next match (resets to 120 on load) |
| `/gg_reroll <slot>` | admin | `RandomLoadouts.Roll` | Debug | new random hero and build for that player now (pending if dead) |
| `/gg_arena <slot>` | admin | `ArenaService.SendToArena` | Debug | teleports that player to their arena spot |

Admin commands check `AdminAuth` (the server console is trusted). All
names are unique across Bublock DLLs (`gg_*`, `/points`).

## State

`GunGameService`: the `TimedSession` (phase, match, scoreboard, timers), the
`RandomLoadouts` (picks, pending), the arena (lazy from the asset), and
player names. All reset on every load; a load admits everyone already
connected (`AdmitConnected`).

## Lifecycle

- Load: convars now and 3 s later (after CleanSlate's), then admit anyone
  connected and check the session. Timers only run while the server is
  awake (not hibernating with nobody on).
- Hot reload (upload): the same, from scratch.

## Deadworks constraints

- Removing a plugin DLL from `plugins/` does not unload it; restart the
  server after parking a game type (seen 2026-09-28).
- Never `SelectHero` while dead (`RandomLoadouts` holds the pick until the
  spawn).

## Logs

`GunGame` folder: `master`, `gungame` (admits, kills, final standings),
`session` (match start / end), `randomloadout` (swaps), `loadout` (builds),
`arena` (strays sent back).

## Next

Classic gun-game ladders (a fixed hero order), weaker builds as kills climb, a
scoreboard board in the world (`Modules/WorldText`), and more arenas as
assets.
