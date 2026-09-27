# GameLoop — Feature

## Purpose

Runs a continuous playtest match (Stage 13a). An admin starts it once with
`/match_start`; rounds then start themselves after a short intermission,
each round's result updates a running score, and every player sees the
score in the game's on-screen banner (Hud module). `/match_end` stops the
loop and returns everyone to the lobby. In 1v1 mode there is no team
score: the only score is the best-streak leaderboard (`Duel/FEATURE.md`).

## Auto-start (Stage 13d)

`AutoStartService` starts the match as soon as 2 human players are
connected and ends it when fewer than 2 remain, so no admin has to be on.
It checks 2 s after every join and on every disconnect (from
`Lobby/LobbyService`) and once 3 s after the DLL loads. A disconnect can
only end a match, never start one (starting one during a disconnect
crashed the server in the 2026-09-27 playtest); the join check is delayed
so the joiner's own `SelectHero` does not swallow the match's hero swaps. `/match_auto <on|off>` switches it (on after
every load); with it off, `/match_start` / `/match_end` work as before.
In 1v1 mode it counts the players in the 1v1 queue instead of everyone
connected (Stage 13j), and also checks after every queue join / leave.

## Configuration (Stage 13b)

`MatchConfig`: hero mode `random` (default), `draft`, or `duel` (`1v1`,
Stage 13g), and format
`continuous` (the only one for now). Set with `/match_mode` and
`/match_format` between matches; `/match_config` shows it. In Random mode,
`RandomMode/RandomModeService` balances teams at match start and gives
everyone a new hero and build at the start of each intermission; 3 s in, a
banner shows each player their hero, build and its soul value. In 1v1 mode,
`Duel/DuelService` locks the two fighters to one copied build and
re-applies it every intermission; the match only starts once a build is
copied. Since Stage 13j the fighters are the first two in the 1v1 queue:
`OnRoundEnded` sends the loser to the back (`DuelService.RecordResult`)
and the banners name the winner, streak and next pairing.

## Rules (playtest)

- Continuous: no match target; `/match_end` or dropping below 2 players
  (auto-start on) stops it.
- A captured rift (`finished`) gives 1 point to the team that owns the
  first new rift trooper. Ties, cancels, and spawn timeouts give no point.
- A player who dies during a round respawns in the draft area and is out
  until the next round (existing Lobby behavior). Wiping a team does not
  end the round.
- Draft mode: picks carry over between rounds; players may `/pick` or
  `/unpick` during the intermission.
- Random mode: heroes and builds change every intermission; teams stay
  unless auto-balance (Stage 13c, `Balance/`) swaps players between rounds.
- 1v1 mode: both players have the same copied hero and build, reset every
  intermission; a menu hero swap kills and restores.
- Buying: CleanSlate disables every shop, and `ShopAccess` turns buying
  anywhere on only during 1v1 setup (1v1 mode, no match). Random builds and
  the 1v1 copy are given with `AddItem`, not bought.
- Banners, only what players need: match start, round result, the Random
  mode build banner (hero, build, souls), and `Round N` / score 3 s before
  each round (no banner when the round starts); `/match_mode` shows a mode
  banner. No debug-style banners or chat lines.
- Souls: while a match runs every earned soul gain (kills, assists, orbs,
  passive and team income) is blocked (`SoulRule`, applied by
  `GameLoopPlugin.OnModifyCurrency`). Power comes only from the round's
  build; bounties grew with the game clock and made one team snowball.
- Kills, deaths and assists count from match start (`Stats/`); Random and
  1v1 mode show them on the Sapphire and Amber boards.

## Files

| File | Role |
|---|---|
| `MatchConfig.cs` | Hero mode and format settings, parsing (pure parts unit tested) |
| `MatchState.cs` | Phase, round, score, ties, result text (pure, unit tested) |
| `MatchService.cs` | Loop: start, countdown, start round, score round, end; mode and format setters |
| `AutoStartRule.cs` | Start / end / nothing decision from player count (pure, unit tested) |
| `AutoStartService.cs` | Counts participants (no bots, no seated admin), starts or ends the match (1v1: only with a copied build), waiting banner, on/off flag |
| `ShopRule.cs` | Buying anywhere only in 1v1 setup (pure, unit tested) |
| `ShopAccess.cs` | Owns `citadel_allow_purchasing_anywhere`; `Sync` from startup, match start / end, mode change |
| `SoulRule.cs` | Which soul gains are blocked while a match runs (pure, unit tested) |
| `MatchProbe.cs` | Snapshot lines to `probe-*.log` at match start and 1 s after each move in / up |
| `GameLoopPlugin.cs` | `OnLoad` auto-start check; `OnGameFrame` / `OnClientConCommand` for `Round/WatchGuard`; `OnModifyCurrency` soul block; `/match_start`, `/match_end`, `/match_auto`, `/match_status`, `/match_intermission`, `/match_mode`, `/match_format`, `/match_config`, `/score` |

## Public operations

See `MatchService.md` and `MatchState.md`. Catalogued in
`reference/admin-commands.md` and `reference/user-commands.md`.

## State

`MatchService.State` (one per DLL load), the intermission length (default
15 s), the kept `ITimer`, two countdown handles, and `MatchConfig` (resets
to random / continuous on every DLL load), and `AutoStartService.Enabled`
(on after every DLL load).

## Dependencies

- `Round/RoundFlow` (`RunRound`, `CancelRound`); `RoundFlow.Steps` wires
  the rift's `RoundEnded` step to `MatchService.OnRoundEnded`.
- `Rift/RiftService` (phase, next side), `Rift/RiftRoundResult`.
- `Draft/DraftService.Reset` for `/match_end` and mode changes.
- `RandomMode/RandomModeService` in Random mode; `Duel/DuelService` in 1v1
  mode.
- `Stats/StatsService` (reset at start, round counts) and
  `Balance/BalanceService` (reset at start, round results).
- `Modules/Hud` for banners.

## Lifecycle vs commands

- The loop is the game lifecycle: it calls `RoundFlow.RunRound` itself.
- Auto-start runs the match in Clean mode; `/match_start` runs it in Debug.
- `/rift_start` and `/rift_cancel` still work during a match; a round they
  end is scored like any other (cancel = no point) and the loop continues.
- Outside a match the round-ended step does nothing, so `/rift_start` alone
  behaves as before.

## Logs

`match-YYYYMMDD.log` (including `Buying anywhere State=`); master gets
match start, round results, match end, and `Match auto-started` /
`Match auto-ended` lines. `probe-YYYYMMDD.log`: `MatchProbe` snapshots.
