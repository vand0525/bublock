# Endless mid-rift mode (shelved proposal)

Status: shelved. Nothing is built. This file records the idea, what is
already known, what must be tested before building, concerns, and
suggestions.

## The idea (as requested)

- One rift, at the middle of the map instead of green / yellow.
- Not round based. The rift respawns every time it disappears (captured or
  given up).
- Players start at level 0 with the regular 800 gold and build up over time
  from rift gold and level-ups ("boons"). An endless battle.
- A team scores a point each time it captures the rift first. First to 25
  wins; the target is set by command.
- Capturing gives gold. Ideally the exact amount is set through the game's
  own rules, with a sensible value.
- A tied rift drops souls and both teams fight over them.
- Optional: a usable shop for each team on its own side of the lane.
- Some protection that keeps enemy players off your side of the map.

## What we already know

### Rift

- The map has only two `info_koth_spawn_location` entities: green
  (7612, -0.000661, 444) and yellow (-7560, 0, 424)
  (`maps/dl_midtown/entities.json`). There is no middle spawn location.
- `RiftSides.MiddlePosition` (0, 0, 0) is defined but unused. The center of the map is the mid-boss area, which sits lower
  (`trigger_midboss_shield` origin z = -704, a `trigger_team_base` at
  z = -512), so (0, 0, 0) may be inside or under the geometry.
- We force a rift by writing `m_vNextKothLocation` and zeroing the spawn
  window / spawn time on `CCitadelGameRules` (`RiftGameRules.ConfigureNextRift`).
  Whether the game accepts a location that is not an
  `info_koth_spawn_location` is untested.
- The spawner (`citadel_item_koth_spawner`) removes itself about 1 s after
  spawning; the live objective is `citadel_koth_cashin`. A capture spawns new
  `npc_trooper` entities (the winner team is their `TeamNum`). A tie is the
  cash-in disappearing without new troopers, about 60 s after the spawn.
  While a cash-in is up, a forced spawn does nothing
  (`resources.md`, "A live rift is its cash-in, not its spawner").
- The 60 s give-up is not controlled by `citadel_koth_spawn_window` or
  `m_timeKothGiveUp` (`chat-handoff.md` §16-17). VData hacking is out of
  scope in the master plan.

### Rift gold

Console settings (`cvarlist.md`), all flagged `cheat`:

- `citadel_koth_reward_base`, default 1260
- `citadel_koth_reward_time_multiplier`, default 222 (unit unknown; probably
  scales with match time)
- `citadel_koth_reward_buff_count`, default 0 (possibly the number of buffs
  granted; unverified)

Other cheat-flagged convars (`citadel_allow_purchasing_anywhere`) are already
set by the plugin with `ServerConVars.TrySet`, so setting these from the
plugin should work. Who receives the reward, how it is split, and when is
unknown.

### Gold and level

- `pawn.SetCurrency(ECurrencyType.EGold, n)` sets gold (`LobbyHeroes`,
  `LoadoutService`).
- `pawn.Level = n` then `ModifyCurrency(EGold, 0, ECheats, silent: true)`
  recalculates stats (`LoadoutService`, DeathmatchPlugin example).
  `LobbyHeroes` already resets to level 0 with 0 gold.
- Whether gold given by the plugin counts toward level-ups depends on the
  currency source; untested.

### Shops

- Each team has a tier-1 shop in the middle lane, on its own half:
  - Sapphire (team 3, +y side): `shop_combine_t1_blue_shop_item_trigger`
    at (-1024, 1248, 440), prop at (-1024, 1184, 376).
  - Amber (team 2, -y side): `shop_rebel_t1_blue_shop_item_trigger` at
    (1024, -1248, 440), prop at (1024, -1184, 376).
  - There are also the two center shops under the map
    (`2841_shop_*` at (3920, -1120, -80) and `2844_shop_*` at
    (-3920, 1120, -80)), each with a `trigger_item_shop_safe_zone`.
- CleanSlate (a separate DLL) removes every `citadel_shop_prop_dynamic` and
  disables every `trigger_item_shop` / `trigger_item_shop_safe_zone` on
  startup and on hot reload. A removed prop does not come back until the map
  restarts, and no entity-creation API is verified.
- `citadel_allow_purchasing_anywhere` is global, not per player or per area.

### Team sides

Sapphire's base is at +y (spawns around y = +10,800), Amber's at -y
(`resources.md`). The lanes run along y, so y = 0 splits the map between the
teams.

## What needs to be tested (in order)

Each test is small and can be run with the existing admin tools plus one or
two discovery commands (see Suggestions).

1. **Rift at a custom spot.** Force a rift at a chosen middle position (stand
   there and spawn it at your own position). Check that it spawns, where the
   cash-in appears, that a capture spawns troopers, and that a tie still
   gives up after about 60 s. Try the mid-boss floor and the mid-lane road
   near the tier-1 shops.
2. **Back-to-back respawn.** Right after a capture or a give-up, force the
   next rift at the same spot. Check for a hidden cooldown
   (`citadel_koth_respawn_interval` is 420 by default; our forced spawn
   normally bypasses it).
3. **Reward settings.** Set `citadel_koth_reward_base`, set
   `citadel_koth_reward_time_multiplier` to 0, capture, and log every
   player's gold before and after. Find out which team gets it, whether it
   is per player or split, and whether it arrives at the capture. Then try
   `citadel_koth_reward_buff_count` 1 and see what it grants.
4. **Tie aftermath.** Take an entity snapshot just before the give-up and
   compare after (DevTools snapshot / compare). Confirm whether souls
   actually drop, what entity they are, and whether players can pick them up.
5. **Rift troopers in an endless mode.** After a capture, watch where the
   troopers go (with lanes off). Decide whether to leave them, remove them
   after a delay, or remove them at once as Rift Roulette does.
