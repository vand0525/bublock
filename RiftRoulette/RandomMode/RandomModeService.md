# RandomModeService

Random mode orchestration (Stage 13b, joiners and hero guard in 13c). Each
intermission, every human player gets a new random hero, plus one of that
hero's top 3 builds (`Modules/Loadout`). Teams are evened out at match start
and kept (auto-balance may swap players between rounds). Static state;
called by `GameLoop/MatchService` only when `MatchConfig.IsRandom`.

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
- `Options`: `LoadoutOptions(Level: 36, Gold: 0)` (9 slots, 20,000 cap).

## Operations

| Op | Behavior | Returns |
|---|---|---|
| `BeginMatch(mode)` | Clears state; `TeamBalance.Even` over connected humans' current teams (keeps them when already even); logs sizes and how many moved | — |
| `PrepareRound(timer, mode, forceBalance = false)` | See below | players swapped now |
| `AddJoiner(player, team, timer, mode)` | Records the joiner's team. In an intermission: draws a hero not assigned this round (any hero if none are left), assigns a random build, marks pending, and after 2 s tries `ApplyPending` as a fallback. During a round: hero at the next intermission | — |
| `ApplyPending(player, timer, mode)` | In Random mode, for a pending player now alive: starts their swap and clears pending | `bool` started |
| `TryGetAssignment(steamId, out assignment)` | Current assignment lookup | `bool` |
| `GuardHero(player, pawn, timer, mode)` | See below | `bool` handled |
| `ConsumeEnforcementKill(steamId)` | Removes and returns the enforcement-kill flag | `bool` |
| `Forget(steamId)` | Drops the player's team, assignment, and lock entries (admin seat) | — |
| `AnnounceBuilds(mode)` | Marks the build banner as sent, then shows each player whose loadout has landed: title = hero game name, description `BuildDescription(build, souls)`. Called by `MatchService` 3 s into the intermission | players shown |
| `BuildDescription(buildName, souls)` | `<build> - 12,345 souls` (invariant culture) | string |
| `EndMatch(mode)` | `ResetHero()` for alive assigned players (clears the build before the lobby reset), then clears state | heroes reset |
| `Describe()` | Config line, then one line per player: slot, name, team, hero, build, `PENDING` | lines |

### PrepareRound

1. Drops teams of players who left or took the admin seat
   (`Participants.Humans()`); players still without a team go to
   `TeamBalance.SmallerTeam`.
2. Evens the teams (private `EvenTeams`): `TeamBalance.Even(Teams)` moves
   random players from the bigger team while the two differ by 2 or more;
   logs `Teams evened Moved= Sapphire= Amber=` when anyone moved. A
   leaver or a seated admin used to leave 2v0.
3. `BalanceService.TryBalance(Teams, mode, forceBalance)` may swap players
   between teams (see `Balance/`).
4. `HeroDraw.Draw(connected, catalog.Heroes, LastHero)`.
5. Clears `Assignments`, `Values`, `Pending`, `DraftState`, and the
   build-banner flag. For each player: a
   random build of the drawn hero; records the assignment and last hero;
   `DraftState.Add` (so the disconnect release, `/status`, and `RoundFlow`
   team moves keep working).
6. `Start` per player:
   - Dead or no pawn: pending (Debug line), applied on the next spawn.
   - Alive: `ChangeTeam(team)` if needed, then `LoadoutService.Swap`
     (`SelectHero`, then after 1 s `ResetHero`, level, abilities, items).
     When applied (and still the current assignment), the player is marked
     `Applied` and the build's soul value is stored; no chat line. If the
     build banner already went out, the player's banner shows now.
7. `StatsService.RefreshBoards` (teams may have changed).

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
round prepared (players / swapped / pending), pending starts, hero swap
punishments, match end. Loadout details are in `loadout-*.log`.

## Deadworks constraints

- Never `SelectHero` or `ChangeTeam` while dead: dead players wait for a
  spawn (respawn is 1 s on this server).
- `Hurt(1_000_000f)` is the upstream `Kill()` helper's damage (the helper is
  not in our `lib/` yet); it counts as a normal death.
- Players come from `Participants.Humans()`: bots and seated admins are
  skipped.
- `DraftState` keeps heroes unique; a duplicate hero (possible only when
  the pool is smaller than the lobby) is not added to `DraftState`.
