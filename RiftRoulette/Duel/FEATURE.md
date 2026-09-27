# Duel — Feature

## Purpose

1v1 mode (Stage 13g). `/match_mode 1v1` (or `duel`) turns off the draft and
unlocks hero switching: players pick heroes from the in-game menu and get
100,000 souls and level 36 to build. When one player's build is ready, the
admin runs `/duel_copy <slot>`: that player's exact hero, items (with
imbues), ability upgrades, level, and ability points are copied onto both
players, they go on opposite teams, and the continuous match starts. During
the match both are locked to that hero (a menu swap kills and restores) and
every intermission resets both to the copy with 0 souls.

Since Stage 13j the mode is winner-stays-on with a join queue
(`Modules/Queue/PlayerQueue`). Players type `/queue`; the first two in the
queue fight. A capture makes the capturing fighter the winner: the loser
goes to the back of the queue and the next in line challenges the winner
(the king, with a streak count). A tie, cancel or timeout keeps both. With
only two queued, every round is a rematch. Only the two fighters get the
copied build and the hero lock; everyone else waits up top.

The only 1v1 score is the best-streak leaderboard: each player's highest
streak this match, highest first, on both side boards and in `/score`.
There are no team points in 1v1.

## Files

| File | Role |
|---|---|
| `DuelService.cs` | Snapshot, lock, queue, fighters, king / streak, setup souls, round re-apply, status |
| `KothRule.cs` | Winner / loser from the fighters' teams, streak (pure) |
| `StreakBoard.cs` | Best streak per player for the match (pure) |
| `DuelPlugin.cs` | Spawn hooks, `/queue`, `/unqueue`, `/duel_copy`, `/duel_clear`, `/duel_status`, `/duel_queue*` |

The copy itself is game-agnostic: `Modules/Loadout` (`LoadoutSnapshot`,
`LoadoutService.Capture` / `ApplySnapshot` / `SwapSnapshot`). The lock is
`Lobby/HeroLock` (shared with Random mode).

## Public operations

See `DuelService.md`. Player commands in `reference/user-commands.md`,
admin commands in `reference/admin-commands.md`.

## State

`DuelService` statics: the snapshot (kept across matches until cleared or
the mode changes), `Locked`, the hero lock, the fighters' teams, the queue
(kept across matches, cleared when leaving 1v1 mode), the king and streak,
and the best-streak board (cleared at match start and end).

## Lifecycle vs commands

- `GameLoop/MatchService`: `Start` refuses in 1v1 mode without a snapshot,
  then calls `BeginMatch`; `OnRoundEnded` in 1v1 mode calls only
  `RecordResult` (no team points, round boards, or balance) and shows the
  1v1 banners; `/score`, `/match_status` and the match-end banner read the
  leaderboard; `StartRound` waits another intermission when two
  fighters aren't ready; each intermission calls `PrepareRound`; `End`
  calls `EndMatch` after the lobby reset; `SetHeroMode` calls `Leave` when
  leaving 1v1 and `EnterSetup` when entering it. `GameLoop/ShopAccess`
  opens buying anywhere only during setup (1v1 mode, no match); the setup
  banner "1v1 setup" / "Shop open anywhere - build your hero" shows on
  `/duel_clear` and for joiners, and the `1v1 mode` banner carries the same
  text.
- `GameLoop/AutoStartService`: in 1v1 mode it counts queued players, not
  participants, and only starts when a snapshot exists. It ends the match
  when fewer than 2 are queued.
- `Round/RoundFlow.MoveTeamsToRift`: in 1v1 mode moves only fighters.
- `Lobby/LobbyService.AdmitPlayer`: setup souls for joiners outside a match.
- `Lobby/AdminSeat.Sit` and `Lobby/LobbyService.RemovePlayer`
  (disconnect): `Forget` (leaves the queue).
- `Draft/DraftService.EnforceHero`: asks `DuelService.GuardHero` first, then
  Random mode; in 1v1 mode Draft enforcement never runs.
- `Stats/StatsService`: skips deaths flagged by either lock; in 1v1 mode
  both side boards show the best-streak leaderboard (`StreakRows`) and
  K/D/A still counts for `/stats`. Balance stays off (Random mode only).
- `/duel_copy`, `/duel_clear`, `/duel_status`, `/duel_queue`,
  `/duel_queue_add`, `/duel_queue_remove` are admin commands (Debug).
  `/queue` and `/unqueue` are player commands (Clean).

## Logs

`duel-YYYYMMDD.log`; the copy's details are in `loadout-YYYYMMDD.log`.
