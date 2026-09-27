# Patch day runbook

What to do when a big Deadlock patch lands. The goal is to know within minutes
what broke, and to have a known fix path for each break.

Tools:

- `scripts/patch-baseline.sh`: saves a "known good" snapshot (run **before** a patch).
- `scripts/patch-check.py`: diffs fresh game data against that snapshot, filtered to what our code uses (run **after** a patch).
- `dw_selftest_run`: in-game read-only check of every live game dependency, one PASS / WARN / FAIL line each (`selftest-*.log`).
- `dw_selftest_live <slot>`: exercises teleport, restraint, banner and loadout reading on one player.
- The existing diagnostics: `dw_ent_find`, `dw_ent_info`, `dw_ent_snapshot` / `dw_ent_diff`, `/spots_walk`, `/rift_start`, `pull-logs.sh`.

---

## 1. Patch day, in order

1. **Before the patch** (any day): `scripts/patch-baseline.sh` and `dw_selftest_run` once. Keep the self-test log as the known-good output; WARN lines in it are normal for this server and are not patch damage.
2. **Does the server start?** Watch the host console. If it crashes or loops on boot, move our three DLLs out of `managed/plugins` over SFTP and boot again (`rollback.sh` would only put older builds of the same DLLs back). If it still crashes without them, it is Deadworks itself: wait for the Deadworks update (GitHub releases, Discord) and stop here.
3. **Update `lib/`**: copy the new `DeadworksManaged.Api.dll` (and `Google.Protobuf.dll`) from the updated server into `deadworks/lib/`. Enum values (`Heroes`, `EModifierState`, `ECurrencyType`, `EAbilitySlot`) are compiled into our DLLs, so an old build can silently use wrong numbers.
4. **`scripts/update.sh`**: compile errors here are API renames or removals. Fix them with the decompiled API (`~/.dotnet/tools/ilspycmd lib/DeadworksManaged.Api.dll > /tmp/dwapi.cs`).
5. **`scripts/test.sh`**.
6. **`python3 scripts/patch-check.py`** (needs network). Every `HIT` line names the code to fix; section 4 says how.
7. **Refresh data if the check says so**: `python3 scripts/fetch-builds.py` (items, heroes), map dump + `scripts/check-spots.py` (geometry), `reference/cvarlist.md` (convars).
8. **Upload** with `scripts/deploy.sh --confirm` (with approval).
9. **`dw_selftest_run`**, then compare with the baseline log. Every new FAIL / WARN maps to a row in section 4.
10. **`dw_selftest_live <slot>`** on yourself, then the smoke list (section 3).
11. **`scripts/pull-logs.sh`** and read `master-*.log` (every feature Warning+ is copied there).

---

## 2. Core function catalogue

One row per feature: what it does, the game dependencies it rests on, and the fastest proof it works.

