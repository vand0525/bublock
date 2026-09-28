# Rift Roulette — User Commands

Player-facing commands. Keep this catalog accurate whenever commands are
added, renamed, restricted, or removed.

Populate entries from verified source. Do not invent undocumented commands.

## Entry template

```markdown
### /command_name <args>

- **Invocation:** chat `/command_name` | console `dw_…` (if any)
- **Who:** players (subject to game-state rules)
- **Calls:** core operation(s) …
- **Mode:** Clean by default (Debug OFF)
- **Side effects:** …
- **Notes:** …
```

## Commands

Every player command below runs in game only (players, not the server
console) and replies in the caller's chat.

### /pick <hero>

- **Invocation:** chat `/pick <hero>` or `!pick <hero>` | console `dw_pick <hero>`
- **Who:** players, in game (`DraftPlugin`)
- **Calls:** `DraftService.Pick` (then `GiveStartingProgression` next tick, then `RedrawBoards`)
- **Mode:** Clean; logged in `draft-*.log`
- **Side effects:** refused (chat reply) if dead, unknown hero, caller already picked, hero already drafted, or hero not in a pool. On success: records the pick, moves the caller to Sapphire (team 3) or Amber (team 2), switches their hero, sets gold to 25,000 next tick, redraws the draft boards, and confirms in chat
- **Notes:** hero name is the case-insensitive `Heroes` enum name. In Random mode (the default) it only replies "Heroes are random this match - you get a new hero and build every round." In 1v1 mode it replies "1v1 mode - pick a hero from the hero menu; the admin copies one build onto both players."

### /unpick

- **Invocation:** chat `/unpick` or `!unpick` | console `dw_unpick`
- **Who:** players, in game (`DraftPlugin`)
- **Calls:** `DraftService.Unpick`
- **Mode:** Clean; logged in `draft-*.log`
- **Side effects:** refused (chat reply) if no pick or dead. Otherwise zeroes gold, ability points, and level; releases the hero; moves the caller to team 2 as Skyrunner; teleports them to the draft area next tick; redraws boards; confirms in chat
- **Notes:** in Random mode it only replies that heroes are random; in 1v1 mode it gives the 1v1 reply

### /picks

- **Invocation:** chat `/picks` or `!picks` | console `dw_picks`
- **Who:** players, in game (`DraftPlugin`)
- **Calls:** `DraftService.DescribePicks`
- **Mode:** Clean; read-only
- **Side effects:** chat reply with every drafted hero and its picker, e.g. `Picks: Shiv (Theo, slot 3), Fencer (offline)`, or `Picks: none`

### /heroes

- **Invocation:** chat `/heroes` or `!heroes` | console `dw_heroes`
- **Who:** players, in game (`DraftPlugin`)
- **Calls:** `DraftService.DescribeHeroes` (→ `DescribePools` in Draft mode)
- **Mode:** Clean; read-only
- **Side effects:** Draft mode: two chat lines, one per pool, e.g. `Sapphire: Shiv (taken), Yamato, ...`. Random mode: one line saying heroes are random. 1v1 mode: the 1v1 line

### /status

- **Invocation:** chat `/status` or `!status` | console `dw_status`
- **Who:** players, in game
- **Calls:** `LobbyService.DescribePlayer` (`LobbyPlugin`)
- **Mode:** Clean
- **Side effects:** replies to the caller in chat with one line: slot, name, Steam ID, team (Sapphire / Amber), pick (or `-`), then hero, life state, alive, health, max health, position, entity index (`Pawn=NULL` if they have no hero). The same line is logged at Information in `players-*.log`

### /commands

- **Invocation:** chat `/commands` or `!commands` | console `dw_commands`
- **Who:** players, in game (`LobbyPlugin`)
- **Calls:** `CommandList.PlayerCommands` (reads the `[Command]` attributes in RiftRoulette.dll)
- **Mode:** Clean; read-only; not logged
- **Side effects:** one chat line per player command (`/name - Description`, sorted: `/bet`, `/commands`, `/heroes`, `/pick`, `/picks`, `/queue`, `/reserve`, `/score`, `/souls`, `/stats`, `/status`, `/unpick`, `/unqueue`), then `Full list: dw_help in console`
- **Notes:** Deadworks' built-in `dw_help` only runs from the game console (there is no chat `/help`) and lists every visible command, admin ones included; `/commands` gives players a chat list of just their commands

### /score

- **Invocation:** chat `/score` or `!score` | console `dw_score`
- **Who:** players, in game (`GameLoopPlugin`)
- **Calls:** `MatchService.DescribeScore`
- **Mode:** Clean; read-only; not logged
- **Side effects:** one chat line, `Round 3: Sapphire 2 - 1 Amber (ties 0)`, or `No match is running.` In 1v1 mode: `Round 3`, `STREAKS`, then one line per player with a win, `1  Name   5`, highest best streak first (or `No streaks yet`)
- **Notes:** the same score appears on screen (HUD banner) after every round. In 1v1 the best streak is the highest run a player reached this match (it does not drop when they lose); the same list is on both side boards

### /stats

