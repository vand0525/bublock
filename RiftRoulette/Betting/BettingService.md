# BettingService

Round betting in Random mode. Owns the static `BetBook` and the open /
closed flag; sends the chat lines and draws the leaderboard board. Log:
`Betting` (`betting-YYYYMMDD.log`).

## State

- `Book`: the `BetBook` (souls and open bets), one per DLL load; a
  reload resets it.
- `IsOpen`: bets accepted (the intermission and the first `LingerSeconds`, 10 s, of the round).
- `Active`: Random mode and a match running.

## Operations

| Op | Behavior |
|---|---|
| `Reset(mode)` | `MatchService.Start`: clears the book (everyone back to 100), closes betting, redraws the board |
| `Open(mode)` | `MatchService.ScheduleNextRound`, after the round is prepared. When `Active`: opens betting and sends every participant with souls and no open bet one chat line: `Bet on the next round: type sapphire or amber (open until 10s into the round). You have 300 souls; a win doubles them.` Players with an assignment for the coming round get `type <their team> to bet on your team`. A player with at least 1,000 souls and no hero reservation also gets ` Or /reserve <hero> for 1,000.` Returns how many were told |
| `Close(mode)` | `MatchService`, `LingerSeconds` (10 s) after the round started (skipped when that round already ended): closes betting, logs the bets and total staked. Bets placed before it count for the round being fought. Refusal after it: `Betting is closed - it opens again after this round.` |
| `TryBet(player, text, mode)` | Null when `text` is not a team name. Otherwise a reply: not `Active`, a spectator (seated admin), or closed each get a refusal; else `BetBook.Place` with the allowed teams and the reply `Bet 300 souls on Amber.`, `Bet moved: ...`, `No souls - get a kill or an assist.`, or `You're fighting for Sapphire - you can only bet on your team.` Logs every attempt; redraws the board on a placed or moved bet |
| `OnKill(killerId, mode)` | `StatsService.RecordDeath` on a credited kill, when `Active`: +100 souls, redraw |
| `OnAssists(assisterIds, mode)` | `StatsService.RecordDeath` with the credited assisters of that kill (`StatsLedger` rules), when `Active` and not empty: +50 souls each (Debug line per player), one redraw |
| `OnRoundEnded(winner, mode)` | `MatchService.OnRoundEnded` (Random mode) with the team that scored, or null: closes betting and settles. Each bettor still connected gets `You won 600 souls (now 600).`, `You lost 300 souls (now 0).`, or `No result - your 300 souls are back.` |
| `EndMatch(mode)` | `MatchService.End` (Random mode): closes and refunds every open bet (same chat line) |
| `RefreshBoard(mode)` | Random mode only: the `bets.leaders` board (`BetBoardText.Board` of connected participants' totals, souls plus open stake) at `BoardLayout.Back`; updates it in place when it exists. Also called from `DraftService.RedrawBoards` so it moves with the watch spot |
| `DescribePlayer(player)` | `/souls`: souls, the open bet, whether betting is open, then `RandomModeService.DescribeReservation` (their reservation, or how to buy one) |
| `Describe()` | `/bet_status`: active, open, bet count, total staked, then per participant slot, name, souls, bet |

## Invariants

- Players in the coming round's fight (a `RandomModeService` assignment)
  may only bet on their own team; the player sitting out and anyone
  without an assignment may bet on either.
- Betting itself never changes gameplay. Souls buy only a hero
  reservation (`/reserve`, `RandomModeService.Reserve`, 1,000 souls for 3
  fighting rounds) or a hero ban (`/heroban`, `RandomModeService.Ban`,
  1,000 souls, one round); spent souls leave the board total.
- Messages are chat only (`PlayerChat`), never a HUD banner.
- A round that fails to start keeps its bets; the reopened window skips
  players who already bet.