| Feature | What it does | Game dependencies | Proof |
|---|---|---|---|
| Session (`Session/`) | Session id, map name, registers locations | `Server.MapName`, `OnLoad` / `OnStartupServer` | `dw_session_info` |
| Lobby (`Lobby/`) | Admits players, balances teams, convars, kick, respawn to watch spot | `OnClientConnect` / `FullConnect` / `Disconnect`, `player_spawn`, `player_death`, `SelectHero(Heroes.Skyrunner)`, `ChangeTeam`, `kickid`, lobby convars | join; `/status`; `dw_player_list` |
| AdminSeat (`Lobby/AdminSeat`) | 13th seat on the spectator side | `maxplayers`, `sv_visiblemaxplayers`, team 1 | `dw_seat_status` |
| Stream camera (`Lobby/StreamCam`, `Modules/Spectate`) | Seated admin's automatic camera: follow, killer cut, top-down on big ults | `observer` pawn designer name, `ObserverServices` (`InEye`, `Roaming`, `SetObserverTarget`), observer `Teleport`, `player_used_ability`, ult class names in `Lobby/BigUlts`, client `spec_player` and `spec_mode 4` (fly cam) | `dw_spec_status`, `dw_spec_overview` |
| Access (`Lobby/Access*`) | Bans / private mode from `bublock/access.json` | `OnClientConnect` returning false, `kickid` | `/access_mode` |
| Draft (`Draft/`) | Draft picks, boards (off in Random mode) | `player_hero_changed`, `SelectHero`, `Heroes` pools, `point_worldtext` | `/draft_status`, `/draft_boards` |
| Rift (`Rift/`) | Forces a rift at a side, detects capture / tie, cleans troopers | KOTH schema fields, `citadel_gamerules`, `citadel_item_koth_spawner`, `citadel_koth_cashin`, `npc_trooper`, `citadel_koth_enabled`, rift positions | `/rift_start green`, `rift-*.log` |
| Round (`Round/`) | Round flow, watch spot, per-slot spots, WatchGuard | `Teleport`, camera net message, anchors, `spots.json`, skybox floor z 1536 | `/spots_walk`, `/rift_start` |
| GameLoop (`GameLoop/`) | Match loop, auto-start, shop access, probe | `OnGameFrame`, `citadel_allow_purchasing_anywhere` | `/match_status`, `probe-*.log` |
| Random (`RandomMode/`) | Random hero + stored build each intermission | `player_respawned`, `player_spawn`, `hero-builds.json`, `Heroes` enum | `/random_status`, `loadout-*.log` |
| Stats (`Stats/`) | Kills / deaths / assists boards | `player_death` fields (`Attacker*`, `Assister1..5controller`) | `/stats` after a kill |
| Balance (`Balance/`) | Swaps players between rounds | `ChangeTeam` | `/balance_status` |
| Duel (`Duel/`) | 1v1 copy build, queue, winner stays | `LoadoutSnapshot` (abilities, items, imbues), `SelectHero`, `Hurt` | `/duel_status`, `/duel_copy` |
| WorldText (`Modules/WorldText`) | In-world text boards | `CPointWorldText.Create`, `point_worldtext` | boards visible; `dw_wt_list` |
| Movement (`Modules/Movement`) | Named locations, teleports, camera angle | `Teleport`, `CCitadelUserMsg_SetClientCameraAngles` | `dw_mv_tp`, `dw_mv_angle` |
| Hud (`Modules/Hud`) | Banners | `HudAnnounce` | `dw_hud_announce` |
| Loadout (`Modules/Loadout`) | Give a stored build, copy a hero | `AddItem`, `ImbueItem`, `ItemInfo`, `FindAbilityByName`, `UpgradeBits`, `ResetHero`, `Level`, currencies | `dw_loadout_give <slot> <hero>` |
| Restraint (`Modules/Restraint`) | Silence / no items / no shooting / no melee up top; NPCs ignore restrained players and they take no damage | `modifier_citadel_silenced`, `EModifierState` values, `OnGameFrame`, `OnTakeDamage` (GameLoop, `HookResult.Stop`) | `dw_restrain <slot>`, `dw_restrain_list`; self-test counter `damage_blocked_restrained` |
| Queue (`Modules/Queue`) | Player queue | none | `/queue` in 1v1 mode |
| CleanSlate (`CleanSlate.dll`) | Removes bosses / shops / powerups, disables shop triggers, spawn convars | designer names in `CleanSlateService`, spawn convars | `dw_cleanup_run` |
| DevTools (`DevTools.dll`) | Entity inspection, log path | `Entities.All`, `ByDesignerName`, `SubclassVData` | `dw_ent_find koth` |
| Shared | Auth, chat, logging | `CCitadelUserMsg_ChatMsg`, `PrintToConsole`, log path from the API assembly | `dw_dev_logpath` |

---

## 3. Smoke list (after upload)

1. Join: you land on your own watch spot facing the board, restrained (try to shoot).
2. `/match_status`, then with 2 players the match auto-starts; banner shows.
3. Random mode: each player gets a hero and a build (items in the shop panel, abilities levelled).
4. Round: teams land in rows at their starts, restraint lifts, rift spawns at the right side.
5. Capture the rift: round result banner, score changes, everyone back up top.
6. Kill someone: `/stats` counts it.
7. `/match_mode 1v1`, `/duel_copy <slot>`: both players get the same build.
8. `dw_cleanup_run` reports 0 of everything already removed; no bosses or shop kiosks visible.

