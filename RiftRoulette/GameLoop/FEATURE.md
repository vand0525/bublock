# GameLoop — Feature

## Purpose

Runs a continuous playtest match. An admin starts it once with
`/match_start`; rounds then start themselves after a short intermission,
each round's result updates a running score, and every player sees the
score in the game's on-screen banner (Hud module). `/match_end` stops the
loop and returns everyone to the lobby hero up top.

## Auto-start

`AutoStartService` starts the match as soon as 2 human players are
connected and ends it when fewer than 2 remain, so no admin has to be on.
It checks 2 s after every join and on every disconnect (from
`Lobby/LobbyService`) and once 3 s after the DLL loads. A disconnect can
only end a match, never start one (starting one during a disconnect
crashed the server); the join check is delayed
so the joiner's own `SelectHero` does not swallow the match's hero swaps. `/match_auto <on|off>` switches it (on after
every load); with it off, admins use `/match_start` / `/match_end`.

## Join budget refresh

Every round adds its fighter count to
`Lobby/MapRefreshService`. At a scored round end, once 160 fighter-rounds
have been played since the map started (the join package nears the
512 KB limit; see `Lobby/MapRefreshRule.md`), `OnRoundEnded` does not
schedule the next round: everyone gets a chat warning that they will
reconnect, the match ends, and the map reloads 10 s later. Auto-start is
held until the reload, then starts a fresh match (score and souls reset)
once 2 humans are back.

## Configuration

`MatchConfig`: hero mode `random` (default) or `mirror`, and format
`continuous` (the only one for now). Set with `/match_mode` and
`/match_format` between matches; `/match_config` shows it. In Random mode,
`RandomMode/RandomModeService` balances teams at match start and gives
everyone a new hero and build at the start of each intermission; 3 s in, a
banner shows each player their hero, build and its soul value. In Mirror mode,
`Mirror/MirrorModeService` gives every fighter the same hero and the same
build each intermission: one pick, or the admin's pins (`/mirror_hero`,
`/mirror_build`); teams, bench and balance work like Random mode, with
the same build banner 3 s in. Both modes write each round's fighters into
`Round/RoundHeroes`; `RoundFlow` moves only those players into the rift.

## Rules (playtest)

- Continuous: no match target; `/match_end` or dropping below 2 players
  (auto-start on) stops it.
- A captured rift (`finished`) gives 1 point to the team that owns the
  first new rift trooper. Ties, cancels, and spawn timeouts give no point.
- A player who dies during a round respawns up top and is out
  until the next round (existing Lobby behavior). Wiping a team does not
  end the round.
- Random mode: heroes and builds change every intermission; teams stay
  unless auto-balance (`Balance/`) swaps players between rounds.
- Mirror mode: every fighter has the same hero and build, chosen again each
  intermission unless pinned; a menu hero swap kills and restores.
- Buying: CleanSlate disables every shop, and `ShopAccess` keeps buying
  anywhere off. Builds are given with `AddItem`, not bought.
- Banners, only what players need: match start, round result, the
  build banner (hero, build, souls), and `Round N` / score 3 s before
  each round (no banner when the round starts); `/match_mode` shows a mode
  banner. No debug-style banners or chat lines.
- Souls: while a match runs every earned soul gain (kills, assists, orbs,
  passive and team income) is blocked (`SoulRule`, applied by
  `GameLoopPlugin.OnModifyCurrency`). Power comes only from the round's
  build; bounties grew with the game clock and made one team snowball.
- Kills, deaths and assists count from match start (`Stats/`) and show on
  the Sapphire and Amber boards.

## Files

| File | Role |
|---|---|
| `MatchConfig.cs` | Hero mode and format settings, parsing (pure parts unit tested) |
| `MatchState.cs` | Phase, round, score, ties, result text (pure, unit tested) |
| `MatchService.cs` | Loop: start, countdown, start round, score round, end; mode and format setters |
| `AutoStartRule.cs` | Start / end / nothing decision from player count (pure, unit tested) |
| `AutoStartService.cs` | Counts participants (no bots, no seated admin), starts or ends the match, waiting chat line, on/off flag |
| `ShopAccess.cs` | Owns `citadel_allow_purchasing_anywhere`; `Disable` from startup and `/lobby_setup` |
| `SoulRule.cs` | Which soul gains are blocked while a match runs (pure, unit tested) |
| `MatchProbe.cs` | Snapshot lines to `probe-*.log` at match start and 1 s after each move in / up |
| `GameLoopPlugin.cs` | `OnLoad` auto-start check; `OnGameFrame` / `OnClientConCommand` for `Round/WatchGuard`; `OnModifyCurrency` soul block; `/match_start`, `/match_end`, `/match_auto`, `/match_status`, `/match_intermission`, `/match_mode`, `/match_format`, `/match_config`, `/score` |

## Public operations

See `MatchService.md` and `MatchState.md`. Catalogued in
`reference/admin-commands.md` and `reference/user-commands.md`.

## State

`MatchService.State` (one per DLL load), the intermission length (default
5 s; betting stays open 10 s into each round), the kept `ITimer`, two countdown handles, and `MatchConfig` (resets
to random / continuous on every DLL load), and `AutoStartService.Enabled`
(on after every DLL load).

## Dependencies

- `Round/RoundFlow` (`RunRound`, `CancelRound`); `RoundFlow.Steps` wires
  the rift's `RoundEnded` step to `MatchService.OnRoundEnded`.
- `Rift/RiftService` (phase, next side), `Rift/RiftRoundResult`.
- `Lobby/LobbyHeroes.ReturnAll` and `Boards/BoardService.Redraw` for
  `/match_end` and mode changes.
- `RandomMode/RandomModeService` in Random mode; `Mirror/MirrorModeService`
  in Mirror mode.
- `Stats/StatsService` (reset at start, round counts) and
  `Balance/BalanceService` (reset at start, round results).
- `Modules/Hud` for banners.
- `Lobby/MapRefreshService` (join budget count, round-end refresh, holds
  auto-start while a reload is pending).

## Lifecycle vs commands

- The loop is the game lifecycle: it calls `RoundFlow.RunRound` itself.
- Auto-start runs the match in Clean mode; `/match_start` runs it in Debug.
- `/rift_start` and `/rift_cancel` still work during a match; a round they
  end is scored like any other (cancel = no point) and the loop continues.
- Outside a match the round-ended step does nothing, so `/rift_start` alone
  runs one unscored round.

## Logs

`match-YYYYMMDD.log` (including `Buying anywhere off`, Debug); master gets
match start, round results, match end, and `Match auto-started` /
`Match auto-ended` lines. `probe-YYYYMMDD.log`: `MatchProbe` snapshots.
