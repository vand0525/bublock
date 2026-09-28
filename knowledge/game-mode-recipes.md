---
id: game-mode-recipes
type: recipes
tags: [game-modes, design, composition]
related:
  - effects-catalog.md
  - mental-models/rift-roulette-architecture.md
  - mental-models/talking-to-the-game.md
---

# Game-mode recipes

A game mode is a loop (when does a round start and end), a setup (who plays
what, where), rules (what is blocked or boosted) and feedback (what players
see). Each recipe lists the levers from [effects-catalog.md](effects-catalog.md)
and marks what is new. The first two are built; the rest are designs to
pick up as their own stage in `master-plan.md`.

## Shipped

### Rift Roulette (random mode)

- **Loop:** `MatchService` intermission → round → score, forever; auto-start at 2 humans.
- **Setup:** random hero + real top build each intermission (`Modules/Loadout`); even teams (`BenchRule`, `Balance`).
- **Rules:** souls blocked (`SoulRule`); players up top are restrained and immune (`Modules/Restraint`, `OnTakeDamage`).
- **Objective:** a forced rift per round (`RiftGameRules`); capture = troopers spawn for the winner.
- **Feedback:** banners, stats boards, betting chips in chat.

### 1v1 winner stays on (duel mode)

- **Loop:** same match loop; `PlayerQueue` picks the two fighters.
- **Setup:** one player's exact build copied to both (`LoadoutSnapshot`), locked (`HeroLock`).
- **Reuse lesson:** a new "wait your turn" feature reuses `Modules/Queue`.

## In progress

### Gun Game

Its own game type, `GunGame.dll` (Stage G1 in
`RiftRoulette/reference/master-plan.md`; `GunGame/FEATURE.md`). No rounds,
no rift: a contained mid-lane brawl, continuous 2-minute matches, and every
kill swaps the killer to a new random hero with a real top build.

- **Loop:** `TimedSession` (Session module): from 2 players, 2-minute
  matches, most kills wins, 5 s result break, next match.
- **Setup:** smaller team on join (`DeadlockTeams`), a random hero and
  build (`RandomLoadouts`), spawns on the team's street in the middle lane
  (`ArenaSpots`, `Data/arena.json`), strays sent back every second.
- **Rules:** an enemy kill by a player scores (`GunGameRules.Credits`) and
  rerolls the killer (`RandomLoadouts.Roll` → `LoadoutService.Swap`); menu
  hero picks and team changes refused (`ChoiceGuard`); earned souls blocked
  (`SoulRule`).
- **Feedback:** `N kills: <hero>` / build and souls banner, `10 seconds
  left`, the winner banner, `/points`.
