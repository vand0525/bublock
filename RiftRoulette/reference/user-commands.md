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

### /status

- **Invocation:** chat `/status` or `!status` | console `dw_status`
- **Who:** players, in game
- **Calls:** `LobbyService.DescribePlayer` (`LobbyPlugin`)
- **Mode:** Clean
- **Side effects:** replies to the caller in chat with one line: slot, name, Steam ID, team (Sapphire / Amber), round hero (or `-`), then hero, life state, alive, health, max health, position, entity index (`Pawn=NULL` if they have no hero). The same line is logged at Information in `players-*.log`

### /commands

- **Invocation:** chat `/commands` or `!commands` | console `dw_commands`
- **Who:** players, in game (`LobbyPlugin`)
- **Calls:** `CommandList.PlayerCommands` (reads the `[Command]` attributes in RiftRoulette.dll)
- **Mode:** Clean; read-only; not logged
- **Side effects:** one chat line per player command (`/name - Description`, sorted: `/about`, `/bet`, `/commands`, `/heroban`, `/mark`, `/reserve`, `/score`, `/souls`, `/stats`, `/status`), then `Full list: dw_help in console`
- **Notes:** Deadworks' built-in `dw_help` only runs from the game console (there is no chat `/help`) and lists every visible command, admin ones included; `/commands` gives players a chat list of just their commands

### /about

