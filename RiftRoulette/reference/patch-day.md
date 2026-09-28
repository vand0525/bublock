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
7. **Refresh data if the check says so**: `python3 scripts/fetch-builds.py` (items, heroes), map dump + `scripts/check-spots.py` (geometry), `reference/cvarlist.md` (convars). After economy or objective changes (soul income, Walker health or bounty, slot unlocks), `python3 scripts/walker-souls.py` and update `Modules/Loadout/ItemSlots` when a breakpoint moved about 1,000 souls or more.
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
| Stream camera (`Lobby/StreamCam`, `Modules/Spectate`) | Seated admin's automatic camera: follow, killer cut, park at the saved framing | `observer` pawn designer name, `ObserverServices` (`InEye`, `Roaming`, `SetObserverTarget`), observer `Teleport`, `CBasePlayerPawn.v_angle` (framing angle; self-test schema), `bublock/streamcam.json` (no client commands: the client refuses `spec_*` from the server) | `dw_spec_status` (`ViewAngle=` changes while turning), `dw_selftest_run` |
| Access (`Lobby/Access*`, `BanStatueService`) | Bans / private mode from `bublock/access.json`; banned players turned to stone then kicked | `OnClientConnect` returning false, `kickid`, `AddModifier` (statue modifier) | `/access_mode`, `/ban_list`, `/ban_modifier` |
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
| Players who leave stay in game as disconnected, pawns standing | a hook method uses an API member whose type changed on the server (Deadworks v0.4.18: `ClientDisconnectedEvent.Reason` int to enum), so the method throws before its first line | self-test Events `client_disconnect` none after a leave; no `Player disconnected` in `lobby-*.log` | copy the server's `managed/DeadworksManaged.Api.dll` into `lib/`, rebuild, upload |
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
| Loadouts stop at 9 items (`Loadout incomplete` `Failed=` 1-3) | team entity renamed, `m_nFlexSlotsUnlocked` renamed / re-typed, flag count changed, or the slot count changed | `dw_lobby_flex` (each team should show 15); self-test Entities `citadel_team_manager` / Schema `CCitadelTeam.m_nFlexSlotsUnlocked`; `lobby-*.log` `Flex slots not unlocked` | new names from `CCitadelTeam.h` / `EFlexSlotTypes_t` into `Lobby/FlexSlots`; update `LoadoutPlanner.DefaultSlots` |
| Loadout level or ranks look wrong for the cap | boon / point thresholds or tier costs changed | `loadout-*.log` `Loadout applied` `Level=` `Points=` `Ranks=` vs the hero panel; wiki soul table | update `Modules/Loadout/Progression.cs` and `LoadoutPlanner.UpgradeCosts`, then `ProgressionTests` |
| Random mode never gives some hero / new hero missing | new hero id not in enum or builds | patch-check Heroes HIT; self-test Heroes WARN | update `lib/`, `fetch-builds.py` |
| Joining fails / players stuck in hero select | `Heroes.Skyrunner` removed or not selectable | patch-check `Skyrunner`; `lobby-*.log` | pick another lobby hero in `LobbyService` / `DraftService` |
| Players fall from the watch spot | skybox floor moved or removed | self-test Map floor WARN (compare to baseline); `watch-*.log` rescues | new map dump; move the watch anchors; `check-spots.py` |
| Players spawn in walls at rift starts | map geometry changed | `/spots_walk sapphire|amber`; `check-spots.py` | new map dump, move anchors / `spots.json` |
| Players can shoot / cast up top | modifier or state renamed / renumbered | self-test live Restraint FAIL; `restraint` Trace `refused` | new names from the schema DB / enum |
| Players take damage up top (turrets, troopers) | `OnTakeDamage` no longer fires or `Stop` no longer blocks | self-test Events `take_damage` = 0 after a fight; `damage_blocked_restrained` stays 0 | check `OnTakeDamage` / `TakeDamageEvent` in `/tmp/dwapi.cs`; fallback state `EModifierState.NoIncomingDamage` (142) in `RestraintService.States` |
| Bosses, shops or urn back on the map | CleanSlate names or crate convars changed | self-test Entities WARN; convar FAIL; `probe-*.log` counts | `dw_ent_find boss` / `shop`; update `CleanSlateService` |
| Players can pause again | pause convar renamed, pause message renamed / renumbered, or a new pause command | self-test Convars WARN; `lobby-*.log` `Pause message hook not registered`; `pause-*.log` has no `Pause blocked` line for the pause | new names from `cvarlist.md` and the new `lib/` into `Lobby/PauseRule` / `LobbyPlugin` |
| Settings not applied (team size, respawn, duplicates) | convar renamed / removed / hidden | self-test Convars FAIL; `Convar missing` warning in master log | new name from `cvarlist.md` upstream |
| Banner or camera angle missing | protobuf message changed | `dw_hud_announce`, `dw_mv_angle` | check the message in the new `lib/` |
| Boards missing | `point_worldtext` / `CPointWorldText` changed | `dw_wt_create test` | check `CPointWorldText` in `/tmp/dwapi.cs` |
| Stream camera saves the wrong framing angle | `v_angle` moved or no longer tracks the observer view | self-test Schema `CBasePlayerPawn.v_angle`; `dw_spec_status` `ViewAngle=`; `lobby-*.log` `framing saved ... AngleRead=` | new field name into `SpectateService.ViewAngle`; `dw_spec_reset` meanwhile |
| Stream camera stuck / not following | observer pawn renamed or observer services changed | `dw_spec_status` (`Observer=False`, `Mode=`); `spectate-*.log` | check `CPlayer_ObserverServices` and the pawn designer name in `/tmp/dwapi.cs` |
| Top-down never moves (camera stays in the directed view) | The admin is not in fly cam (C); the server cannot switch it. If a patch gives a `spec_*` command `server_can_execute`, the server could send it | `lobby-*.log` `Reason=repark` every 6 s; `server_can_execute` in the `patch-check.py` convar diff | press C; or send the newly allowed command from `SpectateService.Park` |

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
| `citadel_hero_demo_unlock_flex_slots` | 1 (did not open the slots alone; `Lobby/FlexSlots` does) | `LobbyService` |
| `citadel_allow_purchasing_anywhere` | 0, or 1 in 1v1 setup | `GameLoop/ShopAccess.cs` |
| `citadel_allow_pausing` | 0 when open, 1 in private mode or after `/pause_allow on` (devonly, replicated) | `Lobby/PauseRule.cs` `ConVars`, set by `PauseGuard.Apply` |
| `citadel_allow_pause_in_match` | 0 when open, 1 in private mode or after `/pause_allow on` | same |
| `citadel_pause_allow_in_pregame` | 0 | same |
| `pause` | command (toggle), automatic unpause | `Lobby/PauseGuard.cs` `Tick` |
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
| `citadel_team_manager` | team entities (`CCitadelTeam`), flex slots written on teams 2 and 3 | `Lobby/FlexSlots.cs` |
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
| `CCitadelTeam.m_nFlexSlotsUnlocked` | `EFlexSlotTypes_t` (uint16 flags, 15 = all four flex slots) | `Lobby/FlexSlots.cs` |
| `CBasePlayerPawn.v_angle` | `QAngle` (read as `Vector3`: pitch, yaw, roll), the observer's view angle | `Modules/Spectate/SpectateService.cs` (`ViewAngle`) |
| `CGameRules.m_bGamePaused`, `CCitadelGameRules.m_bServerPaused` | bool (API `GameRules.GamePaused` / `ServerPaused`) | `Lobby/PauseGuard.cs` |

