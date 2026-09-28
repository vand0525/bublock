# GunGameService

The whole Gun Game game type as a composition of engine modules; the
plugin classes only forward hooks here. Static, one per DLL load.

## State

| Member | What |
|---|---|
| `Arena` | `ArenaSpots.Load(assembly, "GunGame.arena.json")`, lazy |
| `Heroes` | `RandomLoadouts(HeroBuildCatalog.Default, LoadoutOptions(Gold: 0), Random.Shared)`; `Landed` shows the hero banner |
| `Session` | `TimedSession("Gun Game", GunGameRules.Session, playing humans)`; callbacks show the start, 10 s, result banners and the waiting chat line |
| names | Steam ID → name for results and status |

## Operations

| Op | Behavior |
|---|---|
| `ApplyServerConvars(mode)` | `citadel_team_size 6`, `citadel_allow_duplicate_heroes 1`, `citadel_koth_enabled 0` (no rift), `citadel_allow_purchasing_anywhere 0`, `citadel_player_override_spawn_time 1`, and the arena's `citadel_active_lane` (4); logs |
| `Admit(player, timer, mode)` | Smaller team (`DeadlockTeams.SmallerTeam` over the other humans), `Heroes.Draw`, `SelectHero(hero)`, `ChangeTeam(team, true)`, `Heroes.Hold` (build waits for the spawn); after 2 s `ApplyPending` as a fallback and `Session.Check`; a waiting chat line if no match runs |
| `AdmitConnected(timer, mode)` | After a load: every human gets a playable team if needed and `Heroes.Roll` |
| `Remove(player, timer, mode)` | Forgets their pick and points; `Session.Check` with the count without them |
| `BlocksCommand(player, command)` | Humans only: `ChoiceGuard.Blocks(command, heroLocked: has a pick, teamLocked: true)` |
| `BlocksCurrency(type, source, amount)` | `SoulRule.ShouldBlock(..., active: true)` |
| `OnSpawn(player, timer)` | `ArenaService.SendToArenaNextTick`; a pending human gets `ApplyPending` on the next tick |
| `ContainPlayers(mode)` | `ArenaService.ReturnStrays` over players on playable teams |
| `OnDeath(args, timer, mode)` | Only while a match plays; human attacker; `GunGameRules.Credits`; `Session.Scores.Add`; log `Kill Kills= Victim= Match=`; `Heroes.Roll(attacker)` (the banner comes when the build lands: `3 kills: <hero>` / build and souls) |
| `Reroll(player, timer, mode)` | Admin `Heroes.Roll`; reply text |
| `DescribeFor(player)` / `Describe()` | `/points` lines / `/gg_status` lines |
| `Humans()`, `NameOf(steamId)` | Non-bot players; a name for a Steam ID |

## Invariants

- No rift, no rounds: `citadel_koth_enabled 0`, and nothing in Gun Game
  touches KOTH.
- Only the game gives heroes: client `selecthero` is refused for players
  with a pick, `changeteam` / `jointeam` always.
- Points only while `Session.IsPlaying`.

## Logs

`gungame-YYYYMMDD.log` (admits, kills, final standings, convars).
