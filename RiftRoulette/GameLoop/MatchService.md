# MatchService

The continuous playtest match loop. Static; state lives in
`MatchState` plus the countdown timer handles. The `ITimer` comes from the
caller of `Start` (`GameLoopPlugin`) and is kept for the whole match.

## Flow

1. `Start`: score reset; in Random mode `RandomModeService.BeginMatch`
   (teams), in Mirror mode `MirrorModeService.BeginMatch` (teams; pins
   kept); banner `Match starting` / `Round 1 in Ns`; countdown scheduled;
   `MatchProbe.Snapshot("match-start")`.
2. Countdown (`ScheduleNextRound`): `Lobby/FlexSlots.UnlockAll` (every
   flex slot open, so builds get 12 slots), then in Random mode
   `RandomModeService.PrepareRound` (new hero and build for everyone); in
   Mirror mode `MirrorModeService.PrepareRound` (one hero and one build,
   pinned or picked once, for every fighter). Then `BettingService.Open`
   (Random mode: betting opens and everyone gets a chat line with their
   souls). The default intermission is `DefaultIntermissionSeconds` (5 s).
   - `BuildBannerDelaySeconds` (3 s) in: each player gets
     `<Hero>` / `<build> - 12,345 souls` (`RandomModeService.AnnounceBuilds`,
     Mirror: `MirrorModeService.AnnounceBuilds`, through `AnnounceHeroBuilds`;
     a loadout that lands later shows its banner when applied). When the
     intermission leaves no room before the countdown banner, builds show
     as each loadout lands instead.
   - At N-3 s (`FinalCountdownSeconds`), to everyone: `Round X` / the score.
   - At N s `StartRound` runs `RoundFlow.RunRound(timer, mode)` (the
     lifecycle path, Clean when called by the loop). No banner. Once the
     round started, `BettingService.Close` is scheduled
     `BettingService.LingerSeconds` (10 s) later, so bets can still come in
     at the start of the round; the close is skipped if that round already
     ended (the round end closes and settles), and `CancelCountdown`
     cancels it. In Random mode it then calls
     `RandomModeService.AnnounceBans` (chat `Banned this round: ...` to
     everyone when a hero is banned; never a banner) and counts the round
     for the join budget (`MapRefreshService.AddRound(RandomModeService.FighterCount)`).
     In Mirror mode it only counts the round
     (`MapRefreshService.AddRound(MirrorModeService.FighterCount)`).
3. Round end: `RiftService` calls `RoundFlow`'s `RoundEnded` step, which
   calls `OnRoundEnded`. Score applied, banner `Sapphire 1 - 0 Amber` /
   `Sapphire took the rift`, next countdown scheduled. When the
   join budget is reached (`MapRefreshService.TryBegin`) the match ends
   instead and the map reloads 10 s later.
4. Repeat until `End`.

Banners are only what players need (result, their hero and build, the
round countdown); no debug-style text.

## Operations

| Op | Behavior | Returns |
|---|---|---|
| `Start(timer, mode)` | Refuses if a match is running or a rift is running. Else keeps the timer, `State.Start()`, `StatsService.Reset`, `BalanceService.Reset` and `BettingService.Reset`, `BeginMatch` in Random or Mirror mode, logs with the config (feature + master), banner, schedules round 1, match-start probe | reply line |
| `End(timer, mode)` | Refuses if idle. Cancels the countdown, `State.Reset()` (so the round-ended step is ignored), cancels a running rift through `RoundFlow.CancelRound`, in Random mode `BettingService.EndMatch` (open bets refunded) then `RandomModeService.EndMatch` (builds cleared), in Mirror mode `MirrorModeService.EndMatch` (builds cleared, pins kept), `Lobby/LobbyHeroes.ReturnAll` (round heroes cleared, everyone alive back up top as the lobby hero, teams kept), `BoardService.Redraw`, banner `Match over` / final score, logs | reply line |
| `SetHeroMode(heroMode, timer, mode)` | Refuses during a match or when unchanged. Sets `MatchConfig.HeroMode`, then `LobbyHeroes.ReturnAll` and `BoardService.Redraw`, then the `ModeBanner` to everyone. Logs (feature + master) | reply line |
| `ModeBanner(heroMode)` | Random: `Random mode` / `Random hero and build every round`. Mirror: `Mirror mode` / `Everyone has the same hero and build` | (title, description) |
| `SetFormat(format, mode)` | Refuses during a match. Sets `MatchConfig.Format`, logs | reply line |
| `DescribeConfig()` | Config line, then the allowed modes / formats and intermission | 2 lines |
| `OnRoundEnded(result, mode)` | Ignored when idle. `State.Apply`; `BalanceService.RecordRound(pointTo)`; `StatsService.SetRounds` (board round counts); `BettingService.OnRoundEnded(pointTo)` (bets paid, lost, or refunded on no point); Warning if `finished` had no known team; logs (feature + master); score banner; then `MapRefreshService.TryBegin(timer)`: when the join budget is reached it warns in chat, calls `End` and reloads the map 10 s later, and no next round is scheduled; otherwise schedules the next round | — |
| `SetIntermission(seconds, mode)` | 5 to 120 s (default 5); applies from the next countdown | `bool` |
| `DescribeMatch()` | Phase, round, score and ties, auto-start on/off (`AutoStartService.Enabled`); config, intermission, rift phase, next side | 2 lines |
| `DescribeScore()` | `Round X: Sapphire a - b Amber (ties t)`; idle: `No match is running.` | lines |

`StartRound` (private): does nothing unless the phase is `Intermission`.
If a rift is already running (for example a manual `/rift_start` during the
countdown), logs a Warning and waits; that rift's end schedules the next
countdown. If `RunRound` did not start a rift (gamerules missing), logs a
Warning and schedules another countdown without re-preparing heroes
(`prepareHeroes: false`).

## Logs

`Match` feature log (`match-YYYYMMDD.log`). Master: match started, each
round result with score, match ended, plus Warning+.

## Invariants

- Every round-ending path in `RiftService` (finished, tied, cancelled,
  spawn timed out) reaches `OnRoundEnded`, so the loop never stalls.
- No per-tick work: at most three timer callbacks per intermission
  (countdown, final countdown, build banner; all cancelled by `End`) plus
  one loadout callback per player.
- Deaths are not handled here: a dead player respawns at the watch spot
  (restrained) through Lobby's `player_spawn` hook and is out until the
  next round.
  Kills, deaths and assists are counted by `Stats/StatsService`; the final
  stats stay on the boards after `End` until the next `Start`.
- Timers belong to `GameLoopPlugin`; the loop stops if that plugin unloads
  (the state stays and `/match_end` still works).
