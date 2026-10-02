# Mirror — Feature

## Purpose

Mirror hero mode (`/match_mode mirror`). Every intermission one hero and one
of its stored builds are chosen once and given to every fighter on both
teams, so everyone plays the same hero with the same build. An admin can
pin the hero (`/mirror_hero`) and then which of that hero's builds
(`/mirror_build`); a pinned hero or build never goes random again until an
admin changes or clears it. With a hero pinned and no build pinned, the
build is still rolled once per intermission and shared by everyone. The
build's optional item picks are drawn once with it too, so every fighter
ends up with exactly the same items.

Teams, score, the odd-player bench (`RandomMode/BenchRule`) and
auto-balance work like Random mode. Betting, `/reserve` and `/heroban` are
Random mode only. A menu hero change kills the player, who respawns with
the shared hero and build (that death is not counted).

## Files

| File | Role |
|---|---|
| `MirrorPick.cs` | `MirrorPins`, `MirrorChoice`, the one shared pick and `Share` (pure, tested) |
| `MirrorModeService.cs` | Match / round orchestration, pins, joiners, pending swaps, hero guard, banners, status |
| `MirrorPlugin.cs` | Spawn hooks, `/mirror_hero`, `/mirror_build`, `/mirror_status` |

## Public operations

See `MirrorModeService.md`. Admin commands in
`reference/admin-commands.md`.

## State

`MirrorModeService` statics: the pins (kept across matches, mode changes
and map reloads; reset on DLL load), teams, fighters, the current shared
pair, the last shared hero, build values, bench rotation and its own
`Lobby/HeroLock`. Match state is cleared by `BeginMatch` / `EndMatch`.

## Lifecycle vs commands

- Lifecycle (Clean): `MatchService.Start` calls `BeginMatch`; each
  intermission `PrepareRound`, then 3 s in `AnnounceBuilds`; `End` calls
  `EndMatch`. `StartRound` adds `FighterCount` to the join budget.
  `LobbyService` / `BanStatueService` call `AddJoiner` / `OnLeave`,
  `AdminSeat.Sit` calls `Forget`, `DraftService.EnforceHero` calls
  `GuardHero`, `StatsService.RecordDeath` calls `ConsumeEnforcementKill`.
- Admin commands (Debug): pin changes during a mirror intermission reapply
  the shared pair right away; otherwise they wait for the next one.

## Dependencies

- `RandomMode/BenchRule`, `RandomModeService.Options` / `BuildDescription`
  / sit-out banner text.
- `Lobby/TeamBalance`, `Lobby/HeroLock`, `Lobby/Participants`.
- `Balance/BalanceService.TryBalance`.
- `Modules/Loadout` (`HeroBuildCatalog`, `LoadoutService.Swap`),
  `Modules/Hud`, `Modules/Queue`.
- `Draft/DraftState` (fighters hold a pick so `Round/RoundFlow` moves them).
