# MatchService

The continuous playtest match loop. Static; state lives in
`MatchState` plus the countdown timer handles. The `ITimer` comes from the
caller of `Start` (`GameLoopPlugin`) and is kept for the whole match.

## Flow

1. `Start`: score reset; in Random mode `RandomModeService.BeginMatch`
   (teams), in 1v1 mode `DuelService.BeginMatch` (opposite teams, hero lock
   on); buying synced (`ShopAccess.Sync`: off during any match); banner
   `Match starting` / `Round 1 in Ns` (1v1: `1v1` / `<pairing>`); countdown
   scheduled; `MatchProbe.Snapshot("match-start")`.
2. Countdown (`ScheduleNextRound`): `Lobby/FlexSlots.UnlockAll` (every
   flex slot open, so builds get 12 slots), then in Random mode
   `RandomModeService.PrepareRound` (new hero and build for everyone); in
   1v1 mode `DuelService.PrepareRound` (both players reset to the copied
   build with 0 souls). Then `BettingService.Open` (Random mode: betting
   opens and everyone gets a chat line with their souls). The default
   intermission is `DefaultIntermissionSeconds` (5 s).
   - Random mode, `BuildBannerDelaySeconds` (3 s) in: each player gets
     `<Hero>` / `<build> - 12,345 souls` (`RandomModeService.AnnounceBuilds`;
     a loadout that lands later shows its banner when applied). When the
     intermission leaves no room before the countdown banner, builds show
     as each loadout lands instead.
   - At N-3 s (`FinalCountdownSeconds`), to everyone: `Round X` / the score
     (1v1: `Round X` / `<King> vs <Challenger>`).
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
3. Round end: `RiftService` calls `RoundFlow`'s `RoundEnded` step, which
   calls `OnRoundEnded`. Score applied, banner `Sapphire 1 - 0 Amber` /
   `Sapphire took the rift` (1v1 mode: no team score; the streak
   leaderboard updates and the banner is `<Winner> wins (streak N)` /
   `Next: <King> vs <Challenger>`), next countdown scheduled. When the
   join budget is reached (`MapRefreshService.TryBegin`, scored path
   only) the match ends instead and the map reloads 10 s later.
4. Repeat until `End`.

Banners are only what players need (result, their hero and build, the
round countdown); no debug-style text.

## Operations

| Op | Behavior | Returns |
|---|---|---|
| `Start(timer, mode)` | Refuses if a match is running, a rift is running, or 1v1 mode has no copied build (`1v1 needs a build first: /duel_copy <slot>.`). Else keeps the timer, `State.Start()`, `ShopAccess.Sync`, `StatsService.Reset`, `BalanceService.Reset` and `BettingService.Reset`, `BeginMatch` in Random or 1v1 mode, logs with the config (feature + master), banner (1v1 mode names the first pairing), schedules round 1, match-start probe | reply line |
| `End(timer, mode)` | Refuses if idle. Cancels the countdown, `State.Reset()` (so the round-ended step is ignored), `ShopAccess.Sync` (opens buying again in 1v1 mode), cancels a running rift through `RoundFlow.CancelRound`, in Random mode `BettingService.EndMatch` (open bets refunded) then `RandomModeService.EndMatch` (builds cleared), `DraftService.Reset` (everyone alive to the lobby as LobbyHero (Abrams), picks cleared), then in 1v1 mode `DuelService.EndMatch` (lock off, build kept, setup souls, no separate setup banner), banner `Match over` / final score (1v1: `DuelService.StreakSummary()`, taken before `EndMatch` clears it), logs | reply line |
| `SetHeroMode(heroMode, timer, mode)` | Refuses during a match or when unchanged. Leaving 1v1: `DuelService.Leave` (build dropped). Sets `MatchConfig.HeroMode`, `ShopAccess.Sync`, then `DraftService.Reset` (lobby reset; boards redrawn for the new mode); entering 1v1: `DuelService.EnterSetup(announce: false)` (100,000 souls). Then the `ModeBanner` to everyone. Logs (feature + master) | reply line |
| `ModeBanner(heroMode)` | Random: `Random mode` / `Random hero and build every round`. 1v1: `1v1 mode` / `DuelService.SetupDescription`. Draft: `Draft mode` / `Pick your heroes` | (title, description) |
| `SetFormat(format, mode)` | Refuses during a match. Sets `MatchConfig.Format`, logs | reply line |
| `DescribeConfig()` | Config line, then the allowed modes (with `1v1 = duel`) / formats and intermission | 2 lines |
| `OnRoundEnded(result, mode)` | Ignored when idle. 1v1 mode: only `DuelService.RecordResult` (streak, best streak, loser to the back of the queue, boards), logs with king and streak (feature + master), the 1v1 banner, next round; no `State.Apply`, `SetRounds`, or `RecordRound`. Otherwise: `State.Apply`; `BalanceService.RecordRound(pointTo)`; `StatsService.SetRounds` (board round counts); `BettingService.OnRoundEnded(pointTo)` (bets paid, lost, or refunded on no point); Warning if `finished` had no known team; logs (feature + master); score banner; then `MapRefreshService.TryBegin(timer)`: when the join budget is reached it warns in chat, calls `End` and reloads the map 10 s later, and no next round is scheduled; otherwise schedules the next round | — |
| `SetIntermission(seconds, mode)` | 5 to 120 s (default 5); applies from the next countdown | `bool` |
| `DescribeMatch()` | Phase, round, score and ties (1v1: `King=<name> xN`), auto-start on/off (`AutoStartService.Enabled`); config, intermission, rift phase, next side; 1v1 adds the `DuelService.DescribeStreaks()` lines | 2+ lines |
| `DescribeScore()` | `Round X: Sapphire a - b Amber (ties t)`; 1v1: `Round X` then the streak leaderboard lines; idle: `No match is running.` | lines |

`StartRound` (private): does nothing unless the phase is `Intermission`.
In 1v1 mode without two ready fighters (`DuelService.ReadyToFight`, e.g. a
fighter left during the intermission) it logs, shows
`Waiting for fighters` / `<pairing>` and schedules another intermission
(which prepares the next pair).
If a rift is already running (for example a manual `/rift_start` during the
countdown), logs a Warning and waits; that rift's end schedules the next
countdown. If `RunRound` did not start a rift (gamerules missing), logs a
Warning and schedules another countdown without re-preparing heroes
(`prepareHeroes: false`).

## Logs

`Match` feature log (`match-YYYYMMDD.log`). Master: match started, each
round result with score (1v1: with the current streak), match ended, plus
Warning+.

## Invariants

- Every round-ending path in `RiftService` (finished, tied, cancelled,
  spawn timed out) reaches `OnRoundEnded`, so the loop never stalls.
- No per-tick work: at most three timer callbacks per intermission
  (countdown, final countdown, Random-mode build banner; all cancelled by
  `End`) plus one 1 s loadout callback per player in Random or 1v1 mode.
- Deaths are not handled here: a dead player respawns at the watch spot
  (restrained) through Lobby's `player_spawn` hook and is out until the
  next round.
  Kills, deaths and assists are counted by `Stats/StatsService`; the final
  stats stay on the boards after `End` until the next `Start`.
- Timers belong to `GameLoopPlugin`; the loop stops if that plugin unloads
  (the state stays and `/match_end` still works).
