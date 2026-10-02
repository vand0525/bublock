# RandomModeService

Random mode orchestration, joiners and hero guard. Each
intermission, every human player gets a new random hero, plus one of that
hero's top 3 builds (`Modules/Loadout`). Teams are evened out at match start
and kept (auto-balance may swap players between rounds). The fighting teams
are always even: with an odd number of players, one sits out each round in
turn (the bench). Static state; called by `GameLoop/MatchService` only when
`MatchConfig.IsRandom`.

## State

- `Teams`: Steam ID to team number (whole match).
- `LastHero`: Steam ID to the previous round's hero (no repeats).
- `Assignments`: Steam ID to `RandomAssignment(Hero, Build, Team)` for the
  current round.
- `Values`: Steam ID to the soul value of the build actually given
  (`LoadoutResult.Value`), set when the loadout lands.
- `_buildsAnnounced`: whether this intermission's build banner has gone
  out; reset in `PrepareRound`. Loadouts landing after it show their banner
  at once.
- `Lock`: a `Lobby/HeroLock` holding the pending set
  (swap waits for a spawn: dead at prepare time, joiners, players killed by
  the hero guard), the applied set (only these can be punished, so our own
  hero changes in flight never trigger the guard), and enforcement kills
  (`StatsService` consumes the entry and skips that death).
- `Options`: `LoadoutOptions(Gold: 0)` (12 slots, the current
  `LoadoutService.MaxValue` cap). The level and ability ranks follow the
  cap (`LoadoutService.Apply`).
- `BenchRotation`: a `Modules/Queue` `PlayerQueue`, next to sit out first
  (`BenchRule.Next`).
- `_benched` (public `Benched`): who sits out this round, or null.
- `FighterCount`: players with an assignment this round (bench excluded);
  `MatchService.StartRound` counts it for the join budget
  (`Lobby/MapRefreshService`).
  `_returning`: last round's bench player, placed into the gap this round.
  `_benchRound`: the `MatchState.Round` the bench was chosen for, so a
  reroll in the same intermission keeps the bench.
- `Reservations` (public): the `HeroReservations` waiting lines bought with
  betting souls (whole match; reset with the rest in `BeginMatch` /
  `EndMatch`, like the souls at match start). Kept by Steam ID across a
  disconnect.
- `Bans` (public): the `HeroBans` bought with betting souls, one pending
  per team, taken into the draw each intermission; `Current` stays in
  force through the round (late heroes, reveal). Reset with the rest.

## Operations

