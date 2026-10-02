# Betting — Feature

## Purpose

Round betting in Random mode, to give players something to do between
rounds (and the player sitting out something to do during one). Everyone
starts a match with 100 souls and earns 100 per kill and 50 per assist (an
assist needs at least 20% of the victim's max health dealt this life;
`Stats/DamageLedger`). During each
intermission everyone gets a chat line with their souls and types
`sapphire` or `amber` to bet them all on the next round; a win doubles the
stake. A `BETTING` leaderboard hangs on the empty fourth side of the watch
spot.

Betting souls can be spent on three things: a hero reservation (`/reserve <hero>`,
1,000 souls for the player's next 3 fighting rounds, a waiting line per
hero), a hero ban (`/heroban <hero>`, 1,000 souls, one hero out of the
next draw for both teams, one per team per round), and a mark
(`/mark <slot>`, 300 souls). The reservation and ban rules live in
`RandomMode/HeroReservations` and `RandomMode/HeroBans`; this feature
provides `BetBook.TrySpend` and mentions `/reserve` in the betting-open line
(players with 1,000+ souls and no reservation) and in `/souls`.

Marks live here (`MarkBook`, `BettingService.TryMark`). While betting is
open, a fighter marks an enemy fighter for the round (`/mark` alone lists
them by slot). The target is told someone marked them, never who. If the
marker gets the credited kill on the target that round, they steal a
quarter of the target's total souls: free souls first, then off the
target's open bet. One mark per marker per round; several players may
mark the same target. An unused mark is lost at round end, or refunded
when the round had no result, the target didn't fight, or the match ended.

## Public operations

See `BettingService.md` (lifecycle and commands), `BetBook.md` (soul
rules), `BetBoardText.md` (board text).

## State

`BettingService.Book` (souls and open bets), `Marks` (one mark per marker,
one round) and `IsOpen`, static, one per DLL load, reset at every match
start. Betting souls last one match.

## Units

| File | Role |
|---|---|
| `BetBook.cs` | Betting souls, all-in bets, own-team rule, settle / refund, spending, mark refunds and steals (pure, tested) |
| `BetBoardText.cs` | Leaderboard text (pure, tested) |
| `MarkBook.cs` | Marks: one per marker, consume on the marked kill, take all at round end (pure, tested) |
| `BettingService.cs` | Open / close, chat lines, kill and assist souls, payouts, marks and steals, board |
| `BettingPlugin.cs` | `OnChatMessage` team words, `/bet`, `/mark`, `/souls`, `/bet_status` |

## Lifecycle vs commands

- `GameLoop/MatchService`: `Reset` in `Start`, `Open` at the start of every
  intermission (after the round is prepared), `Close` 10 s into the round
  (`LingerSeconds`; the intermission is only 5 s),
  `OnRoundEnded` with the scoring team (settles bets, ends marks),
  `EndMatch` in `End` (refunds bets and marks).
- `Stats/StatsService.RecordDeath`: `OnKill(killer, victim)` for every
  credited kill (pays a mark on that victim), then `OnAssists` with that
  kill's credited assisters.
- `Lobby/AboutText` (`/about`) explains betting to players with the
  `BetBook` numbers.
- `Boards/BoardService.Redraw`: `RefreshBoard` with the stats boards,
  so the board moves with the watch spot.
- Players bet by typing a team name in chat or `/bet <team>`, mark with
  `/mark [slot]`; `/souls` shows their souls. Admins read everything with
  `/bet_status`.
- Off in Mirror mode.

## Logs

`betting-YYYYMMDD.log`: open / close, every bet attempt, kill and assist
souls (Debug), each settlement, marks placed, paid and ended.
