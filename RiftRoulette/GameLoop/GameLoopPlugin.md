# GameLoopPlugin

Thin plugin class for the match loop (`Name` = "Rift Roulette Game Loop").
Its `Timer` drives the match countdowns and the rounds the loop starts.

## Hooks

| Hook | Behavior |
|---|---|
| `OnLoad(isReload)` | 3 s later runs `AutoStartService.Check(Timer)` (Clean), so players already connected at a reload get a match without a connect event |
| `OnGameFrame(simulating)` | Counts the frame (`SelfTest/EventCounters.Hit("game_frame")`), then while simulating: `Round/WatchGuard.Tick()` (checks every 16 frames that waiting players are still up top) |
| `OnClientConCommand` | For a restrained player: `WatchGuard.LogCommand` (to find what the menu's Unstuck sends). Always returns `HookResult.Continue`; nothing is blocked |
| `OnModifyCurrency` | Counts `modify_currency` (self-test). When `SoulRule.ShouldBlock(type, source, amount, MatchService.State.IsRunning)`: counts `soul_blocked_<Source>`, writes a Debug `Soul gain blocked Amount= Source=` line (with the player when the pawn has a controller) and returns `HookResult.Stop`; otherwise `Continue` |

## Commands

Admin commands check `AdminCommand.Authorize` with the `Match` log (server
console trusted), run in Debug mode, and reply to the caller's console with
a `[Match]` prefix.

| Command | Who | Calls | Reply / errors |
|---|---|---|---|
| `/match_start` | admin | `MatchService.Start(Timer, Debug)` | `Match started. Round 1 in 15s.`, or the refusal if a match or a rift is running |
| `/match_end` | admin | `MatchService.End(Timer, Debug)` | `Match ended after N round(s). Final: <score, or 1v1 best streaks>. M player(s) returned to the lobby.`, or `No match is running.` |
| `/match_auto <on\|off>` | admin | `AutoStartService.SetEnabled(Debug)`; when turned on, also `AutoStartService.Check(Timer, Debug)` | `Auto-start on` (plus ` - match started` if the check started one) or `Auto-start off`; error on any other argument |
| `/match_status` | admin | `MatchService.DescribeMatch` | two status lines (includes auto-start, mode and format); 1v1 shows the king and adds the streak leaderboard |
| `/match_intermission <seconds>` | admin | `MatchService.SetIntermission` | confirmation; error outside 5-120 |
| `/match_mode <random\|draft\|duel\|1v1>` | admin | `MatchConfig.TryParseHeroMode`, `MatchService.SetHeroMode(Timer, Debug)` (syncs buying, shows the mode banner) | `Mode set to random. N player(s) returned to the lobby.`; refusal during a match or when unchanged; error on an unknown mode |
| `/match_format <continuous>` | admin | `MatchConfig.TryParseFormat`, `MatchService.SetFormat(Debug)` | `Format set to continuous.`; refusal during a match; error on an unknown format |
| `/match_config` | admin | `MatchService.DescribeConfig` | two config lines |
| `/score` | anyone | `MatchService.DescribeScore` | one chat line per line: team score, or in 1v1 `Round X` then the best-streak leaderboard |

## Invariants

- The match started by `/match_start` runs in Debug mode (admin
  invocation), so its round lines are detailed; the loop and its ops are
  the same in Clean mode.
- `/score` has no `_` so it appears in `/commands`.
- The soul block only runs while a match runs; the lobby and 1v1 setup
  keep normal souls (setup souls, buying).
- With auto-start on, `/match_end` holds only until the next join (or a
  disconnect that drops below 2 players), and `/match_mode` is refused while 2+ players keep a match
  running: use `/match_auto off` first.