- **Invocation:** chat `/about` or `!about` | console `dw_about`
- **Who:** players, in game (`LobbyPlugin`)
- **Calls:** `AboutText.Lines(BettingService.LingerSeconds)` (mirror mode: `AboutText.MirrorLines()`)
- **Mode:** Clean; read-only; not logged
- **Side effects:** seven chat lines to the caller: Rift Roulette gives everyone a random hero with one of its top builds every round; teams stay even and with an odd count one player sits out; betting souls are separate from in-game souls (start 100, 100 per kill, 50 per assist); bet everything on the next round by typing `sapphire` / `amber` or `/bet`, open between rounds until 10 s into the round, a win doubles the stake; fighters bet only on their own team, the player sitting out on either; `/reserve <hero>` (1,000, next 3 rounds), `/heroban <hero>` (1,000, one round, both teams) and `/mark <slot>` (300, kill the marked enemy fighter that round to steal a quarter of their souls); `/souls` and `/commands`
- **Notes:** not named `help` (Deadworks' `dw_help` is the console command list). The numbers come from the betting constants, so the text follows them. In mirror mode the reply is three lines instead: everyone plays the same hero with the same build, even teams with one player sitting out on an odd count, and `/commands`

### /score

- **Invocation:** chat `/score` or `!score` | console `dw_score`
- **Who:** players, in game (`GameLoopPlugin`)
- **Calls:** `MatchService.DescribeScore`
- **Mode:** Clean; read-only; not logged
- **Side effects:** one chat line, `Round 3: Sapphire 2 - 1 Amber (ties 0)`, or `No match is running.`
- **Notes:** the same score appears on screen (HUD banner) after every round

### /stats

- **Invocation:** chat `/stats` or `!stats` | console `dw_stats`
- **Who:** players, in game (`StatsPlugin`)
- **Calls:** `StatsService.Describe`
- **Mode:** Clean; read-only; not logged
- **Side effects:** two chat lines: `You (K / D / A): 3 / 1 / 2`, then `Sapphire: 10 / 8 / 12 | Amber: 8 / 10 / 9` (team totals of connected players by current team)
- **Notes:** counts from `/match_start`; deaths outside a match and hero-swap punishment deaths are not counted. The same numbers are on the Sapphire and Amber boards at the watch spot

### /bet <sapphire|amber> (or type the team name in chat)

- **Invocation:** chat `sapphire` / `amber` (the whole message, any case) | chat `/bet <team>` or `!bet <team>` | console `dw_bet <team>`
- **Who:** players, in game (`BettingPlugin`); Random mode match only
- **Calls:** `BettingService.TryBet` → `BetBook.Place`
- **Mode:** Clean; logged in `betting-*.log`
- **Side effects:** bets all your souls on that team for the next round; typing the other team before the round starts moves the bet. Players in the coming round's fight may only bet on their own team; the player sitting out may bet on either. Chat reply: `Bet 300 souls on Amber.` At round end: `You won 600 souls (now 600).`, `You lost 300 souls (now 0).`, or `No result - your 300 souls are back.` (tie, cancel, match end). The betting board updates
- **Notes:** everyone starts a match with 100 souls and earns 100 per kill and 50 per assist (added even while a bet is open). Betting opens at the start of the intermission (5 s) and closes 10 s into the round (`Betting is closed - it opens again after this round.`); with 0 souls: `No souls - get a kill or an assist.` A plain team-name chat message outside a Random match is ignored; `/bet` then replies that betting is only open during a Random mode match

### /souls

- **Invocation:** chat `/souls` or `!souls` | console `dw_souls`
- **Who:** players, in game (`BettingPlugin`)
- **Calls:** `BettingService.DescribePlayer`
- **Mode:** Clean; read-only; not logged
- **Side effects:** chat lines: `You have N souls.`, your open bet if any, your mark if any (`Your mark: X (next round).`), whether betting is open, and your hero reservation (or how to buy one with `/reserve`)

### /mark [slot]

- **Invocation:** chat `/mark` or `/mark <slot>` (also `!mark`) | console `dw_mark <slot>`. No slot: lists the enemy fighters you can mark
- **Who:** players fighting the coming (or current) round, in game (`BettingPlugin`); Random mode match only, while betting is open (the intermission until 10 s into the round)
- **Calls:** `BettingService.TryMark` → `BetBook.TrySpend`, `MarkBook.Place`; on the kill `BettingService.OnKill` → `MarkBook.TryConsume`, `BetBook.Steal`
- **Mode:** Clean; logged in `betting-*.log`
- **Side effects:** `/mark` alone: `Mark an enemy fighter for 300 souls: /mark <slot>. Kill them next round to steal a quarter of their souls. You have N.`, then one line per enemy fighter, `3 Theo (420 souls)`. `/mark <slot>` spends 300 souls (not souls riding on a bet) and marks that enemy fighter for the round: `Marked Theo for next round (120 souls left). Kill them next round to steal a quarter of their souls.` The target is told right away, without your name: `Someone marked you: if they kill you next round, they take a quarter of your souls.` If you get the kill on them that round you steal a quarter of their total souls (free souls first, then off their open bet, which shrinks its payout): `You stole 105 souls from Theo (mark).`; they get `Kamilk stole 105 souls from you - you were marked.` The betting board updates
- **Notes:** one mark per round (`You already marked Theo next round. One mark per round.`); several players may mark the same enemy, each stealing on their own kill. Only enemy fighters can be marked (`No enemy fighter in slot 5 - type /mark to see them.`). A mark pays once and lasts one round: without the kill it ends at round end (`Your mark on Theo ran out.`). The 300 souls come back if the round had no result (tie, cancel), the target didn't fight (left or became a spectator), or the match ended. Refused outside a Random match, for spectators, for the player sitting out, after betting closes, and with fewer than 300 free souls

### /reserve [hero]

- **Invocation:** chat `/reserve <hero>` or `!reserve <hero>` | console `dw_reserve <hero>`. The hero's name, any case; spaces allowed (`/reserve lady geist`). No argument: shows your reservation
- **Who:** players, in game (`RandomPlugin`); Random mode match only
- **Calls:** `RandomModeService.Reserve` → `BetBook.TrySpend`, `HeroReservations.TryReserve`
- **Mode:** Clean; logged in `random-*.log`
- **Side effects:** spends 1,000 souls (not souls riding on a bet) and puts you in line for that hero. Nobody ahead: `Reserved Haze for your next 3 rounds, starting the round after this one (250 souls left).` Someone ahead: `Someone has reserved Haze. When their 3 rounds are done, it will be your turn.` (the holder is never named) (with more ahead: `2 players are ahead of you for Haze (5 rounds). Then it will be your turn.`). From the next hero draw, the first player in the hero's line who is fighting that round gets it with a random stored build, and a chat line: `Your reserved hero is up: Haze (round 1 of 3).`, then `Reserved hero: Haze (round 2 of 3).` Everyone else is drawn randomly from the other heroes. The betting board total drops by 1,000
- **Notes:** one reservation per player (holding or waiting). Rounds you sit out don't count; if the holder sits out or is away, the next fighter in line plays the hero that round. If your reserved hero is banned (`/heroban`), that round still counts: you get a random hero and `Your reserved Haze was banned this round (round 2 of 3 used).` Refused outside a Random match, for spectators, for an unknown hero, and with fewer than 1,000 free souls. No refunds: an unused reservation ends with the match (souls reset at every match start)

### /heroban [hero]

- **Invocation:** chat `/heroban <hero>` or `!heroban <hero>` | console `dw_heroban <hero>`. The hero's name, any case; spaces allowed. No argument: shows your team's pending ban (hero and who bought it), or the price and your souls
- **Who:** players with a team, in game (`RandomPlugin`), including the player sitting out; Random mode match only
- **Calls:** `RandomModeService.Ban` → `HeroBans.TryGetPending`, `BetBook.TrySpend`, `HeroBans.TryBan`
- **Mode:** Clean; logged in `random-*.log`
- **Side effects:** spends 1,000 souls (not souls riding on a bet) and takes that hero out of the next hero draw for both teams, for one round: `Banned Haze for both teams next round (250 souls left). Everyone sees it when that round starts.` Bought during an intermission (after that round's draw), it says `the round after this one` instead. When the round starts, everyone gets the chat line `Banned this round: Haze` (or `Haze, Lash`), heroes only. A player who reserved the banned hero uses one of their 3 rounds and gets a random hero. The betting board total drops by 1,000
- **Notes:** one ban per team per round, first come: a teammate trying again gets `Your team already banned Haze (Kamilk). One ban per team per round.` and is not charged. The other team's ban stays secret until the round starts; both teams may ban the same hero (both are charged). Refused outside a Random match, for spectators, for an unknown hero, and with fewer than 1,000 free souls. No refunds; pending bans end with the match
