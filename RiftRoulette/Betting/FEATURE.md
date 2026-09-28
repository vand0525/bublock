# Betting — Feature

## Purpose

Round betting in Random mode, to give players something to do between
rounds (and the player sitting out something to do during one). Everyone
starts a match with 100 souls and earns 100 per kill. During each
intermission everyone gets a chat line with their souls and types
`sapphire` or `amber` to bet them all on the next round; a win doubles the
stake. A `BETTING` leaderboard hangs on the empty fourth side of the watch
spot.

Betting souls can be spent on two things: a hero reservation (`/reserve <hero>`,
1,000 souls for the player's next 3 fighting rounds, a waiting line per
hero) and a hero ban (`/heroban <hero>`, 1,000 souls, one hero out of the
next draw for both teams, one per team per round). The rules live in
`RandomMode/HeroReservations` and `RandomMode/HeroBans`; this feature only
provides `BetBook.TrySpend` and mentions `/reserve` in the betting-open line
(players with 1,000+ souls and no reservation) and in `/souls`.

## Public operations

See `BettingService.md` (lifecycle and commands), `BetBook.md` (soul
rules), `BetBoardText.md` (board text).

## State

`BettingService.Book` (souls and open bets) and `IsOpen`, static, one per
DLL load, reset at every match start. Betting souls last one match.

## Units

| File | Role |
|---|---|
| `BetBook.cs` | Betting souls, all-in bets, own-team rule, settle / refund, spending (pure, tested) |
| `BetBoardText.cs` | Leaderboard text (pure, tested) |
| `BettingService.cs` | Open / close, chat lines, kill souls, payouts, board |
| `BettingPlugin.cs` | `OnChatMessage` team words, `/bet`, `/souls`, `/bet_status` |

## Lifecycle vs commands

- `GameLoop/MatchService`: `Reset` in `Start`, `Open` at the start of every
  intermission (after the round is prepared), `Close` 10 s into the round
  (`LingerSeconds`; the intermission is only 5 s),
  `OnRoundEnded` with the scoring team, `EndMatch` in `End` (refunds).
- `Stats/StatsService.RecordDeath`: `OnKill` for every credited kill.
- `Draft/DraftService.RedrawBoards`: `RefreshBoard` with the stats boards,
  so the board moves with the watch spot.
- Players bet by typing a team name in chat or `/bet <team>`; `/souls`
  shows their souls. Admins read everything with `/bet_status`.
- Off in Draft and 1v1 mode.

## Logs

`betting-YYYYMMDD.log`: open / close, every bet attempt, kill souls
(Debug), each settlement.
