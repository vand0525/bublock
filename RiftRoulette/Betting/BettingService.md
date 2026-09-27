# BettingService

Round betting in Random mode. Owns the static `BetBook` and the open /
closed flag; sends the chat lines and draws the leaderboard board. Log:
`Betting` (`betting-YYYYMMDD.log`).

## State

- `Book`: the `BetBook` (chips and open bets), one per DLL load; a
  reload resets it.
- `IsOpen`: bets accepted (the intermission).
- `Active`: Random mode and a match running.

## Operations

| Op | Behavior |
|---|---|
| `Reset(mode)` | `MatchService.Start`: clears the book (everyone back to 100), closes betting, redraws the board |
| `Open(mode)` | `MatchService.ScheduleNextRound`, after the round is prepared. When `Active`: opens betting and sends every participant with chips and no open bet one chat line: `Bet on the next round: type sapphire or amber. You have 300 chips; a win doubles them.` Players with an assignment for the coming round get `type <their team> to bet on your team`. Returns how many were told |
| `Close(mode)` | `MatchService.StartRound` once the round started: closes betting, logs the bets and total staked |
| `TryBet(player, text, mode)` | Null when `text` is not a team name. Otherwise a reply: not `Active`, a spectator (seated admin), or closed each get a refusal; else `BetBook.Place` with the allowed teams and the reply `Bet 300 chips on Amber.`, `Bet moved: ...`, `No chips - get a kill.`, or `You're fighting for Sapphire - you can only bet on your team.` Logs every attempt; redraws the board on a placed or moved bet |
| `OnKill(killerId, mode)` | `StatsService.RecordDeath` on a credited kill, when `Active`: +100 chips, redraw |
| `OnRoundEnded(winner, mode)` | `MatchService.OnRoundEnded` (Random mode) with the team that scored, or null: closes betting and settles. Each bettor still connected gets `You won 600 chips (now 600).`, `You lost 300 chips (now 0).`, or `No result - your 300 chips are back.` |
| `EndMatch(mode)` | `MatchService.End` (Random mode): closes and refunds every open bet (same chat line) |
| `RefreshBoard(mode)` | Random mode only: the `bets.leaders` board (`BetBoardText.Board` of connected participants' totals, chips plus open stake) at `BoardLayout.Back`; updates it in place when it exists. Also called from `DraftService.RedrawBoards` so it moves with the watch spot |
| `DescribePlayer(player)` | `/chips`: chips, the open bet, whether betting is open |
| `Describe()` | `/bet_status`: active, open, bet count, total staked, then per participant slot, name, chips, bet |

## Invariants

- Players in the coming round's fight (a `RandomModeService` assignment)
  may only bet on their own team; the player sitting out and anyone
  without an assignment may bet on either.
- Betting never changes gameplay: chips are only a number on a board.
- Messages are chat only (`PlayerChat`), never a HUD banner.
- A round that fails to start keeps its bets; the reopened window skips
  players who already bet.