- **Invocation:** chat `/stats` or `!stats` | console `dw_stats`
- **Who:** players, in game (`StatsPlugin`)
- **Calls:** `StatsService.Describe`
- **Mode:** Clean; read-only; not logged
- **Side effects:** two chat lines: `You (K / D / A): 3 / 1 / 2`, then `Sapphire: 10 / 8 / 12 | Amber: 8 / 10 / 9` (team totals of connected players by current team)
- **Notes:** counts from `/match_start`; deaths outside a match and hero-swap punishment deaths are not counted. In Random and 1v1 mode the same numbers are on the Sapphire and Amber boards at the watch spot

### /queue

- **Invocation:** chat `/queue` or `!queue` | console `dw_queue`
- **Who:** players, in game (`DuelPlugin`); only in 1v1 mode
- **Calls:** `DuelService.JoinQueue`, then `AutoStartService.Check`
- **Mode:** Clean; logged in `duel-*.log` (Information)
- **Side effects:** puts the caller at the back of the 1v1 queue and replies `<name> joined the 1v1 queue at position N of M.`; if already queued, replies with the current position. The first two in the queue fight; the winner stays on, the loser goes to the back. With auto-start on, the match starts once a build is copied and 2 are queued
- **Notes:** refused outside 1v1 mode (`The queue is only open in 1v1 mode.`) and for a seated admin. Disconnecting or taking the admin seat removes you from the queue

### /unqueue

- **Invocation:** chat `/unqueue` or `!unqueue` | console `dw_unqueue`
- **Who:** players, in game (`DuelPlugin`)
- **Calls:** `DuelService.LeaveQueue`, then `AutoStartService.Check`
- **Mode:** Clean; logged in `duel-*.log` (Information)
- **Side effects:** removes the caller from the 1v1 queue (`<name> left the 1v1 queue.`); with fewer than 2 left in the queue, auto-start ends the match
- **Notes:** a fighter can't leave during a match (`is fighting now` mid-round, `fights next round` between rounds); an admin can remove them between rounds with `/duel_queue_remove`

### /bet <sapphire|amber> (or type the team name in chat)

- **Invocation:** chat `sapphire` / `amber` (the whole message, any case) | chat `/bet <team>` or `!bet <team>` | console `dw_bet <team>`
- **Who:** players, in game (`BettingPlugin`); Random mode match only
- **Calls:** `BettingService.TryBet` → `BetBook.Place`
- **Mode:** Clean; logged in `betting-*.log`
- **Side effects:** bets all your souls on that team for the next round; typing the other team before the round starts moves the bet. Players in the coming round's fight may only bet on their own team; the player sitting out may bet on either. Chat reply: `Bet 300 souls on Amber.` At round end: `You won 600 souls (now 600).`, `You lost 300 souls (now 0).`, or `No result - your 300 souls are back.` (tie, cancel, match end). The betting board updates
- **Notes:** everyone starts a match with 100 souls and earns 100 per kill. Betting opens at the start of the intermission (5 s) and closes 10 s into the round (`Betting is closed - it opens again after this round.`); with 0 souls: `No souls - get a kill.` A plain team-name chat message outside a Random match is ignored; `/bet` then replies that betting is only open during a Random mode match

### /souls

- **Invocation:** chat `/souls` or `!souls` | console `dw_souls`
- **Who:** players, in game (`BettingPlugin`)
- **Calls:** `BettingService.DescribePlayer`
- **Mode:** Clean; read-only; not logged
- **Side effects:** chat lines: `You have N souls.`, your open bet if any, whether betting is open, and your hero reservation (or how to buy one with `/reserve`)

### /reserve [hero]

- **Invocation:** chat `/reserve <hero>` or `!reserve <hero>` | console `dw_reserve <hero>`. The hero's name, any case; spaces allowed (`/reserve lady geist`). No argument: shows your reservation
- **Who:** players, in game (`RandomPlugin`); Random mode match only
- **Calls:** `RandomModeService.Reserve` → `BetBook.TrySpend`, `HeroReservations.TryReserve`
- **Mode:** Clean; logged in `random-*.log`
- **Side effects:** spends 1,000 souls (not souls riding on a bet) and puts you in line for that hero. Nobody ahead: `Reserved Haze for your next 3 rounds, starting the round after this one (250 souls left).` Someone ahead: `Kamilk has reserved Haze. When their 3 rounds are done, it will be your turn.` (with more ahead: `2 players are ahead of you for Haze (5 rounds). Then it will be your turn.`). From the next hero draw, the first player in the hero's line who is fighting that round gets it with a random stored build, and a chat line: `Your reserved hero is up: Haze (round 1 of 3).`, then `Reserved hero: Haze (round 2 of 3).` Everyone else is drawn randomly from the other heroes. The betting board total drops by 1,000
- **Notes:** one reservation per player (holding or waiting). Rounds you sit out don't count; if the holder sits out or is away, the next fighter in line plays the hero that round. Refused outside a Random match, for spectators, for an unknown hero, and with fewer than 1,000 free souls. No refunds: an unused reservation ends with the match (souls reset at every match start)
