# PitRoundService

The pit round state machine (attack / defense around the mid-boss pit).
Static state, created on first use; timers come from the calling plugin
(`ITimer`). Logs to `pit-*.log`; round lines also go to the master log.

## Round flow

| Phase | Entered by | What happens |
|---|---|---|
| `Countdown` | `Start(chosenPitTeam, timer, mode)` | Fighters = every non-bot on team 2 or 3. The pit team is `PitRoundRule.PitTeam` (admin choice, else alternates, Amber first). Unique random heroes (`HeroDraw.Draw`, last round's hero avoided) and a random stored build each (`HeroBuildCatalog.BuildsFor`). Everyone is restrained and sent to their own spot up top (`PitSpots.Watch(slot)`), then swapped (`LoadoutService.Swap`, `Options` gold 0, at `LoadoutService.MaxValue`). A dead player's swap waits for their respawn (`Pending`). Banner to all: `Round N` / `<Team> starts in the pit`. Live after `CountdownSeconds` (3) |
| `Live` | countdown timer (`GoLive`) | `MidBossService.Ready(cap, fighters)` (fighters at least 1), its `Recheck` 1 s later. Each placed fighter (not pending) is released, has rejuv cleared, is teleported to their start spot (pit team `PitSpots.Pit(team, slot)`, other team `PitSpots.Lane(team, slot)`), healed to full and has every cooldown ended. Per-player banner: pit team `Kill the mid-boss` / `Hold the pit and take its crystal`, lane team `Steal the crystal` / `Get into the pit and take the crystal`. Time limit timer (`LimitSeconds`) |
| `BossDown` | `OnBossKilled(team)` (from `MidBossService.BossKilled`, only while `Live`) | Killer team stored, limit cancelled, banner `<Team> killed the boss` / `Grab the crystal`, crystal window timer (`WindowSeconds`) |
| `Idle` | `End(reason, mode)` | See below |

**Crystal.** The first team to take a crystal during `BossDown` is the
crystal team, from either `OnCrystalTaken(team)` (`MidBossService.CrystalTaken`,
the game's rejuv count rising) or `WatchCredit` (every second: a living
fighter holding `modifier_citadel_rebirth_credit`; covers a count that no
longer rises). Banner `<Team> took the crystal`; master line with
`Source=count|credit`.

**End.** Cancels every timer; winner from `PitRoundRule.Winner` (only a
finished window has one: crystal team, else killer team) and the score
goes up. Removes every boss and crystal (`RemoveBosses` /
`RemoveCrystals`, never a kill), clears the rebirth credit and rejuv
buff from every pawn (`ClearRejuv`), sends living team players up top
restrained and restrains the dead (their respawn sends them up). Banner
`<Team> wins round N` / `Took the crystal|Killed the boss | Sapphire N - Amber M`,
or `Round N: no winner` / `PitRoundRule.EndLine | score`. `End` while
`Idle` throws `CommandException`.

## Deaths and respawns

- `OnDeath(player, pawn)`: for a fighter in `Live` / `BossDown` not yet
  out, records the death and whether it is a rebirth: the pawn has the
  rebirth credit at death, or `WatchCredit` saw the player holding it
  this round (either way, so the order of the game's death and rebirth
  never matters). Logs both flags and the death position.
- `OnSpawn(player, timer)`: `PitRoundRule.OnRespawn` decides, on the next
  tick: `SendUp` (restrain + watch spot; a fighter who died during the
  fight is out for the round, gets `You're out` / `Back next round`),
  `KeepIn` (reborn: left where the game put them, teleported back to
  their start spot only when farther than 3000 from the pit; the
  position and seconds dead are logged), or nothing. A pending fighter's
  swap starts on the next tick too.
- A swap that lands calls `Place`: in a live round and not out, the
  fighter is released and placed at their start spot; otherwise up top.

## Other operations

| Op | Result |
|---|---|
| `SetTiming(window, limit, mode)` | Sets either or both (null keeps it); applies to the next timer started. Returns the line |
| `ResetForMap()` | Map start: timers cancelled, round dropped, phase `Idle`; round number, score and last pit team kept |
| `Forget(steamId)` | Disconnect: drops pending swap, credit and death records (the fighter entry stays for the status) |
| `Describe()` | Phase, round, pit team, score, time left in the phase; timing, cap, killer and crystal team; per team in / out counts; one line per human; the first two `MidBossService.Describe` lines |
| `IsFighter(steamId)` | In the current round |

## State

Fighters (hero, build, team), last hero per player, pending swaps, out
set, credit seen, deaths, wins per team, round number, pit team and last
pit team, window and limit seconds, the four timer handles. A hot reload
starts empty (`Idle`, round 0).

## Invariants

- Never undoes the vanilla rebirth: a fighter who died with the credit
  stays in.
- Earned souls are blocked outside `Idle` (`PitRoundPlugin.OnModifyCurrency`).
- Nothing about the round carries over: bosses, crystals, rebirth and
  rejuv buff are removed at the end.

## Dangerous constraints

- `SelectHero` (inside `LoadoutService.Swap`) needs a living pawn; dead
  players wait in `Pending`.
- A hero swap fires `player_spawn` without a death; only a recorded
  death can put a fighter out.
- Teleports on spawn run on the next tick (the game places the pawn
  after the event).
