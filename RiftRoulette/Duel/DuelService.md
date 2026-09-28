# DuelService

1v1 mode orchestration (`HeroMode.Duel`, `/match_mode 1v1`). One
player builds a hero freely; the admin copies that exact hero (items,
imbues, ability upgrades, level, ability points) onto both players, and the
match runs with both locked to it, re-applied every intermission. A queue
decides who fights: the first two in the queue, winner
stays on, loser to the back. Static state, one per DLL load.

## State

- `Snapshot`: the copied `LoadoutSnapshot` (`HasSnapshot`). Kept across
  match end so auto-start can continue with the same build; dropped by
  `ClearSnapshot` and when leaving 1v1 mode.
- `Locked`: true while a 1v1 match runs.
- `Lock`: a `HeroLock` (applied / pending / enforcement kills).
  `PendingCount`.
- `Teams`: Steam ID to team for the current fighters only (`IsFighter`,
  `ReadyToFight` when it holds 2).
- `Queue`: a `PlayerQueue` (`[fighterA, fighterB, next...]`). Kept across
  matches; cleared by `Leave`.
- `King` / `Streak`: last winner and how many rounds in a row they won.
  Reset at match start and end, and when the king disconnects.
- `Streaks`: a `StreakBoard`, each player's best streak this match (the
  1v1 score). Same lifetime as `King` / `Streak` (cleared by `BeginMatch`,
  `EndMatch`, `Leave`); `Forget` drops one player's row.
- Constants: `SetupGold = 100_000`, `SetupLevel = 36`, `CompetitorCount = 2`,
  `SetupDelaySeconds = 1`.

## Operations

