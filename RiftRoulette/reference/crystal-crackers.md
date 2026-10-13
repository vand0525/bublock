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
  (2, 10, 30 s after load). If the probe shows the spawned boss uses a
  classname on CleanSlate's removal list, revisit then.
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

## Unknowns (probe in game before building)

None of these is in the checked-in convar list or schema notes:

- **Scream percent.** No convar or field for the health where the boss
  screams.
- **Player-count damage.** No convar for boss damage by fighter count.
  `OnTakeDamage` sees hits on any entity; whether a hit's damage can be
  scaled there (rather than only stopped) is unverified.
- **Crystal pickup.** No event or entity name known for the crystals or for
  picking one up, or for the revive it grants.
- **Health on a boss.** `CBaseEntity.Health` has a setter and `MaxHealth`
  is read-only; neither has been tried on a boss.
- **The mid-boss classname.** Unknown. The map only has its spawner
  (`info_neutral_trooper_camp`, subclass `neutral_camp_midboss`); the boss
  entity it creates has never been seen. It is not `npc_boss_tier2` (those
  are the Walkers) or `npc_boss_tier3` (the Patrons).
- **Spawning one.** `CreateByDesignerName` exists; spawning a boss with it
  is untested. Another route is letting the scheduler spawn it once
  (`citadel_midboss_initial_spawn_time_override` set low) and adopting it.

Probe: take `dw_ent_snapshot`, lower
`citadel_midboss_initial_spawn_time_override` so the game spawns its own
mid-boss, then `dw_ent_diff` to learn its classname. Then write `Health`
and watch whether a scream, a kill and a crystal entity show up, and try
`CreateByDesignerName` with that classname in the pit. Record every finding in `resources.md`
Discoveries and the self-test (`SelfTest/GameDependencies`,
`patch-day.md` section 5) in the same change that starts depending on it.

## Proposed build order

1. Boss probe (above), as DevTools discovery commands.
2. Lobby and match loop for the Crystal Crackers DLL (copy or extract from
   Rift Roulette), then a real mode switch (today it is a deploy.sh edit).
3. Pit round driver: release into the pit, round end on boss kill or time.
4. Boss control per round: health, damage scale by headcount.
5. Scoring: kills, boss kill, crystals, reduced points for revived kills.
