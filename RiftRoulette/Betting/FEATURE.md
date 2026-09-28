# Betting — Feature

## Purpose

Round betting in Random mode, to give players something to do between
rounds (and the player sitting out something to do during one). Everyone
starts a match with 100 chips and earns 100 per kill. During each
intermission everyone gets a chat line with their chips and types
`sapphire` or `amber` to bet them all on the next round; a win doubles the
stake. A `BETTING` leaderboard hangs on the empty fourth side of the watch
spot.

Chips can be spent on one thing: a hero reservation (`/reserve <hero>`,
1,000 chips for the player's next 3 fighting rounds, a waiting line per
hero). The rules live in `RandomMode/HeroReservations`; this feature only
provides `BetBook.TrySpend` and mentions `/reserve` in the betting-open line
(players with 1,000+ chips and no reservation) and in `/chips`.

## Public operations

See `BettingService.md` (lifecycle and commands), `BetBook.md` (chip
rules), `BetBoardText.md` (board text).

## State

`BettingService.Book` (chips and open bets) and `IsOpen`, static, one per
DLL load, reset at every match start. Chips last one match.

## Units

| File | Role |
|---|---|
| `BetBook.cs` | Chips, all-in bets, own-team rule, settle / refund, spending (pure, tested) |
| `BetBoardText.cs` | Leaderboard text (pure, tested) |
| `BettingService.cs` | Open / close, chat lines, kill chips, payouts, board |
| `BettingPlugin.cs` | `OnChatMessage` team words, `/bet`, `/chips`, `/bet_status` |

## Lifecycle vs commands

- `GameLoop/MatchService`: `Reset` in `Start`, `Open` at the start of every
  intermission (after the round is prepared), `Close` 10 s into the round
  (`LingerSeconds`; the intermission is only 5 s),
  `OnRoundEnded` with the scoring team, `EndMatch` in `End` (refunds).
- `Stats/StatsService.RecordDeath`: `OnKill` for every credited kill.
- `Draft/DraftService.RedrawBoards`: `RefreshBoard` with the stats boards,
  so the board moves with the watch spot.
- Players bet by typing a team name in chat or `/bet <team>`; `/chips`
  shows their chips. Admins read everything with `/bet_status`.
- Off in Draft and 1v1 mode.

## Logs

`betting-YYYYMMDD.log`: open / close, every bet attempt, kill chips
(Debug), each settlement.
