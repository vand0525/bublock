# RandomModeService

Random mode orchestration (Stage 13b, joiners and hero guard in 13c). Each
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
- `Lock`: a `Lobby/HeroLock` (Stage 13g extraction) holding the pending set
  (swap waits for a spawn: dead at prepare time, joiners, players killed by
  the hero guard), the applied set (only these can be punished, so our own
  hero changes in flight never trigger the guard), and enforcement kills
  (`StatsService` consumes the entry and skips that death).
- `Options`: `LoadoutOptions(Gold: 0)` (9 slots, 20,000 cap). The level and
  ability ranks follow the item value (`LoadoutService.Apply`).
- `BenchRotation`: a `Modules/Queue` `PlayerQueue`, next to sit out first
  (`BenchRule.Next`).
- `_benched` (public `Benched`): who sits out this round, or null.
  `_returning`: last round's bench player, placed into the gap this round.
  `_benchRound`: the `MatchState.Round` the bench was chosen for, so a
  reroll in the same intermission keeps the bench.

## Operations

| Op | Behavior | Returns |
|---|---|---|
| `BeginMatch(mode)` | Clears state; `TeamBalance.Even` over connected humans' current teams (keeps them when already even); logs sizes and how many moved | — |
| `PrepareRound(timer, mode, forceBalance = false)` | See below | players swapped now |
| `AddJoiner(player, team, timer, mode)` | Records the joiner's team and adds them to the back of the bench rotation. During a round: hero at the next intermission. In an intermission, keeping the fighters even: an odd number of fighters: the joiner plays on the smaller fighting team; else a bench player is connected: both play (bench player on the other team, swapped now; logged `Subbed in with a joiner`); else the joiner sits out (`Joiner sitting out this round`, sitting-out banner if the build banner already went out). A late hero (`AssignLate`): a hero not assigned this round (any hero if none are left), a random build, pending, `ApplyPending` fallback after 2 s | — |
| `OnLeave(steamId, timer, mode)` | From `LobbyService.RemovePlayer` during a Random match, before the pawn is removed. `Forget`s the leaver. If they were fighting and it is an intermission and a bench player is connected: the bench player takes the leaver's team with a late hero, swapped now (`Subbed in for a player who left`). Mid-round the round plays on uneven; the next intermission evens it | — |
| `ApplyPending(player, timer, mode)` | In Random mode, for a pending player now alive: starts their swap and clears pending | `bool` started |
| `TryGetAssignment(steamId, out assignment)` | Current assignment lookup | `bool` |
| `GuardHero(player, pawn, timer, mode)` | See below | `bool` handled |
| `ConsumeEnforcementKill(steamId)` | Removes and returns the enforcement-kill flag | `bool` |
| `Forget(steamId)` | Drops the player's team, assignment, lock and bench rotation entries; clears the bench if it was them (admin seat, leave) | — |
| `AnnounceBuilds(mode)` | Marks the build banner as sent, then shows each player whose loadout has landed: title = hero game name, description `BuildDescription(build, souls)`. The bench player gets `SitOutTitle` / `SitOutDescription` (`Sitting out` / `You play next round`). Called by `MatchService` 3 s into the intermission | players shown |
| `BuildDescription(buildName, souls)` | `<build> - 12,345 souls` (invariant culture) | string |
| `EndMatch(mode)` | `ResetHero()` for alive assigned players (clears the build before the lobby reset), then clears state | heroes reset |
| `Describe()` | Config line with `Bench=`, then one line per player: slot, name, team, hero, build, `PENDING`, `SITTING OUT` | lines |

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
5. `HeroDraw.Draw(fighters, catalog.Heroes, LastHero)`: the bench player
   gets no hero, no assignment and no `DraftState` pick, so `RoundFlow`
   leaves them up top, restrained, for the round.
6. Clears `Assignments`, `Values`, `Pending`, `DraftState`, and the
   build-banner flag. For each fighter: a
   random build of the drawn hero; records the assignment and last hero;
   `DraftState.Add` (so the disconnect release, `/status`, and `RoundFlow`
   team moves keep working).
7. `Start` per fighter:
   - Dead or no pawn: pending (Debug line), applied on the next spawn.
   - Alive: `ChangeTeam(team)` if needed, then `LoadoutService.Swap`
     (`SelectHero`, then after 1 s `ResetHero`, level, abilities, items).
     When applied (and still the current assignment), the player is marked
     `Applied` and the build's soul value is stored; no chat line. If the
     build banner already went out, the player's banner shows now.
8. `StatsService.RefreshBoards` (teams may have changed).

### GuardHero (hero swap guard)

Called from `DraftService.EnforceHero` on `player_hero_changed` (after
`DuelService.GuardHero`, which only handles 1v1 mode). The kill itself is
`HeroLock.Enforce`.

- Not Random mode, or no assignment: returns false (Draft enforcement runs).
- Right hero, not yet applied, pending, or dead: returns true, does nothing
  (the running swap or the pending respawn fixes the hero).
- Otherwise (the player changed hero from the menu after their loadout was
  applied): marks pending, flags an enforcement kill, `pawn.Hurt(1_000_000f)`,
  logs, and tells the player they respawn as their hero. On respawn,
  `ApplyPending` swaps them back and applies the full build. If the pawn
  survives the damage, the flags are undone and the swap runs in place.

## Logs

`Random` feature log (`random-YYYYMMDD.log`): teams, late joiners, joiners,
sitting out and subbed in, round prepared (players / swapped / pending),
pending starts, hero swap punishments, match end. Loadout details are in `loadout-*.log`.

## Deadworks constraints

- Never `SelectHero` or `ChangeTeam` while dead: dead players wait for a
  spawn (respawn is 1 s on this server).
- `Hurt(1_000_000f)` is the upstream `Kill()` helper's damage (the helper is
  not in our `lib/` yet); it counts as a normal death.
- Players come from `Participants.Humans()`: bots and seated admins are
  skipped.
- `DraftState` keeps heroes unique; a duplicate hero (possible only when
  the pool is smaller than the lobby) is not added to `DraftState`.
