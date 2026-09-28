---
id: rift-roulette-architecture
type: mental-model
tags: [architecture, rift-roulette, layering, match-loop]
related:
  - how-deadworks-mods-work.md
  - ../glossary.md
  - ../game-mode-recipes.md
  - ../generated/tree.md
---

# Rift Roulette architecture

The authority is Theo's `RiftRoulette/FEATURE.md` (composition diagram) and
`.rules` §6. This note is the short version for deciding where new code goes.

## Three DLLs, many plugin classes

```text
RiftRoulette.dll   the game mode: ~20 plugin classes + Shared + 7 modules compiled in
CleanSlate.dll     strips lanes, NPCs, bosses and shops from dl_midtown (+ Shared)
DevTools.dll       entity inspection, log path, diagnostics (+ Shared)
```

`Modules/` (WorldText, Movement, Hud, Loadout, Restraint, Queue, Spectate)
know nothing about Rift Roulette. Anything game-specific (team names, rift
sides, anchors) lives under `RiftRoulette/`.

## Three layers

```text
Game lifecycle (hooks, MatchService)  ─┐
Admin command  (Debug mode)            ├─>  composed ops (RoundFlow)  ─>  services (core operations)
Player command (Clean mode)           ─┘
```

- **Services** hold the logic and state: `RiftService`, `MatchService`,
  `RandomModeService`, `LoadoutService`, `RestraintService`, ...
- **Plugin classes** are thin: hooks and `[Command]` wrappers that call
  services.
- **Pure rules** (`*Rule.cs`: `BenchRule`, `AutoStartRule`, `SoulRule`,
  `AdminSeatRule`, ...) contain no game calls, so they are unit tested
  (381 tests).

## One round, end to end

```text
2 humans connected ─> AutoStartService ─> MatchService.Start
  intermission (5 s): Balance swaps ─> Bench picks sit-out ─> HeroDraw ─> LoadoutService gives hero + build
  RoundFlow.RunRound ─> RiftService forces the rift (gamerules schema fields)
    ─> MoveTeamsToRift: per-slot spots, restraint released, healed to full
    ─> capture detected (troopers spawn for the winner) or tie
  ─> ReturnPlayersToDraft: WatchSpot.SendUp (teleport + restrain), boards follow
  ─> MatchService.OnRoundEnded: score banner, bets settle, next intermission
player_death ─> StatsService ─> boards, Balance counters, chips
```

## Where new code goes

| You are adding | Put it in |
|---|---|
| A reusable mechanic any mode could use (a timer HUD, a zone, a scoreboard) | `Modules/<Name>/` with `<Name>.projitems`, a service, optional `<Name>Commands.projitems` |
| Rift Roulette gameplay | a feature folder under `RiftRoulette/` with `FEATURE.md`, a `*Plugin.cs` and a `*Service.cs` |
| A decision you can test without the game | a `*Rule.cs` plus tests in `Tests/RiftRoulette.Tests` |
| A whole new game mode | a new DLL beside RiftRoulette (own `.csproj`, add to `Bublock.sln` and `deploy.sh` `PLUGINS`) that imports the modules it needs; check command names don't collide |

Every `Foo.cs` needs a `Foo.md`; every feature folder needs a `FEATURE.md`;
commands go in `reference/user-commands.md` or `admin-commands.md`
(`.rules` §1-3, §9).
