# MirrorModeService

Mirror mode orchestration. Each intermission one hero and one of its stored
builds are resolved once (`MirrorPick.Resolve`) and given to every fighter,
so heroes and builds always match. Admin pins fix the hero and, after it,
the build slot. Teams, the odd-player bench and auto-balance work like
Random mode. Static state; called by `GameLoop/MatchService` only when
`MatchConfig.IsMirror`.

## State

- `Pins` (public): `MirrorPins` from `/mirror_hero` / `/mirror_build`.
  Never reset by a match, a mode change or a map reload (plugin statics
  survive a map change); a DLL load or a clear command resets it.
- `Teams`: Steam ID to team number (whole match).
- `Fighters`: Steam IDs fighting this round (bench excluded); `FighterCount`
  feeds the join budget (`Lobby/MapRefreshService`).
- `_current` (public `Current`): the round's `MirrorChoice`. `_lastHero`:
  the previous shared hero (an unpinned roll avoids it).
- `_itemOrder`: the shared build's buy order
  (`LoadoutPlanner.ItemOrder(build, Random.Shared)`, one random item per
  optional group), drawn once per choice and passed to every fighter's
  loadout (`LoadoutOptions.ItemOrder`). Dropped when a new choice is
  rolled and on match end.
- `Values`: Steam ID to the soul value of the build given
  (`LoadoutResult.Value`), for the banner.
- `Lock`: its own `Lobby/HeroLock` (pending, applied, enforcement kills).
- `BenchRotation`, `_benched`, `_returning`, `_benchRound`: as in
  `RandomModeService` (`BenchRule`).
- `_buildsAnnounced`: the build banner went out this intermission.
- `_version`: bumped on every assignment pass; a loadout callback from an
  older pass is ignored.

## Operations

| Op | Behavior | Returns |
|---|---|---|
| `BeginMatch(mode)` | Clears match state (pins stay); `TeamBalance.Even` over connected humans' teams | — |
| `PrepareRound(timer, mode)` | Prunes teams to `Participants.Humans()`, places late joiners on the smaller team, picks the bench on a new intermission (`BenchRule.Next`), fighting teams (`BenchRule.FightingTeams`), `BalanceService.TryBalance`; resets `Fighters` and `DraftState`, then `ApplyChoice(roll: true)` | players swapped now |
| `ApplyChoice` (private) | Rolls a new `MirrorChoice` when asked, when there is none, or when it no longer matches the pins; else keeps the current one (and its item order). Draws `_itemOrder` when there is none. Then for every connected fighter: `DraftState` pick of the shared hero (so `RoundFlow` moves them in), `LoadoutService.Swap(hero, that one build, RandomModeService.Options with { ItemOrder = _itemOrder })` when alive, else pending. Logs `Round prepared Hero= Build= BuildId= Slot= Pins= Fighters= Swapped= Pending= ItemOrder=`; refreshes the stats boards | swapped |
| `PinHero(text, timer, mode)` | Empty: `DescribePins`. `clear`: drops both pins. Else parses the hero (`TryParseHero`, also without spaces; must have stored builds) and `Pins.PinHero` (keeps the slot when the hero has it). Then `AfterPinChange` | reply |
| `PinBuild(text, timer, mode)` | Refused with no hero pinned. Empty: the pinned hero's numbered builds. `clear`: drops the slot. Else `Pins.TryPinSlot` (1 to the hero's build count). Then `AfterPinChange` | reply |
| `AfterPinChange` (private) | Not mirror mode: saved. No match: used at match start. In a round: used next intermission. In a mirror intermission: `ApplyChoice(roll: false)` now (same fighters and bench; banners re-show as loadouts land) | reply suffix |
| `AddJoiner(player, team, timer, mode)` | Like Random: during a round (or before the first choice) the hero comes next intermission; in an intermission the joiner fills an odd gap, subs in with the bench player, or sits out. A late fighter gets the current shared hero and build, pending, `ApplyPending` fallback after 2 s | — |
| `OnLeave(steamId, timer, mode)` | `Forget`s the leaver; in an intermission a leaving fighter's team goes to the bench player, who gets the shared pair now | — |
| `ApplyPending(player, timer, mode)` | Mirror mode, pending fighter, now alive: starts the swap and clears pending | `bool` |
| `GuardHero(player, pawn, timer, mode)` | Mirror mode fighter: `HeroLock.Enforce` against the shared hero (menu hero change kills; respawn restores). Else false | `bool` handled |
| `ConsumeEnforcementKill(steamId)` | Removes and returns the enforcement-kill flag (`StatsService` skips that death) | `bool` |
| `Forget(steamId)` | Drops team, fighter, value, lock and bench entries | — |
| `AnnounceBuilds(mode)` | Marks the banner sent; each fighter whose loadout landed sees hero name / `RandomModeService.BuildDescription(build, souls)`; the bench player sees `Sitting out` | players shown |
| `EndMatch(mode)` | `ResetHero()` for alive fighters, then clears match state (pins stay) | heroes reset |
| `DescribePins()` | `Mirror hero: random each round. Build: random each round.` or the pinned hero and its build pin | string |
| `Describe()` | Config / fighters / pending / bench line, pins, current pair (`Build N=<name> (<id>)`), the pinned hero's numbered builds, then one line per player | lines |

## Invariants

- One `MirrorChoice` per assignment pass; every fighter gets that exact hero
  and `HeroBuild`. A build is never rolled per player.
- Every fighter's loadout shops the same `_itemOrder` (late joiners,
  pending spawns and hero-lock restores included), so the items match
  exactly; the optional-group pick is never drawn per player.
- A pinned hero is never replaced by a roll (unless it has no stored
  builds, which `PinHero` refuses; logged as a Warning). A pinned slot is
  never rolled.
- The bench player gets no hero and no `DraftState` pick, so `RoundFlow`
  leaves them up top, restrained.

## Logs

`Mirror` feature log (`mirror-YYYYMMDD.log`): teams, late joiners, bench,
round prepared, pins set and cleared, pending starts, match end. Loadout
details are in `loadout-*.log`.

## Deadworks constraints

- Never `SelectHero` or `ChangeTeam` while dead: dead fighters wait for a
  spawn.
- Several players on one hero needs `citadel_enable_duplicate_heroes`,
  which `LobbyService.ApplyServerConvars` turns on at startup.
- `DraftState` keeps one hero entry for every pick of it; releasing one
  fighter drops the hero from `SelectedHeroes` (only the draft boards read
  it, and they are off in mirror mode).