6. **Level 0 and 800 gold.** On spawn, set level 0 and 800 gold. Check that
   earning gold (rift reward, kills) levels the hero up normally, and which
   `ECurrencySource` to use if the plugin grants gold itself.
7. **Shop.** Keep one mid-lane shop prop (CleanSlate change), enable its
   trigger with `AcceptInput("Enable")`, and check that players can buy
   there, whether the enemy team can also use it, and whether the trigger
   works without the prop.
8. **Respawn.** Where players respawn (base, far from mid), how long the
   timer is, and whether teleporting them to a spot near their mid-lane shop
   on `player_spawn` works cleanly.
9. **Side protection.** Try teleporting an enemy who crosses into the other
   half back to their own side, as `Round/WatchGuard` does. Check whether a
   damage API exists before considering damage over time (none is verified
   today). Also check what the native `trigger_item_shop_safe_zone` and
   `trigger_team_base` do when enabled.
10. **Lanes and ziplines.** CleanSlate sets `citadel_active_lane 0` and turns
    troopers off. Check that ziplines and movement to mid still work.

## Concerns

- **The middle may not accept a rift.** If the game only spawns rifts at
  `info_koth_spawn_location` entities (or snaps to the nearest one), a true
  middle rift is impossible without new map entities. The fallback would be
  alternating green / yellow with no rounds.
- **The 60 s give-up is fixed.** A rift nobody captures still ties after
  about 60 s, so ties will be common in small games. That may be fine if
  ties drop souls (test 4).
- **Snowballing.** Endless play with gold for the capturing team rewards the
  team already ahead. Without catch-up, games can become one-sided.
- **Rift Roulette hooks assume rounds.** On spawn, Lobby sends players to
  the watch spot and restrains them; WatchGuard pulls restrained players up;
  CleanSlate removes shops; ShopAccess owns the buy-anywhere convar;
  Random / Mirror modes lock heroes; stats and balance run per intermission.
  An endless mode must switch all of these off, or they will fight it.
- **CleanSlate is a separate DLL.** It cannot be told about the mode (no
  cross-DLL calls). Keeping the two mid-lane shop props means changing its
  removal list for every mode, or adding a convar-based switch.
- **"Endless" versus "first to 25".** The requirements have both. What
  happens at 25 (match over, reset score only, reset levels too?) needs a
  decision.
- **Respawn distance.** Base spawns are about 10,000 units from mid; without
  a respawn teleport, most of the game is walking.
- **Unverified APIs.** Damage, entity creation, and per-player shop access
  are not verified in Deadworks. Do not build on them before testing.

## Suggestions

- **Build it as a match format inside Rift Roulette** (for example
  `/match_mode endless`), not a separate DLL. It reuses lobby, team balance,
  HUD, stats and auto-start. Add one check (`MatchConfig.IsEndless`) where
  the round-only hooks run (watch spot on spawn, restraint, WatchGuard,
  intermission work). A separate DLL would need its own copies of all of
  that and must never be deployed alongside Rift Roulette.
- **Discovery commands first**, built on their own:
  - `/rift_at`: force a rift at the admin's current position (no typed
    coordinates), reusing `ConfigureNextRift` and the existing spawn wait
    and watch.
  - `/koth_reward <base> <multiplier> <buffs>`: set the three reward
    convars and log them.
  - A per-player gold snapshot in the rift log around each capture.
- **Rift loop.** Reuse `RiftService`'s spawn, adopt, watch and cleanup
  steps. Replace the intermission with a short respawn delay (for example
  10 s, with a `Rift in 10s` banner) and never send players up top.
- **Scoring.** Score the first capture per rift (the winner team is already
  known from the trooper `TeamNum`). `/endless_target <n>` with a default of
  25. At the target: winner banner, then reset scores and start again.
  Levels and gold carry over unless you choose otherwise.
- **Gold values.** Tier-1 items cost 800, tier 2 1600, tier 3 3200. A rift
  takes 60-120 s. Suggested starting point: about 1,500 per player on the
  capturing team (roughly one tier-2 item per capture) and about 500 per
  player on the other team as catch-up, with the time multiplier at 0 so
  the value stays predictable. If the convars cannot give exact per-player
  amounts, set `citadel_koth_reward_base` to 0 and grant gold from the
  plugin on capture, which gives full control.
- **Shops.** Keep only the two mid-lane tier-1 shop props in CleanSlate and
  enable just those two triggers in endless mode. If a shop cannot be
  limited to one team, the side protection handles that, because enemies
  cannot reach it.
- **Side protection.** A per-frame guard like `Round/WatchGuard`: an enemy
  more than a neutral band past y = 0 (for example 2,500 units, so the fight
  around the rift stays open) is teleported back to the edge of the band
  with a short cooldown and one log line. Teleporting is proven today;
  damage is not.
- **Respawn point.** Teleport respawning players to a spot near their team's
  mid-lane shop, on their own half.

## Open questions for the user

- Hero choice: free pick from the menu, random on join, or random and
  locked?
- What happens at the score target?
- Enemy on your side: teleport back, damage over time, or killed after a
  warning?
- Catch-up gold for the losing team: yes or no?
- Keep rift troopers after a capture, or remove them?

## Proposed build order

1. Discovery: `/rift_at`, `/koth_reward`, gold snapshot; run tests 1-4.
2. Endless loop: `MatchConfig` endless format, rift respawn loop, scoring to
   the target, round-only hooks switched off.
3. Economy: level 0 and 800 gold on join, rift gold (convars or
   plugin-granted), catch-up.
4. Shops: CleanSlate keeps the mid-lane props; endless mode enables their
   triggers.
5. Side protection and respawn teleport.
