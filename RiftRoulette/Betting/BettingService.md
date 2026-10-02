# BettingService

Round betting in Random mode. Owns the static `BetBook`, the `MarkBook`
and the open / closed flag; sends the chat lines and draws the leaderboard
board. Log: `Betting` (`betting-YYYYMMDD.log`).

## State

- `Book`: the `BetBook` (souls and open bets), one per DLL load; a
  reload resets it.
- `Marks`: the `MarkBook` (one mark per marker, for one round), reset with
  `Book`. A mark bought in an intermission is for `State.Round + 1`; one
  bought in a round's first `LingerSeconds`, for `State.Round`.
- `IsOpen`: bets accepted (the intermission and the first `LingerSeconds`, 10 s, of the round).
- `Active`: Random mode and a match running.

## Operations

| Op | Behavior |
|---|---|
| `Reset(mode)` | `MatchService.Start`: clears the book and the marks (everyone back to 100), closes betting, redraws the board |
| `Open(mode)` | `MatchService.ScheduleNextRound`, after the round is prepared. When `Active`: opens betting and sends every participant with souls and no open bet one chat line: `Bet on the next round: type sapphire or amber (open until 10s into the round). You have 300 souls; a win doubles them.` Players with an assignment for the coming round get `type <their team> to bet on your team`. A player with at least 1,000 souls and no hero reservation also gets ` Or /reserve <hero> for 1,000.`; a fighter with at least 300 souls and no mark also gets ` Or /mark an enemy for 300.` Returns how many were told |
| `Close(mode)` | `MatchService`, `LingerSeconds` (10 s) after the round started (skipped when that round already ended): closes betting, logs the bets and total staked. Bets placed before it count for the round being fought. Refusal after it: `Betting is closed - it opens again after this round.` |
| `TryBet(player, text, mode)` | Null when `text` is not a team name. Otherwise a reply: not `Active`, a spectator (seated admin), or closed each get a refusal; else `BetBook.Place` with the allowed teams and the reply `Bet 300 souls on Amber.`, `Bet moved: ...`, `No souls - get a kill or an assist.`, or `You're fighting for Sapphire - you can only bet on your team.` Logs every attempt; redraws the board on a placed or moved bet |
| `TryMark(player, text, mode)` | `/mark`. Reply lines. Refusals in order: not `Active` (`Marks are only open during a Random mode match.`), not a participant (`Spectators can't mark.`), no assignment (`Only this round's fighters can mark - you're sitting out.`), betting closed (`Marking is closed - it opens again after this round.`). Empty text: the menu, `Mark an enemy fighter for 300 souls: /mark <slot>. Kill them next round / this round to steal a quarter of their souls. You have N.`, their current mark if any, then one line per enemy fighter (a participant with an assignment on the other team, slot order): `<slot> <name> (<total> souls)`, or `No enemy fighters right now.` Already holding a mark: `You already marked X next round. One mark per round.` Text not a slot of an enemy fighter: `No enemy fighter in slot N - type /mark to see them.` Short of 300 unstaked souls: the price, their souls, and ` Your souls are on a bet until the round ends.` when they bet. Else spends 300, `Marks.Place`, tells the target `Someone marked you: if they kill you next round / this round, they take a quarter of your souls.` (never who), logs `Mark placed Target= TargetSteamId= Round= Chips=`, redraws, replies `Marked X for next round (N souls left). Kill them next round to steal a quarter of their souls.` |
| `OnKill(killerId, victimId, mode)` | `StatsService.RecordDeath` on a credited kill, when `Active`: +100 souls. Then, in a round (`InRound`) and when the killer holds a mark on the victim for this round (`Marks.TryConsume`): `BetBook.Steal(victim, killer, 4)`. Killer: `You stole N souls from X (mark).` (or `X had no souls to steal - your mark is used.`); victim: `Y stole N souls from you - you were marked.`; logs `Mark paid`. Redraw |
| `OnAssists(assisterIds, mode)` | `StatsService.RecordDeath` with the credited assisters of that kill (20% of the victim's max health dealt this life, `DamageLedger`; then `StatsLedger` rules), when `Active` and not empty: +50 souls each (Debug line per player), one redraw |
| `OnRoundEnded(winner, mode)` | `MatchService.OnRoundEnded` (Random mode) with the team that scored, or null: closes betting and settles. Each bettor still connected gets `You won 600 souls (now 600).`, `You lost 300 souls (now 0).`, or `No result - your 300 souls are back.` Then every unused mark ends (`Marks.TakeAll`): refunded 300 when `winner` is null (`No result - your 300 souls for the mark on X are back.`) or the target has no assignment any more (left, seated: `X didn't fight - your 300 souls for the mark are back.`), else lost (`Your mark on X ran out.`). Chat to markers still connected; a `Mark ended ... Refunded=` line each; one redraw. Returns the bet count |
| `EndMatch(mode)` | `MatchService.End` (Random mode): closes and refunds every open bet (same chat line) and every unused mark (`Match ended - your 300 souls for the mark are back.`) |
| `RefreshBoard(mode)` | Random mode only: the `bets.leaders` board (`BetBoardText.Board` of connected participants' totals, souls plus open stake) at `BoardLayout.Back`; updates it in place when it exists. Also called from `Boards/BoardService.Redraw` so it moves with the watch spot |
| `DescribePlayer(player)` | `/souls`: souls, the open bet, their mark (`Your mark: X (next round).`), whether betting is open, then `RandomModeService.DescribeReservation` (their reservation, or how to buy one) |
| `Describe()` | `/bet_status`: active, open, bet count, total staked, mark count, then per participant slot, name, souls, bet, mark (target and round) |

## Invariants

- Players in the coming round's fight (a `RandomModeService` assignment)
  may only bet on their own team; the player sitting out and anyone
  without an assignment may bet on either.
- Betting itself never changes gameplay. Souls buy only a hero
  reservation (`/reserve`, `RandomModeService.Reserve`, 1,000 souls for 3
  fighting rounds), a hero ban (`/heroban`, `RandomModeService.Ban`,
  1,000 souls, one round), or a mark (`/mark`, 300 souls, one round);
  spent souls leave the board total.
- Marks: only that round's fighters mark, only an enemy fighter, one mark
  per marker per round; it pays only on the marker's credited kill on the
  target in that round. A steal is a quarter of the target's total, free
  souls first, then off their open bet. The target is told they were
  marked, never by whom, until the steal.
- Messages are chat only (`PlayerChat`), never a HUD banner.
- A round that fails to start keeps its bets; the reopened window skips
  players who already bet.
