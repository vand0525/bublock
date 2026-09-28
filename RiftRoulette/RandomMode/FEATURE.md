# RandomMode — Feature

## Purpose

Random hero mode for the continuous match (the default
`MatchConfig.HeroMode`). No draft and no shepherding: at match start the
teams are evened out (current teams kept where possible), and every
intermission each player is swapped to a new random hero with one of that
hero's top 3 real builds (the build's items, one random pick per optional
group, bought within the soul cap, 20,000 by default, into 12 slots with
sells for pricier items once full; the level and boons a real hero has at
the cap, and the build's ability order as far as that level's unlocks and
points pay for; 0 souls and 0 unspent points). While the match runs,
ability-point and unlock gains are blocked (`GameLoop/SoulRule`), so the
ranks stay the build's. Later, heroes will come from a real match ID; only `HeroDraw`
changes then.

The mode also handles players joining mid-match (a hero right away during an
intermission), team evening (`TeamBalance.Even`, when someone left or
spectated) then auto-balance before each draw (`Balance/`), and the hero swap
guard (changing hero from the menu kills you; you respawn with your
assigned hero and full build).

The fighting teams are always even. With an odd number of players (3 or
more), one sits out each round, up top and restrained, with a `Sitting out`
banner; everyone takes turns (`BenchRule`, a `Modules/Queue` rotation). Last
round's bench player fills the gap the new one leaves. A join or leave
during an intermission subs the bench player in; mid-round the round plays
on uneven until the next intermission.

Hero reservations: a player can spend 1,000 betting souls (`/reserve
<hero>`) to play that hero in their next 3 fighting rounds
(`HeroReservations`). Several players can reserve the same hero; they wait
in line in the order they bought, and are told how many are ahead and for
how many rounds (never who). At each draw the first player in a hero's line who is
fighting that round gets it (so a benched or absent holder never stalls the
line, and keeps their rounds); everyone else is drawn randomly from the
other heroes. Rounds spent sitting out don't count. Reservations last one
match, like the souls.

Hero bans: any player with a team (bench included) can spend 1,000 betting
souls (`/heroban <hero>`) to take a hero out of the next draw for both teams
(`HeroBans`). One ban per team per round, first come; a teammate trying
again is told the team's ban and not charged. The other team's ban is
secret until the round starts, when everyone gets one chat line
(`Banned this round: Haze`, heroes only). Both teams may ban the same hero.
A ban bought in an intermission (after the draw) applies to the round after.
A ban on a reserved hero uses one of the holder's rounds; they get a random
hero and a private chat line, so bans can counter a reservation.

## Files

| File | Role |
|---|---|
| `HeroDraw.cs` | Unique random heroes, no repeat of last round; reserved (fixed) heroes honored (pure, tested) |
| `HeroReservations.cs` | Hero reservations: a waiting line per hero, rounds used per fighting round (or burned by a ban), reply text (pure, tested) |
| `HeroBans.cs` | Hero bans: one pending per team, taken into each draw, reveal text (pure, tested) |
| `BenchRule.cs` | Who sits out (rotation) and the fighting teams around them (pure, tested) |
| `RandomModeService.cs` | Match / round orchestration, joiners, pending swaps, hero guard, banners, status |
| `RandomPlugin.cs` | `player_respawned` + `player_spawn` hooks, `/reserve` and `/heroban` (player), `/random_status`, `/random_reroll` |

Team placement (`TeamBalance`) lives in `Lobby/`.

## Public operations

See `RandomModeService.md`. Player commands `/reserve` and `/heroban` in
`reference/user-commands.md`; admin commands in
`reference/admin-commands.md`.

## State

`RandomModeService` statics: teams, last heroes, current assignments, the
bench rotation and bench player, the hero reservation lines
(`Reservations`, kept by Steam ID for the match), the hero bans (`Bans`),
and a
`Lobby/HeroLock` (pending set, applied set, enforcement kills; one per DLL
load; cleared by `BeginMatch` / `EndMatch`). 1v1 mode owns its own lock.

## Dependencies

- `Modules/Loadout` (`HeroBuildCatalog`, `LoadoutService.Swap`).
- `Modules/Queue` (`PlayerQueue` bench rotation).
- `Modules/Hud` (per-player build banner: hero, build, soul value),
  `Shared` (`PlayerChat` for the hero-lock line, logging, auth).
- `Draft/DraftState` (assignments are written as picks), `Lobby/RiftRouletteTeams`,
  `Lobby/TeamBalance`.
- `Balance/BalanceService` (auto-balance), `Stats/StatsService` (board refresh).
- `Betting/BettingService` (`Book.TrySpend` for reservations and bans, `Active`,
  board refresh after a purchase).
- `GameLoop/MatchConfig`, `GameLoop/MatchService.State` (reroll / joiner gate).

## Lifecycle vs commands

- `GameLoop/MatchService` calls `BeginMatch` in `Start`, `PrepareRound` at
  the start of every intermission (not on a failed-start retry),
  `AnnounceUpcoming` at the 5 s warning, `AnnounceBans` once each round
  goes live, and `EndMatch` in `End`. These run
  in the match's mode (Debug when an admin started the match).
- `Lobby/LobbyService.AdmitPlayer` calls `AddJoiner` for players who connect
  during a Random match; `RemovePlayer` calls `OnLeave` for players who
  disconnect during one.
- `Draft/DraftService.EnforceHero` calls `DuelService.GuardHero`, then
  `GuardHero`.
- `Lobby/AdminSeat.Sit` calls `Forget`.
- `/reserve` (player, Clean) calls `Reserve`; `/souls` shows
  `DescribeReservation`. `/heroban` (player, Clean) calls `Ban`.
- `/random_reroll` calls the same `PrepareRound` in Debug mode (a reroll
  does not use up another reserved round);
  `/balance_now` calls it with `forceBalance: true`.
- In Random mode, Draft's pool boards are replaced by the stats boards and
  `/pick`, `/unpick`, `/heroes`, and `/draft_assign` reply that heroes are
  random.

## Logs

`random-YYYYMMDD.log`; per-player loadout lines in `loadout-YYYYMMDD.log`.