- **Levers:** `player_death` (verified), `SelectHero` on a living pawn
  mid-fight (verified in the owner's playtest, 2026-09-28), the loadout
  path (verified), `citadel_active_lane 4` (untested effect).
- **Open issues:** souls reset on death, the score should show as
  cumulative souls ([#2](https://github.com/scho0124/bublock_redock/issues/2));
  one spot per slot per team invites spawn camping, several random spots
  in the arena ([#3](https://github.com/scho0124/bublock_redock/issues/3)).
- **Next:** a fixed hero ladder option, weaker builds higher up, a world
  scoreboard (`WorldText`).

## Designs (not built)

### Grifball

Reference: [Grifball on Halopedia](https://www.halopedia.org/Grifball).
Two small teams on an enclosed court fight melee-only for one ball; carrying
it into the enemy goal scores. One hit kills, respawns are fast and at your
own goal, and the carrier glows and is tougher and faster.

- **Court:** the skybox floor Theo already uses up top (z 1536, flat,
  verified by `check-spots.py`), fenced with a WatchGuard-style boundary
  check (verified pattern) or `Utils.Zone` (untested). Goals are two zones
  at the ends.
- **Setup:** everyone on one melee-strong hero with a fixed build
  (`SelectHero` + `Modules/Loadout`, verified; `HeroLock` keeps it).
  Guns off with `EModifierState.ShootingDisabled`, abilities and items off
  with `Silenced` / `ItemsDisabled` (all verified in Restraint), melee left
  on. `/status_add <slot> <state>` tries each state by hand today.
- **Rules:** one-hit kills by turning any melee hit into a kill in
  `OnTakeDamage` (`Hurt` is verified; changing damage in the hook is
  untested). Respawn in about 3 s (`citadel_player_override_spawn_time`,
  verified as a convar we set) at your own goal via `player_spawn` →
  per-slot spots (verified).
- **Ball:** a position plus a carrier. Pick up by proximity in
  `OnGameFrame` (verified hook); drop on `player_death` (verified); score
  when the carrier enters the enemy goal. Show it with a world-text marker
  following the carrier (verified boards) or a particle attached to the
  pawn (untested).
- **Carrier buffs:** a bigger build or health on pickup (verified
  levers); speed and damage resistance states (untested).
- **Feedback:** score banner per goal, round timer (3 min) on the boards.
- **First thing to prove:** can a pawn with only melee enabled still
  melee, and does `Hurt` from `OnTakeDamage` kill cleanly without
  crediting the wrong player?

### Fat Boy / Zombie Escape

Reference: [Zombie Escape on the CSO wiki](https://cso.fandom.com/wiki/Zombie_Escape)
(the owner's "fat boy zombie mode" from Counter-Strike Source). One
random player starts as a huge, tanky "origin" zombie; zombies convert
humans on hit; humans run a route to an escape point and must hold it
before the zombies arrive or everyone is converted.

- **Teams:** humans on Sapphire, zombies on Amber (`ChangeTeam`,
  verified). Keep teams unbalanced on purpose: turn auto-balance off for
  this mode (`Balance/`).
- **Origin zombie ("fat boy"):** a tank hero with a very high-soul build
  (`Modules/Loadout`, verified), guns and items off (verified states),
  melee only. More health states (`Unkillable`, damage resistance) are
  untested.
- **Infection:** a zombie's melee hit on a human in `OnTakeDamage` kills
  the human (`Hurt`, verified); on respawn they come back on the zombie
  team with the zombie hero and build. That is Random mode's
  kill-then-respawn-with-assigned-hero path (verified in `HeroLock` and
  `RandomModeService`).
- **Escape point:** reuse the rift. `RiftService` forces a rift at a
  chosen side (verified) and detects which team captured it (verified).
  Humans capturing = escape success; zombies capturing, or no humans
  left, = zombies win. Humans start at the far base and run a lane to it.
- **Humans:** normal heroes and builds (Random mode's draw, verified);
  their guns are how they slow zombies. Knockback items and abilities
  come with the heroes.
- **First thing to prove:** can we force the rift far from spawn and
  have a team of 1 capture it against a team of many (capture rules
  may depend on player counts)?

### Last team standing

Rounds without respawns: dead players go up top until the round ends.

- **Levers:** `player_spawn` → `WatchSpot.SendUp` (verified; already sends
  respawns up top and restrains them); round end when one team has no living
  fighters (new rule); `citadel_player_override_spawn_time` (verified as a
  convar we set; long values untested).
- **New:** "alive fighters per team" tracking in `RoundFlow`; round result
  without a rift.

### Hold the zone

Rift Roulette without the rift: a team scores while it alone stands in a
box.

- **Levers:** `Utils.Zone` enter/leave and `Occupants` (untested); `CBeam.CreateBox`
  to show the box (untested); HUD banner and boards (verified); `Timer.Every`
  to tick score (verified).
- **New:** a reusable `Modules/Zone` service (game-agnostic), a score rule.
- **Risk:** medium; first use of `Zone` and `CBeam`. Prove both with an
  admin command first, then self-test them.

### Juggernaut

One player gets a huge build and damage protection; everyone else hunts
them. Whoever kills the juggernaut becomes the next one.

- **Levers:** `LoadoutService` with a high soul value (verified),
  `EModifierState.NoIncomingDamage` or `OnTakeDamage` scaling (the hook is
  verified; changing damage amounts instead of blocking is untested),
  `player_death` for the handover (verified), `CParticleSystem` attached to
  the juggernaut (untested).

### Endless mid-rift

Theo's shelved proposal: one rift at the middle, respawning forever, first
to 25. Full notes, open questions and tests to run first:
`RiftRoulette/reference/endless-mode.md`.

## Owner's backlog

Modes and practice tools the redock server exists to prototype. Each gets a
recipe above (loop, setup, rules, feedback, levers) before any code. Game
context: [deadlock.wiki](https://deadlock.wiki/Deadlock) (standard mode is
6v6 over three lanes; Valve's own Street Brawl is 4v4 in one lane over short
rounds).

| Idea | Kind | First lever to prove |
|---|---|---|
| Grifball | mode | designed above; its own game type from the scaffold (Arena + Session already fit), then melee-only one-hit kills |
| Fat Boy / Zombie Escape | mode | designed above; build it as its own game type (`scripts/new-game-type.sh`), with an escape zone instead of Rift Roulette's rift |
| Bumper cars | mode | knockback-only fighting (`OnTakeDamage` changes, untested beyond `Stop`) |
| Lane practice | practice | turning one lane and its troopers back on (`citadel_active_lane`, `citadel_trooper_spawn_enabled`, verified as convars we set) |
| Build orders | practice | stepping a player through a stored build (`Modules/Loadout`, verified) |
| Strategies | practice | to be described by the owner |

## Checklist for a new mode

1. Write the loop, setup, rules and feedback as above. Mark every lever
   verified or untested.
2. Prove each untested lever with a Debug admin command and a log line
   before building on it.
3. Put reusable parts in `Modules/`, game-specific parts in a feature folder
   (or a new DLL for a whole new mode).
4. Pure decisions go in `*Rule.cs` with tests.
5. Add every new game dependency to `SelfTest/GameDependencies` and
   `patch-day.md` §5, and move the catalog entries to verified.
6. Update the command catalogs, `FEATURE.md`, and [changelog.md](changelog.md).
