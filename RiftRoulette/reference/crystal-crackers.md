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
| Mid-boss spawn, killable boss sized by souls and fighters, kill / crystal watch | `Modules/MidBoss` (`MidBossService`, `/midboss_*`; discovery stage) |

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
unkillable boss: a fresh boss, its resistance modifier removed, max and
health written (just above the scream), and a damage scale for this
round's headcount (6v6 = 1.0). The probe showed the resistance, not the
health, is what makes a late boss unkillable.

`/midboss_ready <souls> [fighters]` (`MidBossService.Ready`) does all of
it in one call; the round driver will call it with the round's soul cap
and fighter count. Sizing:

- **Max health from real matches.** `scripts/midboss-health.py` (1,000
  ranked matches, 2026-10-13) gives the game minute at which a real
  lobby's average net worth per player reaches each soul value; the max
  is what the game gives a boss spawned at that minute
  (13,000 + 195 × minute). Examples: 10,000 souls is minute 12.6, 15,535;
  20,000 is minute 21.1, 17,095; 30,000 is minute 28.4, 18,460; 45,000 is
  minute 38.9, 20,605. Real first kills come at a median minute 23.9 and
  23,665 souls, on that curve.
- **Health** 51% of that max.
- **Damage scale** 12 / fighters (2 fighters hit 6×, 1 is clamped to
  10×).
- Without the resistance the boss is easier than in a real match (a real
  boss at minute 21 has a threshold near 140). If rounds end too fast,
  lower the scale or raise the health; that needs playtests.
- The boss is never killed by code; there is no kill command.

## What is known about the boss

From game data and the first in-game probe (2026-10-13). Sources:
`resources.md` ("The mid-boss is `npc_super_neutral`", "Mid-boss probe
results"); full run in `probes/2026-10-13-midboss.md`.

- **Classname.** `npc_super_neutral` (C++ `CNPC_MidBoss`). Not
  `npc_mid_boss`, `npc_boss_tier2` (Walkers) or `npc_boss_tier3` (Patrons).
- **Spawning (proven).** `/midboss_spawn` (`CreateByDesignerName` +
  keyvalues) at the camp spot makes a full mid-boss with no crash: team 4,
  pit rule, kill credit, crystal drop, and the game's own
  `midboss_respawned` event. The scheduler route did nothing with
  CleanSlate's `citadel_npc_spawn_enabled 0`; it isn't needed.
- **Health (proven).** A new boss starts at 13,000 + 195 × **game**
  minutes (about 36,400 two hours in), regenerates about 15 health/s, and
  gets the vdata max on every spawn. `Health` writes hold;
  `m_iMaxHealth` writes hold but don't clamp current health. Each round:
  write the max, then the health.
- **Resistance (proven).** `npc_super_neutral/midboss_modifier_damage_resistance`
  is a damage threshold (35 + 5 per game minute) applied before
  `OnTakeDamage`: with it on, pit hits reach the hook at 0 damage, so
  scaling can't help. Removing it works (`RemoveModifier`) and the
  in-game resistance bar goes away. Remove it on every spawn.
- **Damage scale (proven, without the resistance).** Writing
  `Info.Damage` in `OnTakeDamage` changes the hit (×5 turned 390 into
  1,950).
- **Scream.** Not triggered by writing health below 50%, or by shooting a
  boss already below half. Probably needs damage to cross 50% (untested).
- **Pit (proven).** Hits from outside `trigger_midboss_shield` arrive with
  `NotInMidbossPit` and 0 damage, on a created boss too.
- **Death and crystal (proven).** `entity_killed` names the killer; the
  crystal `citadel_item_pickup_rejuv` spawns the same frame 500 units above
  the pit (team 4) and falls to the floor; the boss entity goes 1.4 s
  later. The killer's team count rose 1, 2, 3 over 6 s, the crystal was
  removed with `m_bPickedUp` set, and the count reset after about 180 s.

## Still unknown

- What makes it scream (try damage crossing 50% on a full-health boss
  with the resistance removed).
- What the three steps of the team's crystal count are.
- Whether a `/midboss_ready` boss at 51% is fair to kill for the
  fighter count (playtest), and whether the game re-adds the resistance
  after spawn (the 1 s recheck reports it).
- Whether the scheduler is blocked by `citadel_npc_spawn_enabled 0` or by
  the clock being past the initial spawn (only matters if the direct
  route ever breaks).

## Proposed build order

1. Boss probe with `Modules/MidBoss` (first run done 2026-10-13); test
   `/midboss_ready` and the scream on a ready boss.
2. Lobby and match loop for the Crystal Crackers DLL (copy or extract from
   Rift Roulette), then a real mode switch (today it is a deploy.sh edit).
3. Pit round driver: release into the pit, round end on boss kill or time.
4. Boss control per round: spawn directly, remove the resistance, write
   max then health, damage scale by headcount.
5. Scoring: kills, boss kill, crystals, reduced points for revived kills.