| Op | Behavior | Returns |
|---|---|---|
| `BeginMatch(mode)` | Clears state; `TeamBalance.Even` over connected humans' current teams (keeps them when already even); logs sizes and how many moved | — |
| `PrepareRound(timer, mode, forceBalance = false)` | See below | players swapped now |
| `AddJoiner(player, team, timer, mode)` | Records the joiner's team and adds them to the back of the bench rotation. During a round: hero at the next intermission. In an intermission, keeping the fighters even: an odd number of fighters: the joiner plays on the smaller fighting team; else a bench player is connected: both play (bench player on the other team, swapped now; logged `Subbed in with a joiner`); else the joiner sits out (`Joiner sitting out this round`, sitting-out banner if the build banner already went out). A late hero (`AssignLate`): a priority hero with builds that nobody holds this round (its holder left), with no reserved round used; else the player's reserved hero when `Reservations.TakeLate` allows it (nobody plays it this round and it is not in `Bans.Current`; counts one of their rounds, chat `TurnLine`), else an unbanned hero not assigned this round (any unbanned hero if none are left); a random build, pending, `ApplyPending` fallback after 2 s | — |
| `OnLeave(steamId, timer, mode)` | From `LobbyService.RemovePlayer` during a Random match, before the pawn is removed. `Forget`s the leaver. If they were fighting and it is an intermission and a bench player is connected: the bench player takes the leaver's team with a late hero, swapped now (`Subbed in for a player who left`). Mid-round the round plays on uneven; the next intermission evens it | — |
| `ApplyPending(player, timer, mode)` | In Random mode, for a pending player now alive: starts their swap and clears pending | `bool` started |
| `OnSwapFailed(player, failed, timer, mode)` (private) | `swapFailed` callback of every Random `LoadoutService.Swap` (after 3 tries the hero never changed). Only for a priority hero, while that assignment is still the player's and a match runs: adds the hero to `FailedPriority` (left out of `Priority()`, so out of every draw and `AssignLate`, until the next `BeginMatch`), draws a new hero from `PriorityHeroes.RerollPool(unbanned pool, FailedPriority, heroes other fighters hold)` (`HeroDraw.Draw`, a random build via `Assign`), logs Warning `Priority hero did not spawn, rerolled Failed= Hero=`, and starts that swap (pending when the player is dead) | — |
| `TryGetAssignment(steamId, out assignment)` | Current assignment lookup | `bool` |
| `GuardHero(player, pawn, timer, mode)` | See below | `bool` handled |
| `ConsumeEnforcementKill(steamId)` | Removes and returns the enforcement-kill flag | `bool` |
| `Forget(steamId)` | Drops the player's team, assignment, lock and bench rotation entries; clears the bench if it was them (admin seat, leave) | — |
| `AnnounceBuilds(mode)` | Marks the build banner as sent, then shows each player whose loadout has landed: title = hero game name, description `BuildDescription(build, souls)`. The bench player gets `SitOutTitle` / `SitOutDescription` (`Sitting out` / `You play next round`). Called by `MatchService` 3 s into the intermission | players shown |
| `BuildDescription(buildName, souls)` | `<build> - 12,345 souls` (invariant culture) | string |
| `EndMatch(mode)` | `ResetHero()` for alive assigned players (clears the build before the lobby reset), then clears state | heroes reset |
| `Describe()` | Config line with `Bench=`, a `Bans: this round=<heroes or none> \| pending Sapphire=<hero (buyer)> Amber=...` line, a `Priority=<hero (in pool \| no builds \| swap failed, off this match), ...>` line (`none` when the list is empty), then one line per player: slot, name, team, hero, build, `PENDING`, `SITTING OUT`, and `Reserved=<hero> (N left)` (front of a line) or `Waiting=<hero> #N` | lines |
| `Reserve(player, heroText, mode)` | `/reserve`. Refuses outside a running Random match (`BettingService.Active`) and for spectators. Empty text: `DescribeReservation`. Parses the hero (`TryParseHero`, also with spaces removed; must have stored builds); refuses a priority hero (`PriorityHeroes.RefusedLine`: `<hero> is a priority hero: someone gets it every round, so it can't be reserved.`, not charged); refuses a player who already holds or waits for one; spends `HeroReservations.Cost` (1,000) unstaked souls (`BetBook.TrySpend`; short: the price, their souls, and a note when souls are on a bet); joins the hero's line; logs `Hero reserved Hero= Result= Ahead= RoundsAhead= Chips=`; refreshes the betting board. Reply: `Reserved <hero> for your next 3 rounds, starting the round after this one / the next round you play (N souls left).`, or `HeroReservations.WaitingLine` (`Someone has reserved <hero>...`; the holder is never named) | reply |
| `DescribeReservation(player)` | No reservation: price, usage, and their souls. Front of the line: hero and rounds left. Waiting: place, rounds ahead, own rounds | reply |
| `Ban(player, heroText, mode)` | `/heroban`. Refuses outside a running Random match (`BettingService.Active`), for spectators, and for a player with no entry in `Teams` (bench players have one). Empty text: the team's pending ban and buyer, or the price, usage and their souls. Parses the hero like `Reserve`; a priority hero is refused (`... so it can't be banned.`, not charged). A team that already has a pending ban: `Your team already banned <hero> (<buyer>). One ban per team per round.`, not charged. Spends `HeroBans.Cost` (1,000) unstaked souls (short: the price, their souls, a note when souls are on a bet); `Bans.TryBan`; logs `Hero banned Hero= Team= Chips=`; refreshes the betting board. Reply: `Banned <hero> for both teams next round / the round after this one (bought in an intermission, after the draw) (N souls left). Everyone sees it when that round starts.` The other team's ban is never checked or shown | reply |
| `AnnounceBans(mode)` | From `MatchService.StartRound` in Random mode, after the round goes live. When `Bans.Current` is not empty: one chat line to every connected player, `HeroBans.RevealLine` (`Banned this round: Haze, Lash`, sorted names, never who banned); logs `Bans revealed Heroes= Told=`. Chat only, never a banner | players told |

### PrepareRound

1. Drops teams of players who left or took the admin seat
   (`Participants.Humans()`); players still without a team go to
   `TeamBalance.SmallerTeam`.
2. Bench: on a new intermission (`_benchRound` differs from
   `MatchState.Round`), the old bench player becomes `_returning` and
   `BenchRule.Next` picks this round's (odd count of 3+: the front of the
   rotation, who then goes to the back). A reroll in the same intermission
   keeps the bench. Logs `Sitting out this round Players=`.
3. Fighting teams: `BenchRule.FightingTeams(Teams, bench, returning)`:
   without the bench player, the returner unassigned (fills the side the
   bench player left), then `TeamBalance.Even`. Logs `Teams evened Moved=
   Sapphire= Amber=` when anyone moved.
4. `BalanceService.TryBalance(fighters, mode, forceBalance)` may swap
   players between teams (see `Balance/`; with equal teams it swaps a pair,
   so the teams stay even). The fighters' teams are written back to
   `Teams`; the bench player keeps their old entry.
5. `Bans.Take(MatchState.Round)`: the pending team bans become this
   round's bans. `Reservations.Take(fighters, MatchState.Round, bans)`: for
   each reserved hero, the first fighter in its line; on a banned hero that
   fighter uses one round without getting it (`Reservations.Burned`). A
   reroll in the same intermission reuses both results without counting
   again. Then `HeroDraw.Draw(fighters, unbanned heroes, LastHero,
   fixedHeroes, priority)`: reserved fighters get their hero, then each
   priority hero with builds (`PriorityHeroes.InPool`; a listed hero with
   no builds logs Warning `Priority hero has no builds, skipped Hero=` once
   per load; a hero whose swap failed this match is left out, see
   `OnSwapFailed`) goes to a random other fighter, and everyone else is drawn
   randomly from the rest, never a banned hero (unless the bans cover the
   whole pool, then they are ignored). Priority heroes can't be banned or
   reserved, so neither step can take one out. The bench player
   gets no hero, no assignment and no `RoundHeroes` entry, so `RoundFlow`
   leaves them up top, restrained, for the round.
6. Clears `Assignments`, `Values`, `Pending`, `RoundHeroes`, and the
   build-banner flag. For each fighter: a
   random build of the drawn hero; records the assignment and last hero;
   `RoundHeroes.Set` (so the disconnect cleanup, `/status`, and `RoundFlow`
   team moves keep working).
7. `Start` per fighter:
   - Dead or no pawn: pending (Debug line), applied on the next spawn.
   - Alive: `ChangeTeam(team)` if needed, then `LoadoutService.Swap`
     (`SelectHero`, then after 1 s `ResetHero`, level, abilities, items).
     When applied (and still the current assignment), the player is marked
     `Applied` and the build's soul value is stored; no chat line. If the
     build banner already went out, the player's banner shows now.
8. Each reserved fighter gets one chat line, `HeroReservations.TurnLine`
   (`Your reserved hero is up: Haze (round 1 of 3).` /
   `Reserved hero: Haze (round 2 of 3).`), logged `Reserved hero used`.
   Each burned reservation's owner gets `HeroReservations.BurnedLine`
   (`Your reserved Haze was banned this round (round 2 of 3 used).`), logged
   `Reserved hero banned, round used`. `Round prepared` logs `Reserved=`
   (turns this round), `Banned=` and `Burned=`.
9. `StatsService.RefreshBoards` (teams may have changed).

### GuardHero (hero swap guard)

Called from `Lobby/LobbyHeroes.Enforce` on `player_hero_changed`. The kill
itself is `HeroLock.Enforce`.

- Not Random mode, or no assignment: returns false (Mirror's guard, then
  the lobby hero rule, runs).
- Right hero, not yet applied, pending, or dead: returns true, does nothing
  (the running swap or the pending respawn fixes the hero).
- Otherwise (the player changed hero from the menu after their loadout was
  applied): marks pending, flags an enforcement kill, `pawn.Hurt(1_000_000f)`,
  logs, and tells the player they respawn as their hero. On respawn,
  `ApplyPending` swaps them back and applies the full build. If the pawn
  survives the damage, the flags are undone and the swap runs in place.

## Logs

`Random` feature log (`random-YYYYMMDD.log`): teams, late joiners, joiners,
sitting out and subbed in, round prepared (players / swapped / pending /
reserved / banned / burned), hero reservations bought, used and banned,
hero bans bought and revealed, pending starts, hero swap
punishments, match end. Loadout details are in `loadout-*.log`.

## Deadworks constraints

- Never `SelectHero` or `ChangeTeam` while dead: dead players wait for a
  spawn (respawn is 1 s on this server).
- `Hurt(1_000_000f)` is the upstream `Kill()` helper's damage (the helper is
  not in our `lib/` yet); it counts as a normal death.
- Players come from `Participants.Humans()`: bots and seated admins are
  skipped.
- `RoundHeroes` allows a duplicate hero (possible only when the pool is
  smaller than the lobby); every fighter gets an entry.