### Net messages

Incoming, hooked with `NetMessages.HookIncoming` and blocked while pausing is
off: `CCLCMsg_RequestPause` (clc 33) and `CCitadelClientMsg_Pause` (1008).
`Lobby/LobbyPlugin.cs`. A missing ID logs `Pause message hook not registered`
in `lobby-*.log`. Client pause commands blocked in `OnClientConCommand`:
`pause`, `setpause`, `citadel_pause`, `citadel_toggle_server_pause`
(`Lobby/PauseRule.cs`).

### Modifiers and states

`modifier_citadel_silenced`; `EModifierState.Silenced` (15), `ItemsDisabled` (14), `ShootingDisabled` (62), `MeleeDisabled` (106), `IgnoredByNpcTargeting` (33). Never `Disarmed` (12). `Modules/Restraint/RestraintService.cs`.

Banned-player statue: the modifier name is data, not code: `statueModifier`
in `bublock/access.json` on the server (`/ban_modifier`), added by
`Lobby/BanStatueService.cs` with a `duration`. After a patch, if statues
stop turning to stone (Warning `Statue modifier refused` in `access-*.log`),
look up `m_PetrifyModifier` `_class` under `ability_viper_petrifybola` in
GameTracking-Deadlock `scripts/abilities.vdata` (or have someone cast
Vyper's Petrify and read DevTools `modifiers-*.log`), then
`/ban_modifier <name>`. Current: `modifier_citadel_petrify`. It needs Vyper
precached (`LobbyPlugin.OnPrecacheResources`, `Heroes.Viper`) or it shows as
a red wireframe; if the hero enum or the ability moves, update
`BanStatueService.StatueLookHero`.

### Events and hooks

`player_spawn` (Lobby, Duel, Random), `player_death` (Lobby, Stats, stream camera), `player_respawned` (Duel, Random), `player_hero_changed` (Draft); `OnClientConnect`, `OnClientFullConnect`, `OnClientDisconnect`, `OnClientConCommand`, `OnGameFrame`, `OnModifyCurrency` (GameLoop soul block, counted as `modify_currency`), `OnTakeDamage` (GameLoop up-top damage block, `TakeDamageEvent.Entity`, counted as `take_damage`), `OnAddModifier` (DevTools `ModifierProbe`, `AddModifierEvent.ModifierVData.Name`; diagnostics only), `OnLoad`, `OnStartupServer`.

### Enums and hero data

- `Heroes.Skyrunner` is the lobby hero (`LobbyService`, `DraftService`); draft pools in `Draft/DraftPools.cs`.
- `hero-builds.json` (38 heroes, 161 items, 154 abilities, per-build sell priorities), banned items in `Modules/Loadout/LoadoutPlanner.cs`.
- Item slots: universal (any item in any slot), 9 open by default (one flex slot already open), 12 with every flex slot (`LoadoutPlanner.DefaultSlots`); each enemy Walker opens one in a real match, which `Modules/Loadout/ItemSlots` mirrors by soul value (`scripts/walker-souls.py`).
- `EAbilitySlot.Signature1..4`, `ECurrencyType.EGold` / `EAbilityPoints` / `EAbilityUnlocks`, `ECurrencySource.ECheats` / `EStartingAmount` / `EItemSale` (the sources `GameLoop/SoulRule` lets through for gold; for ability points and unlocks only `ECheats` passes in Random and 1v1 matches), `ImbueResult.Success`.
- Level table: 36 soul thresholds, unlock rows and 32 points in `Modules/Loadout/Progression.cs`, from the wiki's [Data:SoulUnlockData.json](https://deadlock.wiki/index.php?title=Data:SoulUnlockData.json&action=raw) (+600 each). Upgrade tier costs 1 / 2 / 5 in `LoadoutPlanner.UpgradeCosts`. An economy patch that moves boons or points needs both updated (`ProgressionTests` pins the current values).
- Teams: Amber 2, Sapphire 3, spectator 1.
- Observer: `ObserverMode_t.InEye` (player view) and `Roaming` (free cam); no client commands: `spec_player` / `spec_mode` are `clientcmd_can_execute`, not `server_can_execute`, and the client refuses them from the server, so parks only move a viewer already in fly cam (C). Never `IsValidObserverTarget` (rejects team 3).

### Coordinates (dl_midtown)

- Anchors: `Locations/RiftRouletteLocations.cs`. Watch spots on the skybox floor at z 1536.0625.
- Rift positions: `Rift/RiftSide.cs` (Green (7612, 0, 444), Yellow (-7560, 0, 424)).
- Per-slot offsets: `Round/Data/spots.json`; board offsets in `Round/WatchLayout.cs`, `Draft/BoardLayout.cs`.

---

## 6. Keeping this current

A new game dependency (convar, entity name, schema field, modifier, event)
must be added to `SelfTest/GameDependencies.cs`, to section 5 here, and, if
it is data (hero, item), to `patch-check.py`'s inputs.