---

## 4. Symptom to fix

| Symptom | Likely cause | Check | Fix |
|---|---|---|---|
| Server crashes on boot only with our DLLs | Deadworks API changed under an old build | host console | update `lib/`, rebuild, upload |
| Server crashes without our DLLs | Deadworks not updated for the patch | host console | wait for Deadworks |
| Build fails after updating `lib/` | API member renamed / removed | compiler error | find the new name in `/tmp/dwapi.cs` |
| No commands work | plugin failed to load | host console `[Deadworks]` lines; `dw_help` | rebuild against new `lib/` |
| Hooks silent (no join handling, no deaths) | event renamed or Deadworks hook broken | self-test Events WARN; `dw_dev_herowatch` | check the event name in the decompiled `GameEventHandler` list |
| Rift never spawns | KOTH schema field renamed, proxy name changed, or scheduler changed | self-test Schema / Entities FAIL; `rift-*.log` `ProxyMissing` / `PointerNull` | new field names from the schema DB (deadworks.net/db/schema, `CCitadelGameRules`); update `RiftGameRules` |
| Rift spawns in the wrong place / not detected | rift position moved, or spawner / cash-in renamed | self-test Map `info_koth_spawn_location` WARN; `dw_ent_snapshot` / `dw_ent_diff` around a spawn | update `RiftSide` positions / `RiftService` names |
| Round never ends on capture | `npc_trooper` renamed or capture no longer spawns troopers | `dw_ent_diff` after a capture | update `RiftService` capture detection |
| Heroes get no / few items | item class names renamed / removed | self-test Items FAIL; `loadout-*.log` `Unknown=` / `Failed=`; patch-check Items HIT | `fetch-builds.py`, rebuild |
| Abilities not levelled | ability names changed or upgrade bits changed | `loadout-*.log` Trace `missing ability` | `fetch-builds.py`; check `UpgradeBits` in `/tmp/dwapi.cs` |
| Loadout level or ranks look wrong for the build's value | boon / point thresholds or tier costs changed | `loadout-*.log` `Loadout applied` `Level=` `Points=` `Ranks=` vs the hero panel; wiki soul table | update `Modules/Loadout/Progression.cs` and `LoadoutPlanner.UpgradeCosts`, then `ProgressionTests` |
| Random mode never gives some hero / new hero missing | new hero id not in enum or builds | patch-check Heroes HIT; self-test Heroes WARN | update `lib/`, `fetch-builds.py` |
| Joining fails / players stuck in hero select | `Heroes.Skyrunner` removed or not selectable | patch-check `Skyrunner`; `lobby-*.log` | pick another lobby hero in `LobbyService` / `DraftService` |
| Players fall from the watch spot | skybox floor moved or removed | self-test Map floor WARN (compare to baseline); `watch-*.log` rescues | new map dump; move the watch anchors; `check-spots.py` |
| Players spawn in walls at rift starts | map geometry changed | `/spots_walk sapphire|amber`; `check-spots.py` | new map dump, move anchors / `spots.json` |
| Players can shoot / cast up top | modifier or state renamed / renumbered | self-test live Restraint FAIL; `restraint` Trace `refused` | new names from the schema DB / enum |
| Players take damage up top (turrets, troopers) | `OnTakeDamage` no longer fires or `Stop` no longer blocks | self-test Events `take_damage` = 0 after a fight; `damage_blocked_restrained` stays 0 | check `OnTakeDamage` / `TakeDamageEvent` in `/tmp/dwapi.cs`; fallback state `EModifierState.NoIncomingDamage` (142) in `RestraintService.States` |
| Bosses, shops or urn back on the map | CleanSlate names or crate convars changed | self-test Entities WARN; convar FAIL; `probe-*.log` counts | `dw_ent_find boss` / `shop`; update `CleanSlateService` |
| Settings not applied (team size, respawn, duplicates) | convar renamed / removed / hidden | self-test Convars FAIL; `Convar missing` warning in master log | new name from `cvarlist.md` upstream |
| Banner or camera angle missing | protobuf message changed | `dw_hud_announce`, `dw_mv_angle` | check the message in the new `lib/` |
| Boards missing | `point_worldtext` / `CPointWorldText` changed | `dw_wt_create test` | check `CPointWorldText` in `/tmp/dwapi.cs` |
| Stream camera never goes top-down on ults | ult renamed / reworked, or `player_used_ability` no longer fires | self-test Events `player_used_ability`; `lobby-*.log` `Ability name seen for the first time` | new `signature4` names from `assets.deadlock-api.com/v2/heroes` into `Lobby/BigUlts` |
| Stream camera stuck / not following | observer pawn renamed or observer services changed | `dw_spec_status` (`Observer=False`, `Mode=`); `spectate-*.log` | check `CPlayer_ObserverServices` and the pawn designer name in `/tmp/dwapi.cs` |
| Top-down never moves (camera stays in the directed view) | `spec_mode` renamed, lost `clientcmd_can_execute`, or fly cam is no longer 4 | `lobby-*.log` `Reason=repark` every 6 s; `spec_mode` in `patch-check.py` convar diff | new fly cam value / command from `cvarlist.md` into `SpectateService.FlyCamMode` |