| Op | Behavior | Returns |
|---|---|---|
| `Copy(source, timer, mode)` | Refuses unless 1v1 mode, no match, source is a participant with a live pawn, and 2 or more connected participants are queued (the source need not be queued). Captures `LoadoutService.Capture(pawn)`, logs it (and a master line), then `MatchService.Start` | reply |
| `ClearSnapshot(timer, mode)` | Refuses during a match. Drops the build; in 1v1 mode re-grants setup souls | reply |
| `BeginMatch(mode)` | Clears the lock, teams, king, streak and best streaks, `Locked = true`, prunes the queue | — |
| `RecordResult(result, mode)` | After each round in 1v1 mode. A `finished` result: `KothRule.Winner` / `Loser` from `Teams`; `KothRule.Crown` updates king and streak, `Streaks.Record` raises the king's best, the loser goes to the back of the queue, Info (with `Best`) + master line, then `StatsService.RefreshBoards`. Tie / cancel / timeout / unknown winner: both stay (Info line) | winner or null |
| `PrepareRound(timer, mode)` | Each intermission: prunes the queue (disconnected or seated), drops non-fighters from `Teams` and the lock, clears pending. Fewer than 2 queued: logs and returns 0. Otherwise assigns teams (a fighter already in `Teams`, the winner, keeps their team and the challenger gets the other; two new fighters get `TeamBalance.Even`), then `Start` per fighter (dead → pending). Refreshes stats boards | players applied now |
| `JoinQueue(player, mode)` | Refused outside 1v1 mode or for non-participants. Already queued: replies with the position. Else `Queue.Join`, Info line | reply |
| `LeaveQueue(player, mode, force)` | Not queued: reply. A fighter during a running rift: refused. A fighter during a match intermission: refused unless `force` (admin). Else leaves the queue and the fighter slot | reply |
| `QueuedCount(leavingSteamId)` | Queued players who are still connected participants, minus the leaving one | count |
| `IsFighter(steamId)` / `ReadyToFight` | Locked and in `Teams` / `Teams` holds 2 | bool |
| `ResultHeadline(result)` | `<Winner> wins (streak N)`, or `Tie - both stay` / `Round cancelled - both stay` / `Rift did not spawn - both stay` / `No winner - both stay` | text |
| `NextPairing()` | `<first> vs <second>` from the queue front, or `Waiting for a challenger (/queue)` | text |
| `DescribeQueue()` | Header with the pairing, then `N. name (fighting, king, streak N)` per queued player | lines |
| `ApplyPending(player, timer, mode)` | 1v1 match, player pending and alive: `Start`, clear pending | bool |
| `GuardHero(player, pawn, timer, mode)` | Not 1v1: false. 1v1: returns true (Draft enforcement never runs); for a fighter, `Lock.Enforce` against the snapshot hero, rebuilding with `Start`. Queued players who aren't fighting switch freely | bool handled |
| `ConsumeEnforcementKill(steamId)` | `Lock.ConsumeKill` | bool |
| `EndMatch(timer, mode)` | `Locked = false`, clears lock, teams, king, streak and best streaks, keeps the snapshot and the queue, refreshes the boards (`No streaks yet`), then `EnterSetup(announce: false)` (the `Match over` banner shows instead) | competitors |
| `EnterSetup(timer, mode, announce = false)` | `GrantSetup(announce)` for every participant. `ClearSnapshot` passes `announce: true`; `MatchService.SetHeroMode` passes false and shows its own `1v1 mode` banner | players |
| `GrantSetup(player, timer, mode, announce = false)` | After 1 s, if still 1v1 with no match and the player is an alive participant: raises `Level` to 36 (then `ModifyCurrency(EGold, 0, ECheats, silent)` to recalculate) and sets gold to 100,000; with `announce`, shows `SetupTitle` / `SetupDescription` ("1v1 setup" / "Shop open anywhere - build your hero"; the admin command is not in player text). Debug line logs level, AP, and unlocks. `LobbyService.AdmitPlayer` announces for a joiner; the respawn hook (`DuelPlugin`) does not, so hero switches during setup stay quiet | — |
| `Forget(steamId)` | Removes from the queue, teams, lock and best streaks; clears king and streak if it was the king (admin seat, disconnect; both callers refresh the boards) | — |
| `Leave(mode)` | Drops snapshot, lock, teams, queue, king, best streaks (mode switched away from 1v1) | — |
| `KingName()` | King's name, or `-` | text |
| `StreakRows()` | Best streaks as `StreakRow(name, best)`, ranked by `StatsBoardText.RankStreaks` (names from connected players, Steam ID otherwise) | rows |
| `DescribeStreaks()` | `STREAKS`, then `1  Name   5` lines or `No streaks yet` (`/score`, `/match_status`) | lines |
| `StreakSummary()` | `Best streaks: A 5, B 3` or `No streaks yet` (match-end banner and reply) | text |
| `Describe()` | Config / lock / pending / queued / king line, build line, item list, then one line per participant (slot, name, team, hero, queue position, FIGHTER, PENDING) | lines |

### Start (private)

Alive pawn required (else false → pending). `ChangeTeam(team)` if needed,
`Lock.Unapply`, then `LoadoutService.SwapSnapshot(player, snapshot, timer,
gold: 0)`: same hero → applied now; different hero → `SelectHero`, then
after 1 s `ApplySnapshot`. When applied during a locked match the player is
marked applied. No chat line (the countdown banner names the pairing).

## Invariants

- Setup (1v1 mode, no match): no hero lock, no Draft enforcement; players
  switch heroes from the menu and get 100,000 souls on mode switch, on
  admit, and on each spawn.
- Match: every intermission resets both fighters to the exact copy with 0
  souls, so anything bought mid-round is wiped.
- Queue order is the fight order: fighters are always `Queue.Front(2)`
  after `PrepareRound`. Only a finished round with a known winner reorders
  it.
- A fighter who disconnects mid-intermission leaves `ReadyToFight` false;
  `MatchService.StartRound` then waits one more intermission and prepares
  the next pair.
- The snapshot is exact: no soul cap and no banned-item filter.
- The 1v1 score is the best-streak leaderboard only. `MatchService` does not
  add team points, round boards, or balance rounds in 1v1 mode. A best
  never drops within a match; the banner's `(streak N)` is the current run.

## Deadworks constraints

- Never `SelectHero` / `ChangeTeam` while dead (dead players are pending).
- Whether raising `Level` alone grants ability points is unconfirmed; the
  setup Debug line logs AP and unlocks for checking in game.
