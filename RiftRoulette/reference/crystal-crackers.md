# Crystal Crackers (mode note)

Status: in development (`Bublock/CrystalCrackers/`), the mode DLL
`deploy.sh` uploads for now (Rift Roulette parked). No gameplay code yet;
the server is always private (`AccessGate`: admins and the `allowed` list
only).
This file records the mode as requested, how it fits next to Rift
Roulette, what is reusable today, and what must be found in game before
building. Same role as `endless-mode.md`.

## The idea (as requested)

- Same base as Rift Roulette Random mode: random heroes, one stored build
  each, round based, even teams.
- Each round starts once in the mid-boss pit, with a mid-boss already just
  above the health where it screams.
- That team can kill the boss; the other team can come in and steal the
  kill, take the crystals, and fight.
- Score from kills, the mid-boss kill, and the number of crystals taken.
- A kill on someone the crystal then revives is worth fewer points.
- Damage to the boss scales with the number of fighters; 6v6 is the
  baseline.
- The boss must stay killable in a long lobby (two hours or more), so the
  mode needs granular control over the boss itself.

## How it fits next to Rift Roulette

- Separate DLL, shared modules. `CrystalCrackers.dll` compiles in
  `Shared` and the same `Modules/` as Rift Roulette (WorldText, Movement,
  Hud, Loadout, Restraint with their commands; Queue, Spectate,
  RandomHeroes).
- **One mode DLL on the server at a time.** Deadworks loads every
  `plugins/*.dll`, each with its own copy of module statics, and both mode
  DLLs register the module commands (`loadout_*`, `mv_*`, `restrain`,
  `hud_*`, `wt_*`) and the access commands. During development
  `scripts/deploy.sh` uploads Crystal Crackers (`PLUGINS`) and parks Rift
  Roulette (`PARKED_PLUGINS`: backed up, removed from `plugins/`, logs
  kept). Going back is swapping the two names; `rollback.sh` removes the
  other mode DLL when it restores one. A real mode switch comes later.
- **Always private.** `AccessPlugin` / `AccessGate` check the shared
  `bublock/access.json` (`Modules/Access`) with `forcePrivate: true`: only
  `AdminAuth` admins and the `allowed` list join, a ban is refused, and
  players without access are kicked 2 s after every load. The file's
  `private` value is never written, so Rift Roulette comes back in its old
  mode. Hiding the server from the browser is done on the host.
- **CleanSlate stays mode-agnostic.** It keeps the vanilla mid-boss
  scheduler off (`citadel_midboss_initial_spawn_time_override 999999`) and
  removes troopers, lane guardians, Walkers (`npc_boss_tier2`, the six lane
  bosses at z 384), barracks bosses, shops and the urn for every mode. The
  mid-boss is not one of those: it is not on the map at load, the game
  spawns it at runtime from an `info_neutral_trooper_camp` (subclass
  `neutral_camp_midboss`) at about (0, 0, -768). Crystal Crackers will
  spawn and retune its own boss each round, after CleanSlate's passes
  (2, 10, 30 s after load). The boss is `npc_super_neutral`, which is not
  on CleanSlate's removal list.
- Restrained players take no damage in any DLL that imports Restraint's
  commands (`RestraintPlugin.OnTakeDamage`).

## Reusable today

| Need | Piece |
|---|---|
| Random hero per fighter, no repeat | `Modules/RandomHeroes` `HeroDraw.Draw` |
| Hero + build within a soul cap | `Modules/Loadout` `LoadoutService.Swap`, `HeroBuildCatalog` |
| Restrain up top, release into the fight | `Modules/Restraint` |
| Teleports, named spots | `Modules/Movement` |
| Banners, boards | `Modules/Hud`, `Modules/WorldText` |
| Bench rotation on an odd count | `Modules/Queue` `PlayerQueue` |
| Stream camera follow | `Modules/Spectate` |
| Logging, admin auth, chat | `Shared` |
| Bans, allowed list, private gate | `Modules/Access` (`AccessStore`, `AccessList.Check`) |
| Mid-boss spawn, health, damage scale, kill / crystal watch | `Modules/MidBoss` (`MidBossService`, `/midboss_*`; discovery stage) |