---

## 5. Dependency tables (file:line at 2026-09-27)

### Convars

| Name | Value | Where |
|---|---|---|
| `citadel_team_size` | 6 | `Lobby/LobbyService.cs` `ApplyServerConvars` |
| `maxplayers` | 13 | same |
| `sv_visiblemaxplayers` | 12 | same |
| `citadel_koth_enabled` | 0 idle, toggled per round | same; `Rift/RiftGameRules.cs` `SetKothEnabled` |
| `citadel_koth_warning_time` | 1 (console) | `LobbyService` |
| `citadel_koth_early_warning_time` | 1 (console) | `LobbyService` |
| `citadel_player_override_spawn_time` | 1 (console) | `LobbyService` |
| `citadel_allow_duplicate_heroes` | 1 | `LobbyService` |
| `citadel_allow_purchasing_anywhere` | 0, or 1 in 1v1 setup | `GameLoop/ShopAccess.cs` |
| `citadel_trooper_spawn_enabled` | 0 | `CleanSlate/CleanSlateService.cs` |
| `citadel_npc_spawn_enabled` | 0 | same |
| `citadel_active_lane` | 0 | same |
| `citadel_midboss_initial_spawn_time_override` | 999999 | same |
| `citadel_crate_spawn_enabled` | 0 | same |
| `citadel_crate_disable_early_spawn` | 1 (may not exist) | same |
| `citadel_crate_spawn_initial_delay` | 999999 | same |
| `citadel_crate_respawn_interval` | 999999 | same |
| `kickid <slot>` | command | `LobbyService.KickPlayer` |

### Entity names

| Name | Use | Where |
|---|---|---|
| `citadel_gamerules` | gamerules proxy | `Rift/RiftGameRules.cs` |
| `citadel_item_koth_spawner` | rift spawner | `Rift/RiftService.cs` |
| `citadel_koth_cashin` | live rift | `Rift/RiftService.cs` |
| `npc_trooper` | capture detection, cleanup | `Rift/RiftService.cs` |
| `point_worldtext` | boards | `Modules/WorldText/WorldTextService.cs` |
| `info_koth_spawn_location` | map rift points (2 in the dump) | self-test only |
| `npc_trooper_boss`, `npc_boss_tier2`, `npc_barrack_boss`, `citadel_item_powerup_spawner`, `citadel_herotest_orbspawner`, `citadel_shop_prop_dynamic` | removed | `CleanSlate/CleanSlateService.cs` |
| `trigger_item_shop`, `trigger_item_shop_safe_zone` | disabled | same |
| `info_super_trooper_spawn`, `item_crate_spawn` | never remove | same (comment) |
| `observer` | spectator pawn designer name (seated admin) | `Modules/Spectate/SpectateService.cs` `ObserverDesignerName` |

Map dump counts (build 6698): `info_koth_spawn_location` 2, `info_super_trooper_spawn` 12, `item_crate_spawn` 6, `trigger_item_shop` 9, `trigger_item_shop_safe_zone` 2, `citadel_shop_prop_dynamic` 8, `npc_boss_tier2` 6, `npc_barrack_boss` 12, `citadel_item_powerup_spawner` 2.

### Schema fields (raw)

| Class.field | Type | Where |
|---|---|---|
| `CCitadelGameRulesProxy.m_pGameRules` | pointer | `RiftGameRules.TryResolve` |
| `CCitadelGameRules.m_vNextKothLocation` | Vector3 | `RiftGameRules` |
| `CCitadelGameRules.m_timeNextKothSpawnWindowTime` | float | same |
| `CCitadelGameRules.m_timeNextKothSpawn` | float | same |
| `CCitadelGameRules.m_timeKothGiveUp` | float | same (read only) |

### Modifiers and states

`modifier_citadel_silenced`; `EModifierState.Silenced` (15), `ItemsDisabled` (14), `ShootingDisabled` (62), `MeleeDisabled` (106), `IgnoredByNpcTargeting` (33). Never `Disarmed` (12). `Modules/Restraint/RestraintService.cs`.

### Events and hooks

`player_spawn` (Lobby, Duel, Random), `player_death` (Lobby, Stats, stream camera), `player_used_ability` (stream camera: `Abilityname`, `Player`, `Caster`), `player_respawned` (Duel, Random), `player_hero_changed` (Draft); `OnClientConnect`, `OnClientFullConnect`, `OnClientDisconnect`, `OnClientConCommand`, `OnGameFrame`, `OnModifyCurrency` (GameLoop soul block, counted as `modify_currency`), `OnTakeDamage` (GameLoop up-top damage block, `TakeDamageEvent.Entity`, counted as `take_damage`), `OnLoad`, `OnStartupServer`.

### Enums and hero data

- `Heroes.Skyrunner` is the lobby hero (`LobbyService`, `DraftService`); draft pools in `Draft/DraftPools.cs`.
- `hero-builds.json` (38 heroes, 161 items, 154 abilities), banned items in `Modules/Loadout/LoadoutPlanner.cs`.
- `EAbilitySlot.Signature1..4`, `ECurrencyType.EGold` / `EAbilityPoints` / `EAbilityUnlocks`, `ECurrencySource.ECheats` / `EStartingAmount` / `EItemSale` (the sources `GameLoop/SoulRule` lets through for gold; for ability points and unlocks only `ECheats` passes in Random and 1v1 matches), `ImbueResult.Success`.
- Level table: 36 soul thresholds, unlock rows and 32 points in `Modules/Loadout/Progression.cs`, from the wiki's [Data:SoulUnlockData.json](https://deadlock.wiki/index.php?title=Data:SoulUnlockData.json&action=raw) (+600 each). Upgrade tier costs 1 / 2 / 5 in `LoadoutPlanner.UpgradeCosts`. An economy patch that moves boons or points needs both updated (`ProgressionTests` pins the current values).
- Teams: Amber 2, Sapphire 3, spectator 1.
- Big teamfight ults: 17 ability class names in `Lobby/BigUlts.cs` (each hero's `signature4` from `assets.deadlock-api.com/v2/heroes`, 2026-09-27). Heroes get reworked; re-check after a hero patch.
- Observer: `ObserverMode_t.InEye` (player view) and `Roaming` (free cam); client command `spec_player <slot>` (`clientcmd_can_execute`) as the follow fallback; client command `spec_mode 4` (`clientcmd_can_execute`) puts the client in fly cam before every park (the server mode alone does not). Never `IsValidObserverTarget` (rejects team 3).

### Coordinates (dl_midtown)

- Anchors: `Locations/RiftRouletteLocations.cs`. Watch spots on the skybox floor at z 1536.0625.
- Rift positions: `Rift/RiftSide.cs` (Green (7612, 0, 444), Yellow (-7560, 0, 424)).
- Per-slot offsets: `Round/Data/spots.json`; board offsets in `Round/WatchLayout.cs`, `Draft/BoardLayout.cs`.

---

## 6. Keeping this current

A new game dependency (convar, entity name, schema field, modifier, event)
must be added to `SelfTest/GameDependencies.cs`, to section 5 here, and, if
it is data (hero, item), to `patch-check.py`'s inputs.