Still inside Rift Roulette (copy or extract in a later stage): the lobby
(connect / disconnect, `Participants`, admin seat, team balance with
`RiftRouletteTeams`), the match loop (`MatchService`, auto-start), the
bench rule, stats and assists, betting. The rift round composer
(`RoundFlow` to `RiftService`) is not reused.

## Round shape

Same intermission as Rift Roulette: random hero and one stored build,
even teams, bench on an odd count, everyone restrained up top. Then a new
round driver in the Crystal Crackers DLL releases fighters into the pit.
The pit shield trigger (`trigger_midboss_shield`) sits at about
(72, 0, -704); today's center spots are lane height (z 384), so the pit
needs new per-slot anchors (and `scripts/check-spots.py`).

## Scoring (once the events exist)

- A kill.
- The mid-boss kill.
- Each crystal taken.
- A kill on a player the crystal revives is worth less.

## Boss control

Each round sets the boss again, so a long lobby cannot leave an
unkillable boss: health just above the scream, and a damage scale for this
round's headcount (6v6 = 1.0).

## What is known about the boss (game data, 2026-10-13)

Details and sources: `resources.md` ("The mid-boss is `npc_super_neutral`").

- **Classname.** `npc_super_neutral` (C++ `CNPC_MidBoss`). Not
  `npc_mid_boss`, `npc_boss_tier2` (Walkers) or `npc_boss_tier3` (Patrons).
- **Health.** 13,000 + 195 per minute alive, 15 health/s regen. Health is
  the plain `CBaseEntity` health (`Health` setter, `m_iMaxHealth` through a
  schema write). A fresh boss each round resets the growth.
- **Resistance.** 35 + 5 per **game** minute. This is what would make it
  unkillable in a long lobby; counter it by removing the modifier, hitting
  with `IgnoreResistances`, or scaling damage up.
- **Scream.** At 50% health; no field or convar, probably hard-coded.
  Setting health to 51% puts it just above.
- **Pit.** Only players inside `trigger_midboss_shield` can damage it.
- **Crystal.** `citadel_item_pickup_rejuv` (`m_bPickedUp`), dropped by game
  code on death; `GameRules.AmberRejuvCount` / `SapphireRejuvCount` count
  pickups. No game event for a pickup or a boss kill (use `entity_killed`).
- **Spawning.** Two routes: the camp's scheduler
  (`citadel_midboss_initial_spawn_time_override`), which CleanSlate parks
  and `citadel_npc_spawn_enabled 0` may block, or
  `CreateByDesignerName("npc_super_neutral")` with keyvalues (untested;
  community notes report crashes for bosses created without keyvalues).

## Still unknown (the `/midboss_*` probe answers these)

`Modules/MidBoss` (`/midboss_*` admin commands, `midboss-*.log`) tries each
on the private dev server:

1. `/midboss_status`: nothing tracked, convar values.
2. `/midboss_schedule 1`: does the scheduler spawn one with
   `citadel_npc_spawn_enabled 0`? (`/midboss_schedule off` parks it again.)
3. `/midboss_spawn`: does a created boss spawn and fight, or crash the
   server (rollback stamp ready)? Does it keep the pit gating?
4. `/midboss_percent 51`, `/midboss_maxhealth`: do health writes stick,
   and does the scream follow?
5. `/midboss_damage_scale 3`, then shoot it: does a written
   `Info.Damage` change the hit (status shows damage before / after and
   the health)?
6. `/midboss_status` modifier list, then `/midboss_modifier_remove <name>`
   on the resistance modifier.
7. `/midboss_kill`: does the crystal drop for the killer's team (master
   `Mid-boss killed`, `Crystal spawned`), and is the pickup logged
   (`Crystal removed PickedUp=`, counts changed)?

Record each answer in `resources.md` Discoveries.

## Proposed build order

1. Boss probe (above) with `Modules/MidBoss`.
2. Lobby and match loop for the Crystal Crackers DLL (copy or extract from
   Rift Roulette), then a real mode switch (today it is a deploy.sh edit).
3. Pit round driver: release into the pit, round end on boss kill or time.
4. Boss control per round: health, damage scale by headcount.
5. Scoring: kills, boss kill, crystals, reduced points for revived kills.
