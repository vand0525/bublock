---
type: generated-reference
generator: scripts/knowledge-graph.py
---

# Indexes

Every command, game dependency, hook and type in the code, with where it lives.
Generated from source; the command catalogs (`reference/user-commands.md`,
`admin-commands.md`) stay the authority for behavior.

## Commands (104)

| Command | Who | Description | File |
|---|---|---|---|
| `/access_mode` | admin | Show join access, or set it: access_mode [open\|private] | `RiftRoulette/Lobby/AccessPlugin.cs` |
| `/allow_add` | admin | Whitelist a Steam64 ID for private mode: allow_add <steamid> | `RiftRoulette/Lobby/AccessPlugin.cs` |
| `/allow_list` | admin | List whitelisted Steam64 IDs | `RiftRoulette/Lobby/AccessPlugin.cs` |
| `/allow_remove` | admin | Remove a Steam64 ID from the whitelist: allow_remove <steamid> | `RiftRoulette/Lobby/AccessPlugin.cs` |
| `/balance_auto` | admin | Turn auto-balance on or off: balance_auto <on\|off> | `RiftRoulette/Balance/BalancePlugin.cs` |
| `/balance_now` | admin | Swap the leading team's best player now and reroll heroes (Random mode intermission) | `RiftRoulette/Balance/BalancePlugin.cs` |
| `/balance_status` | admin | Show auto-balance counters since the last swap and the current verdict | `RiftRoulette/Balance/BalancePlugin.cs` |
| `/ban_add` | admin | Ban a Steam64 ID: ban_add <steamid> | `RiftRoulette/Lobby/AccessPlugin.cs` |
| `/ban_list` | admin | List banned Steam64 IDs | `RiftRoulette/Lobby/AccessPlugin.cs` |
| `/ban_remove` | admin | Unban a Steam64 ID: ban_remove <steamid> | `RiftRoulette/Lobby/AccessPlugin.cs` |
| `/bet_status` | admin | Show every player's betting chips and open bets | `RiftRoulette/Betting/BettingPlugin.cs` |
| `/cleanup_run` | admin | Re-apply the spawn convars, remove lane bosses, spawners and shop kiosks, disable shop zones | `CleanSlate/CleanSlatePlugin.cs` |
| `/dev_herowatch` | admin | Toggle logging of your hero id changes | `DevTools/DevToolsPlugin.cs` |
| `/dev_logpath` | admin | Show where this server writes Bublock logs | `DevTools/DevToolsPlugin.cs` |
| `/draft_assign` | admin | Pick a hero for a player: draft_assign <slot> <hero> | `RiftRoulette/Draft/DraftPlugin.cs` |
| `/draft_boards` | admin | Redraw the draft boards | `RiftRoulette/Draft/DraftPlugin.cs` |
| `/draft_release` | admin | Unpick for a player: draft_release <slot> | `RiftRoulette/Draft/DraftPlugin.cs` |
| `/draft_reset` | admin | Clear the whole draft and return everyone to the lobby | `RiftRoulette/Draft/DraftPlugin.cs` |
| `/draft_status` | admin | Show both pools and every pick with player and slot | `RiftRoulette/Draft/DraftPlugin.cs` |
| `/duel_clear` | admin | 1v1: drop the copied build and go back to free hero switching (no match running) | `RiftRoulette/Duel/DuelPlugin.cs` |
| `/duel_copy` | admin | 1v1: copy this player's exact hero, items, abilities and level onto both players and start: duel_copy <slot> | `RiftRoulette/Duel/DuelPlugin.cs` |
| `/duel_queue` | admin | 1v1: list the queue, fighters and the king's streak | `RiftRoulette/Duel/DuelPlugin.cs` |
| `/duel_queue_add` | admin | 1v1: put a player at the back of the queue: duel_queue_add <slot> | `RiftRoulette/Duel/DuelPlugin.cs` |
| `/duel_queue_remove` | admin | 1v1: take a player out of the queue (not mid-fight): duel_queue_remove <slot> | `RiftRoulette/Duel/DuelPlugin.cs` |
| `/duel_status` | admin | 1v1: show the copied build, lock state and competitors | `RiftRoulette/Duel/DuelPlugin.cs` |
| `/ent_diff` | admin | List entities added or removed since ent_snapshot | `DevTools/DevToolsPlugin.cs` |
| `/ent_find` | admin | List entities whose designer, class, or name contains a filter: ent_find <filter> | `DevTools/DevToolsPlugin.cs` |
| `/ent_info` | admin | Show fields of every entity with a designer name: ent_info <designerName> | `DevTools/DevToolsPlugin.cs` |
| `/ent_remove` | admin | Remove every entity with a designer name: ent_remove <designerName> | `DevTools/DevToolsPlugin.cs` |
| `/ent_snapshot` | admin | Remember every entity for a later ent_diff | `DevTools/DevToolsPlugin.cs` |
| `/gungame_reroll` | admin | Give a player a new random hero and build now, as a Gun Game kill does: gungame_reroll <slot> | `RiftRoulette/GunGame/GunGamePlugin.cs` |
| `/gungame_status` | admin | Gun Game: on or off, target, winner, every player's kills | `RiftRoulette/GunGame/GunGamePlugin.cs` |
| `/gungame_target` | admin | Set the Gun Game kill target between matches: gungame_target <1-50> | `RiftRoulette/GunGame/GunGamePlugin.cs` |
| `/hud_announce` | admin | Show an on-screen banner to everyone: hud_announce <title> [\| description] | `Modules/Hud/HudPlugin.cs` |
| `/hud_say` | admin | Say something to everyone as a big on-screen banner: hud_say <message> | `Modules/Hud/HudPlugin.cs` |
| `/loadout_copy` | admin | Copy one player's exact hero, items, ability upgrades and level onto another: loadout_copy <from> <to> | `Modules/Loadout/LoadoutPlugin.cs` |
| `/loadout_give` | admin | Swap a player to a hero and apply a top build: loadout_give <slot> <hero> [build 1-3] | `Modules/Loadout/LoadoutPlugin.cs` |
| `/loadout_info` | admin | Show the build data date, source, hero count, baseline value, and cap | `Modules/Loadout/LoadoutPlugin.cs` |
| `/loadout_list` | admin | List the stored top builds for a hero: loadout_list <hero> | `Modules/Loadout/LoadoutPlugin.cs` |
| `/lobby_setup` | admin | Re-apply the Rift Roulette server convars | `RiftRoulette/Lobby/LobbyPlugin.cs` |
| `/match_auto` | admin | Turn match auto-start on or off: match_auto <on\|off> | `RiftRoulette/GameLoop/GameLoopPlugin.cs` |
| `/match_config` | admin | Show the match configuration (hero mode, format, intermission) | `RiftRoulette/GameLoop/GameLoopPlugin.cs` |
| `/match_end` | admin | End the match, show the final score, and return everyone to the lobby | `RiftRoulette/GameLoop/GameLoopPlugin.cs` |
| `/match_format` | admin | Set the match format: match_format <continuous> | `RiftRoulette/GameLoop/GameLoopPlugin.cs` |
| `/match_intermission` | admin | Set the seconds between rounds: match_intermission <seconds> | `RiftRoulette/GameLoop/GameLoopPlugin.cs` |
| `/match_mode` | admin | Set how heroes are chosen: match_mode <random\|draft\|duel\|1v1> | `RiftRoulette/GameLoop/GameLoopPlugin.cs` |
| `/match_start` | admin | Start a continuous match; rounds then run themselves | `RiftRoulette/GameLoop/GameLoopPlugin.cs` |
| `/match_status` | admin | Show match phase, round, score, and intermission length | `RiftRoulette/GameLoop/GameLoopPlugin.cs` |
| `/mv_angle` | admin | Set a camera angle: mv_angle <pitch> <yaw> <roll> [slot] | `Modules/Movement/MovementPlugin.cs` |
| `/mv_list` | admin | List named teleport locations | `Modules/Movement/MovementPlugin.cs` |
| `/mv_remove` | admin | Remove a location saved with mv_save: mv_remove <name> | `Modules/Movement/MovementPlugin.cs` |
| `/mv_save` | admin | Save your position and view as a location until reload: mv_save <name> | `Modules/Movement/MovementPlugin.cs` |
| `/mv_tp` | admin | Teleport yourself or a slot to a location: mv_tp <location> [slot] | `Modules/Movement/MovementPlugin.cs` |
| `/mv_tp_all` | admin | Teleport every player to a location: mv_tp_all <location> | `Modules/Movement/MovementPlugin.cs` |
| `/mv_tp_team` | admin | Teleport a team to a location: mv_tp_team <team number> <location> | `Modules/Movement/MovementPlugin.cs` |
| `/mv_where` | admin | Show a player's position and view angle: mv_where [slot] | `Modules/Movement/MovementPlugin.cs` |
| `/player_ban` | admin | Ban a connected player by slot and kick them: player_ban <slot> | `RiftRoulette/Lobby/AccessPlugin.cs` |
| `/player_info` | admin | Show one player's status: player_info <slot> | `RiftRoulette/Lobby/LobbyPlugin.cs` |
| `/player_kick` | admin | Kick a player and release their pick: player_kick <slot> | `RiftRoulette/Lobby/LobbyPlugin.cs` |
| `/player_list` | admin | List every player with team, hero, pick, and health | `RiftRoulette/Lobby/LobbyPlugin.cs` |
| `/player_team` | admin | Move a player without a pick to a team: player_team <slot> <sapphire\|amber> | `RiftRoulette/Lobby/LobbyPlugin.cs` |
| `/random_reroll` | admin | Give everyone a new random hero and build now (intermission only) | `RiftRoulette/RandomMode/RandomPlugin.cs` |
| `/random_status` | admin | Show Random mode teams, heroes, builds, and pending swaps | `RiftRoulette/RandomMode/RandomPlugin.cs` |
| `/restrain` | admin | Silence, disarm and block melee for a player until released: restrain <slot> | `Modules/Restraint/RestraintPlugin.cs` |
| `/restrain_list` | admin | List restrained players and their active restraint modifiers | `Modules/Restraint/RestraintPlugin.cs` |
| `/restrain_release` | admin | Remove silence, disarm and the melee block from a player: restrain_release <slot> | `Modules/Restraint/RestraintPlugin.cs` |
| `/rift_cancel` | admin | End the running rift round and return players to draft (a spawned rift stays) | `RiftRoulette/Rift/RiftPlugin.cs` |
| `/rift_cleanup` | admin | Remove every rift trooper on the map | `RiftRoulette/Rift/RiftPlugin.cs` |
| `/rift_next` | admin | Set the next rift side: rift_next <green\|yellow> | `RiftRoulette/Rift/RiftPlugin.cs` |
| `/rift_start` | admin | Start the next rift (green/yellow alternating) | `RiftRoulette/Rift/RiftPlugin.cs` |
| `/rift_status` | admin | Show rift phase, current and next side, last outcome | `RiftRoulette/Rift/RiftPlugin.cs` |
| `/seat_play` | admin | Admin seat: move the admin from spectator onto a team (console: dw_seat_play) | `RiftRoulette/Lobby/LobbyPlugin.cs` |
| `/seat_spec` | admin, ConsoleOnly | Admin seat: move the admin to spectator, any time; console only (dw_seat_spec) | `RiftRoulette/Lobby/LobbyPlugin.cs` |
| `/seat_status` | admin | Show player slots, the admin seat, and maxplayers | `RiftRoulette/Lobby/LobbyPlugin.cs` |
| `/selftest_live` | admin | Teleport, restrain, banner and loadout check on one player, between rounds: selftest_live <slot> | `RiftRoulette/SelfTest/SelfTestPlugin.cs` |
| `/selftest_run` | admin | Check every game dependency (convars, schema, entities, heroes, items, floors, events): selftest_run [all] | `RiftRoulette/SelfTest/SelfTestPlugin.cs` |
| `/session_info` | admin | Show the Rift Roulette session id, round, map, and log folder | `RiftRoulette/Session/SessionPlugin.cs` |
| `/spec_auto` | admin | Stream camera: automatic follow / top-down on or off: spec_auto <on\|off> | `RiftRoulette/Lobby/LobbyPlugin.cs` |
| `/spec_overview` | admin | Stream camera: show the top-down view over the rift now for 10 s | `RiftRoulette/Lobby/LobbyPlugin.cs` |
| `/spec_status` | admin | Stream camera: who is on camera, top-down state, and the round | `RiftRoulette/Lobby/LobbyPlugin.cs` |
| `/spots_list` | admin | List every slot's watch and rift start spots: spots_list [green\|yellow] | `RiftRoulette/Round/SpotsPlugin.cs` |
| `/spots_walk` | admin | Teleport yourself through each slot spot and log where you land: spots_walk <watch\|sapphire\|amber> [green\|yellow] | `RiftRoulette/Round/SpotsPlugin.cs` |
| `/stats_board` | admin | Redraw the Sapphire and Amber stats boards (Random mode) | `RiftRoulette/Stats/StatsPlugin.cs` |
| `/stats_reset` | admin | Zero every player's match kills, deaths, and assists | `RiftRoulette/Stats/StatsPlugin.cs` |
| `/status_add` | admin | Test a game modifier by name on a player: status_add <slot> <modifier> [seconds] | `Modules/Restraint/RestraintPlugin.cs` |
| `/status_remove` | admin | Remove a game modifier by name from a player: status_remove <slot> <modifier> | `Modules/Restraint/RestraintPlugin.cs` |
| `/wt_clear` | admin | Remove every text board on the map | `Modules/WorldText/WorldTextPlugin.cs` |
| `/wt_create` | admin | Create a text board in front of you: wt_create <id> <text> | `Modules/WorldText/WorldTextPlugin.cs` |
| `/wt_list` | admin | List text boards created through WorldText | `Modules/WorldText/WorldTextPlugin.cs` |
| `/wt_remove` | admin | Remove one text board: wt_remove <id> | `Modules/WorldText/WorldTextPlugin.cs` |
| `/wt_update` | admin | Change a board's text: wt_update <id> <text> | `Modules/WorldText/WorldTextPlugin.cs` |
| `/bet` | player | Bet all your chips on a team for the next round: bet <sapphire\|amber> | `RiftRoulette/Betting/BettingPlugin.cs` |
| `/chips` | player | Show your betting chips and your bet | `RiftRoulette/Betting/BettingPlugin.cs` |
| `/commands` | player | List the commands players can use | `RiftRoulette/Lobby/LobbyPlugin.cs` |
| `/heroes` | player | Show both hero pools and which heroes are taken | `RiftRoulette/Draft/DraftPlugin.cs` |
| `/ladder` | player | Show the Gun Game kill ladder and your place | `RiftRoulette/GunGame/GunGamePlugin.cs` |
| `/pick` | player | Draft a hero: pick <hero> | `RiftRoulette/Draft/DraftPlugin.cs` |
| `/picks` | player | List drafted heroes and who picked them | `RiftRoulette/Draft/DraftPlugin.cs` |
| `/queue` | player | 1v1: join the queue (winner stays on), or show your place in it | `RiftRoulette/Duel/DuelPlugin.cs` |
| `/score` | player | Show the match score | `RiftRoulette/GameLoop/GameLoopPlugin.cs` |
| `/stats` | player | Show your kills, deaths, and assists this match | `RiftRoulette/Stats/StatsPlugin.cs` |
| `/status` | player | Show your slot, team, hero, pick, and health | `RiftRoulette/Lobby/LobbyPlugin.cs` |
| `/unpick` | player | Give your drafted hero back | `RiftRoulette/Draft/DraftPlugin.cs` |
| `/unqueue` | player | 1v1: leave the queue | `RiftRoulette/Duel/DuelPlugin.cs` |

## Game dependencies

What the code touches in the game. A new entry here needs a self-test and a row in
`patch-day.md` §5.

### Game events (5)

| Name | Used in |
|---|---|
| `player_death` | `RiftRoulette/Lobby/LobbyPlugin.cs`, `RiftRoulette/Stats/StatsPlugin.cs` |
| `player_hero_changed` | `RiftRoulette/Draft/DraftPlugin.cs` |
| `player_respawned` | `RiftRoulette/Duel/DuelPlugin.cs`, `RiftRoulette/RandomMode/RandomPlugin.cs` |
| `player_spawn` | `RiftRoulette/Duel/DuelPlugin.cs`, `RiftRoulette/Lobby/LobbyPlugin.cs`, `RiftRoulette/RandomMode/RandomPlugin.cs` |
| `player_used_ability` | `RiftRoulette/Lobby/LobbyPlugin.cs` |

### Hooks (11)

| Name | Used in |
|---|---|
| `OnChatMessage` | `RiftRoulette/Betting/BettingPlugin.cs` |
| `OnClientConCommand` | `RiftRoulette/GameLoop/GameLoopPlugin.cs` |
| `OnClientConnect` | `RiftRoulette/Lobby/LobbyPlugin.cs` |
| `OnClientDisconnect` | `RiftRoulette/Lobby/LobbyPlugin.cs` |
| `OnClientFullConnect` | `RiftRoulette/Lobby/LobbyPlugin.cs` |
| `OnGameFrame` | `Modules/Restraint/RestraintPlugin.cs`, `RiftRoulette/GameLoop/GameLoopPlugin.cs` |
| `OnLoad` | `CleanSlate/CleanSlatePlugin.cs`, `DevTools/DevToolsPlugin.cs`, `RiftRoulette/Draft/DraftPlugin.cs`, `RiftRoulette/GameLoop/GameLoopPlugin.cs`, `RiftRoulette/Lobby/AccessPlugin.cs`, `RiftRoulette/Lobby/LobbyPlugin.cs`, `RiftRoulette/Session/SessionPlugin.cs` |
| `OnModifyCurrency` | `RiftRoulette/GameLoop/GameLoopPlugin.cs` |
| `OnStartupServer` | `CleanSlate/CleanSlatePlugin.cs`, `DevTools/DevToolsPlugin.cs`, `RiftRoulette/Draft/DraftPlugin.cs`, `RiftRoulette/Lobby/LobbyPlugin.cs`, `RiftRoulette/Session/SessionPlugin.cs` |
| `OnTakeDamage` | `RiftRoulette/GameLoop/GameLoopPlugin.cs` |
| `OnUnload` | `DevTools/DevToolsPlugin.cs`, `RiftRoulette/Session/SessionPlugin.cs` |

### Convars (16)

| Name | Used in |
|---|---|
| `citadel_active_lane` | `CleanSlate/CleanSlateService.cs`, `RiftRoulette/SelfTest/GameDependencies.cs` |
| `citadel_allow_duplicate_heroes` | `RiftRoulette/Lobby/LobbyService.cs`, `RiftRoulette/SelfTest/GameDependencies.cs` |
| `citadel_allow_purchasing_anywhere` | `RiftRoulette/GameLoop/ShopAccess.cs`, `RiftRoulette/SelfTest/GameDependencies.cs` |
| `citadel_crate_disable_early_spawn` | `CleanSlate/CleanSlateService.cs`, `RiftRoulette/SelfTest/GameDependencies.cs` |
| `citadel_crate_respawn_interval` | `CleanSlate/CleanSlateService.cs`, `RiftRoulette/SelfTest/GameDependencies.cs` |
| `citadel_crate_spawn_enabled` | `CleanSlate/CleanSlateService.cs`, `RiftRoulette/SelfTest/GameDependencies.cs` |
| `citadel_crate_spawn_initial_delay` | `CleanSlate/CleanSlateService.cs`, `RiftRoulette/SelfTest/GameDependencies.cs` |
| `citadel_koth_early_warning_time` | `RiftRoulette/SelfTest/GameDependencies.cs` |
| `citadel_koth_enabled` | `RiftRoulette/Lobby/LobbyService.cs`, `RiftRoulette/Rift/RiftGameRules.cs`, `RiftRoulette/SelfTest/GameDependencies.cs` |
| `citadel_koth_warning_time` | `RiftRoulette/SelfTest/GameDependencies.cs` |
| `citadel_midboss_initial_spawn_time_override` | `CleanSlate/CleanSlateService.cs`, `RiftRoulette/SelfTest/GameDependencies.cs` |
| `citadel_npc_spawn_enabled` | `CleanSlate/CleanSlateService.cs`, `RiftRoulette/SelfTest/GameDependencies.cs` |
| `citadel_player_override_spawn_time` | `RiftRoulette/SelfTest/GameDependencies.cs` |
| `citadel_team_size` | `RiftRoulette/Lobby/LobbyService.cs`, `RiftRoulette/SelfTest/GameDependencies.cs` |
| `citadel_trooper_spawn_enabled` | `CleanSlate/CleanSlateService.cs`, `RiftRoulette/SelfTest/GameDependencies.cs` |
| `sv_cheats` | `Shared/Cheats/Cheats.cs` |

### Entities (designer names) (17)

| Name | Used in |
|---|---|
| `citadel_gamerules` | `RiftRoulette/Rift/RiftGameRules.cs`, `RiftRoulette/SelfTest/GameDependencies.cs` |
| `citadel_herotest_orbspawner` | `CleanSlate/CleanSlateService.cs`, `RiftRoulette/SelfTest/GameDependencies.cs` |
| `citadel_item_koth_spawner` | `RiftRoulette/Rift/RiftService.cs` |
| `citadel_item_powerup_spawner` | `CleanSlate/CleanSlateService.cs`, `RiftRoulette/SelfTest/GameDependencies.cs` |
| `citadel_koth_cashin` | `RiftRoulette/Rift/RiftService.cs` |
| `citadel_shop_prop_dynamic` | `CleanSlate/CleanSlateService.cs`, `RiftRoulette/GameLoop/MatchProbe.cs`, `RiftRoulette/SelfTest/GameDependencies.cs` |
| `info_koth_spawn_location` | `RiftRoulette/SelfTest/GameDependencies.cs` |
| `info_super_trooper_spawn` | `RiftRoulette/SelfTest/GameDependencies.cs` |
| `item_crate_spawn` | `RiftRoulette/SelfTest/GameDependencies.cs` |
| `npc_barrack_boss` | `CleanSlate/CleanSlateService.cs`, `RiftRoulette/GameLoop/MatchProbe.cs`, `RiftRoulette/SelfTest/GameDependencies.cs` |
| `npc_boss_tier2` | `CleanSlate/CleanSlateService.cs`, `RiftRoulette/GameLoop/MatchProbe.cs`, `RiftRoulette/SelfTest/GameDependencies.cs` |
| `npc_trooper` | `RiftRoulette/Rift/RiftService.cs` |
| `npc_trooper_boss` | `CleanSlate/CleanSlateService.cs`, `RiftRoulette/GameLoop/MatchProbe.cs`, `RiftRoulette/SelfTest/GameDependencies.cs` |
| `observer` | `Modules/Spectate/SpectateService.cs` |
| `point_worldtext` | `Modules/WorldText/WorldTextService.cs` |
| `trigger_item_shop` | `CleanSlate/CleanSlateService.cs`, `RiftRoulette/SelfTest/GameDependencies.cs` |
| `trigger_item_shop_safe_zone` | `CleanSlate/CleanSlateService.cs`, `RiftRoulette/SelfTest/GameDependencies.cs` |

### Modifiers (1)

| Name | Used in |
|---|---|
| `modifier_citadel_silenced` | `Modules/Restraint/RestraintService.cs` |

### Modifier states (5)

| Name | Used in |
|---|---|
| `EModifierState.IgnoredByNpcTargeting` | `Modules/Restraint/RestraintService.cs` |
| `EModifierState.ItemsDisabled` | `Modules/Restraint/RestraintService.cs` |
| `EModifierState.MeleeDisabled` | `Modules/Restraint/RestraintService.cs` |
| `EModifierState.ShootingDisabled` | `Modules/Restraint/RestraintService.cs` |
| `EModifierState.Silenced` | `Modules/Restraint/RestraintService.cs` |

### Schema fields (5)

| Name | Used in |
|---|---|
| `m_pGameRules` | `RiftRoulette/Rift/RiftGameRules.cs`, `RiftRoulette/SelfTest/SelfTestService.cs` |
| `m_timeKothGiveUp` | `RiftRoulette/Rift/RiftGameRules.cs` |
| `m_timeNextKothSpawn` | `RiftRoulette/Rift/RiftGameRules.cs` |
| `m_timeNextKothSpawnWindowTime` | `RiftRoulette/Rift/RiftGameRules.cs` |
| `m_vNextKothLocation` | `RiftRoulette/Rift/RiftGameRules.cs` |

### Abilities (7)

| Name | Used in |
|---|---|
| `citadel_ability_bebop_laser_beam` | `RiftRoulette/Lobby/BigUlts.cs` |
| `citadel_ability_bull_leap` | `RiftRoulette/Lobby/BigUlts.cs` |
| `citadel_ability_lash_ultimate` | `RiftRoulette/Lobby/BigUlts.cs` |
| `citadel_ability_rocket_barrage` | `RiftRoulette/Lobby/BigUlts.cs` |
| `citadel_ability_self_vacuum` | `RiftRoulette/Lobby/BigUlts.cs` |
| `citadel_ability_storm_cloud` | `RiftRoulette/Lobby/BigUlts.cs` |
| `citadel_ability_tengu_airlift` | `RiftRoulette/Lobby/BigUlts.cs` |

### Net messages (2)

| Name | Used in |
|---|---|
| `CCitadelUserMsg_ChatMsg` | `Shared/Chat/PlayerChat.cs` |
| `CCitadelUserMsg_SetClientCameraAngles` | `Modules/Movement/MovementService.cs` |

## Stages (28)

From `RiftRoulette/reference/master-plan.md`; each links to the code its entry names.

| Stage | Status | Touches |
|---|---|---|
| Stage 1: RiftRoulette project shell + deploy tooling | done |  |
| Stage 2: Multi-plugin shell (DevTools + CleanSlate) | done |  |
| Stage 3: Shared foundation + Legacy move | done | DevToolsPlugin, Shared |
| Stage 4: Logging foundation (Shared) | done | SessionPlugin, Shared, pull-logs.sh, test.sh |
| Stage 5: Behavioral inventory + module map | done | Bublock — Behavioral Inventory (Stage 5) |
| Stage 6: WorldText module | done | point_worldtext, WorldText |
| Stage 7: Movement module | done | Movement |
| Stage 8: Lobby plugin class (RiftRoulette) | done | Lobby, OnStartupServer |
| Stage 9: Draft plugin class (RiftRoulette) | done | /heroes, /pick, /picks, /unpick, player_hero_changed, Draft |
| Stage 10: Rift plugin class (RiftRoulette) | done | Rift |
| Stage 11: Parity composition | done |  |
| Stage 12: Parity harden + docs pass | done | /cleanup_run, Stage 12 — Parity review (archive vs Bublock), pull-logs.sh |
| Stage 13: New gameplay (only then) | open |  |
| Stage 13a: Playtest match loop | awaiting playtest | /hud_announce, /match_end, /match_intermission, /match_start, /match_status, /score, GameLoop, HudService, GameLoopPlugin, MatchService, MatchState, RiftRoundResult ... |
| Stage 13b: Random mode with real builds | awaiting playtest | /loadout_give, /loadout_info, /loadout_list, /match_config, /match_end, /match_format, /match_mode, /match_start, /random_reroll, /random_status, player_respawned, RandomMode ... |
| Stage 13c: Match stats boards, auto-balance, balanced joins | awaiting playtest | /balance_auto, /balance_now, /balance_status, /stats, /stats_board, /stats_reset, player_death, player_spawn, Balance, RandomMode, Stats, HeroBuildCatalog ... |
| Stage 13d: Auto-start the match | awaiting playtest | /match_auto, /match_end, /match_status, AutoStartRule, AutoStartService, GameLoopPlugin, LobbyService, AutoStartRuleTests |
| Stage 13e: Ban Cultist Sacrifice | awaiting playtest | /loadout_list, LoadoutPlanner, HeroBuildCatalogTests, fetch-builds.py |
| Stage 13f: Admin seat (13th connection) | awaiting playtest | /seat_play, /seat_spec, /seat_status, player_spawn, AdminSeat, AdminSeatRule, LobbyPlugin, Participants, RandomModeService, AdminSeatRuleTests, OnClientConnect |
| Stage 13g: 1v1 mode (exact copy, locked) | awaiting playtest | /duel_clear, /duel_copy, /duel_status, /loadout_copy, /match_mode, Duel, LoadoutService, LoadoutSnapshot, DraftService, DuelPlugin, DuelService, AutoStartService ... |
| Stage 13h: Restrain inactive players | awaiting playtest | /restrain, /restrain_list, /restrain_release, /status_add, /status_remove, RestraintPlugin, RestraintService, RoundFlow, OnGameFrame, modifier_citadel_silenced, Restraint |
| Stage 13i: Watch spot above the rift | awaiting playtest | /rift_next, BoardLayout, RiftRouletteLocations, RoundLocations, WatchSpot, WatchSpotRule, RoundLocationsTests, WatchSpotRuleTests |
| Stage 13j: 1v1 queue, winner stays on | awaiting playtest | /duel_queue, /duel_queue_add, /duel_queue_remove, /queue, /unqueue, PlayerQueue, DuelService, KothRule, AutoStartService, MatchService, RoundFlow, PlayerQueueTests ... |
| Playtest fixes for 13h-13j | awaiting playtest | MatchProbe, ShopAccess, ShopRule, WatchGuard, WatchLayout, RoundLocationsTests, ShopRuleTests, WatchGuardRuleTests, WatchLayoutTests, OnLoad |
| Quiet banners, reload, leftover rift | awaiting playtest | /match_end, citadel_koth_cashin, RiftService, RiftSide, RiftSidesTests |
| Per-slot spots | awaiting playtest | /spots_list, /spots_walk, MovementLocation, RoundFlow, SlotSpots, SpotCheck, SpotsPlugin, WatchSpot, MovementLocationTests, SlotSpotsTests, check-spots.py |
| Patch-day readiness | open | /selftest_live, /selftest_run, Patch day runbook, SelfTest, HeroBuildCatalog, ServerConVars, patch-baseline.sh, patch-check.py |
| Stage 13k: Gun Game format (redock fork) | open | /gungame_reroll, /gungame_status, /gungame_target, /ladder, /match_format, AutoStartService, MatchConfig, MatchService, GunGameLadder, GunGamePlugin, GunGameService, RandomModeService ... |

## Game modes (9)

From `knowledge/game-mode-recipes.md`; each links to the levers and code it uses.

| Mode | Status | Uses |
|---|---|---|
| 1v1 winner stays on (duel mode) | shipped | LoadoutSnapshot, PlayerQueue, HeroLock, Queue |
| Rift Roulette (random mode) | shipped | Balance, MatchService, SoulRule, BenchRule, RiftGameRules, OnTakeDamage, Loadout, Restraint |
| Gun Game | in progress | /gungame_target, /ladder, /match_format, /match_mode, Bublock — Master Plan, player_death, GunGame, LoadoutService, AutoStartService, MatchService, SoulRule, GunGameService, RandomModeService, StatsService, Stage 13k: Gun Game format (redock fork) |
| Endless mid-rift | designed | Endless mid-rift mode (shelved proposal) |
| Fat Boy / Zombie Escape | designed | Balance, HeroLock, RandomModeService, RiftService, OnTakeDamage, Loadout |
| Grifball | designed | /status_add, citadel_player_override_spawn_time, player_death, player_spawn, HeroLock, OnGameFrame, OnTakeDamage, EModifierState.ShootingDisabled, Loadout |
| Hold the zone | designed |  |
| Juggernaut | designed | player_death, LoadoutService, OnTakeDamage |
| Last team standing | designed | citadel_player_override_spawn_time, player_spawn, RoundFlow, WatchSpot |

## Types (174)

| Type | File | Summary |
|---|---|---|
| `CleanSlatePlugin` | `CleanSlate/CleanSlatePlugin.cs` | Thin plugin host for map cleanup: disables trooper/NPC/midboss/urn spawning, removes lane bosses, powerup spawners and shop kiosks, and disables the shop buy zones shortly after startup. |
| `CleanSlateService` | `CleanSlate/CleanSlateService.cs` | Static service holding the CleanSlate map-cleanup operations. |
| `CleanupResult` | `CleanSlate/CleanupResult.cs` | Immutable result of one `CleanSlateService.RemoveMapEntities` run. |
| `DevToolsPlugin` | `DevTools/DevToolsPlugin.cs` | Deadworks admin/diagnostics plugin: entity find/inspect/remove, snapshot and diff, hero watch, log path. |
| `HudPlugin` | `Modules/Hud/HudPlugin.cs` | Thin Deadworks plugin class (`Name` = `Hud`) exposing the admin `/hud_announce` and `/hud_say` commands. |
| `HudService` | `Modules/Hud/HudService.cs` | Static service that shows the game's on-screen announcement banner (title plus smaller description) to one player or to everyone. |
| `HeroBuildCatalog` | `Modules/Loadout/HeroBuildCatalog.cs` | Lookup over `HeroBuildData`: builds per hero, the playable hero pool, display names, and item components. |
| `AbilityStep` | `Modules/Loadout/HeroBuildData.cs` | Records for the build data file (`Data/hero-builds.json`, written by `scripts/fetch-builds.py`) and its JSON parser. |
| `BuildCategory` | `Modules/Loadout/HeroBuildData.cs` | Records for the build data file (`Data/hero-builds.json`, written by `scripts/fetch-builds.py`) and its JSON parser. |
| `HeroBuild` | `Modules/Loadout/HeroBuildData.cs` | Records for the build data file (`Data/hero-builds.json`, written by `scripts/fetch-builds.py`) and its JSON parser. |
| `HeroBuildData` | `Modules/Loadout/HeroBuildData.cs` | Records for the build data file (`Data/hero-builds.json`, written by `scripts/fetch-builds.py`) and its JSON parser. |
| `HeroBuildSet` | `Modules/Loadout/HeroBuildData.cs` | Records for the build data file (`Data/hero-builds.json`, written by `scripts/fetch-builds.py`) and its JSON parser. |
| `AbilityPlan` | `Modules/Loadout/LoadoutPlanner.cs` | Pure planning for a build: which items to grant and which upgrade bits to set on each ability. |
| `LoadoutPlanner` | `Modules/Loadout/LoadoutPlanner.cs` | Pure planning for a build: which items to grant and which upgrade bits to set on each ability. |
| `LoadoutPlugin` | `Modules/Loadout/LoadoutPlugin.cs` | Thin admin command host for the Loadout module. |
| `LoadoutOptions` | `Modules/Loadout/LoadoutService.cs` | Applies a stored hero build to a live pawn, and swaps a player to a hero and then applies a build. |
| `LoadoutResult` | `Modules/Loadout/LoadoutService.cs` | Applies a stored hero build to a live pawn, and swaps a player to a hero and then applies a build. |
| `LoadoutService` | `Modules/Loadout/LoadoutService.cs` | Applies a stored hero build to a live pawn, and swaps a player to a hero and then applies a build. |
| `LoadoutSnapshot` | `Modules/Loadout/LoadoutSnapshot.cs` | Plain records describing one player's exact hero state, used to copy a hero from one player onto another (`LoadoutService.Capture` / `ApplySnapshot`). |
| `SnapshotAbility` | `Modules/Loadout/LoadoutSnapshot.cs` | Plain records describing one player's exact hero state, used to copy a hero from one player onto another (`LoadoutService.Capture` / `ApplySnapshot`). |
| `SnapshotItem` | `Modules/Loadout/LoadoutSnapshot.cs` | Plain records describing one player's exact hero state, used to copy a hero from one player onto another (`LoadoutService.Capture` / `ApplySnapshot`). |
| `SnapshotResult` | `Modules/Loadout/LoadoutSnapshot.cs` | Plain records describing one player's exact hero state, used to copy a hero from one player onto another (`LoadoutService.Capture` / `ApplySnapshot`). |
| `Progression` | `Modules/Loadout/Progression.cs` | Deadlock's level table: how many boons, ability unlocks and ability points a hero has at a given soul count. |
| `ProgressionLevel` | `Modules/Loadout/Progression.cs` | Deadlock's level table: how many boons, ability unlocks and ability points a hero has at a given soul count. |
| `Entry` | `Modules/Movement/LocationRegistry.cs` | Pure name -> `MovementLocation` registry. |
| `Listing` | `Modules/Movement/LocationRegistry.cs` | Pure name -> `MovementLocation` registry. |
| `LocationRegistry` | `Modules/Movement/LocationRegistry.cs` | Pure name -> `MovementLocation` registry. |
| `MovementLocation` | `Modules/Movement/MovementLocation.cs` | Named teleport target. |
| `MovementPlugin` | `Modules/Movement/MovementPlugin.cs` | Thin Deadworks plugin class (`Name` = `Movement`) exposing the admin `/mv_*` commands. |
| `MovementService` | `Modules/Movement/MovementService.cs` | Core teleport and camera operations. |
| `PlayerQueue` | `Modules/Queue/PlayerQueue.cs` | An ordered line of unique Steam IDs. |
| `RestraintPlugin` | `Modules/Restraint/RestraintPlugin.cs` | Thin host for `RestraintService`: the per-frame hook and admin commands. |
| `RestraintService` | `Modules/Restraint/RestraintService.cs` | Keeps chosen players silenced and unable to use items, shoot or melee until they are released (Stage 13h), and ignored by NPC targeting. |
| `SpectateChoice` | `Modules/Spectate/SpectateRule.cs` | Pure decisions for an automatic spectator camera. |
| `SpectateReason` | `Modules/Spectate/SpectateRule.cs` | Pure decisions for an automatic spectator camera. |
| `SpectateRule` | `Modules/Spectate/SpectateRule.cs` | Pure decisions for an automatic spectator camera. |
| `SpectateService` | `Modules/Spectate/SpectateService.cs` | Drives a spectating player's camera: follow a player's view or park a free camera at a position. |
| `WorldTextColor` | `Modules/WorldText/WorldTextColor.cs` | RGBA color for a text board (`byte` channels, alpha defaults to 255). |
| `WorldTextFormat` | `Modules/WorldText/WorldTextFormat.cs` | Pure text helpers for board commands. |
| `WorldTextPlacement` | `Modules/WorldText/WorldTextPlacement.cs` | Pure math for placing a board in front of a viewer. |
| `WorldTextPlugin` | `Modules/WorldText/WorldTextPlugin.cs` | Thin Deadworks plugin class (`Name` = `World Text`) exposing the admin `/wt_*` commands. |
| `BoardInfo` | `Modules/WorldText/WorldTextService.cs` | Core operations for in-game text boards (`point_worldtext`). |
| `Entry` | `Modules/WorldText/WorldTextService.cs` | Core operations for in-game text boards (`point_worldtext`). |
| `WorldTextService` | `Modules/WorldText/WorldTextService.cs` | Core operations for in-game text boards (`point_worldtext`). |
| `WorldTextSpec` | `Modules/WorldText/WorldTextSpec.cs` | Immutable description of one text board. |
| `BalanceCandidate` | `RiftRoulette/Balance/BalancePicker.cs` | Pure choice of who moves when auto-balance triggers (Stage 13c). |
| `BalanceMove` | `RiftRoulette/Balance/BalancePicker.cs` | Pure choice of who moves when auto-balance triggers (Stage 13c). |
| `BalancePicker` | `RiftRoulette/Balance/BalancePicker.cs` | Pure choice of who moves when auto-balance triggers (Stage 13c). |
| `BalancePlugin` | `RiftRoulette/Balance/BalancePlugin.cs` | Thin plugin class (`Name` = "Rift Roulette Balance"). |
| `BalanceService` | `RiftRoulette/Balance/BalanceService.cs` | Auto-balance for Random mode (Stage 13c). |
| `BalanceReason` | `RiftRoulette/Balance/BalanceTracker.cs` | Pure counters and the auto-balance trigger (Stage 13c). |
| `BalanceTracker` | `RiftRoulette/Balance/BalanceTracker.cs` | Pure counters and the auto-balance trigger (Stage 13c). |
| `BalanceVerdict` | `RiftRoulette/Balance/BalanceTracker.cs` | Pure counters and the auto-balance trigger (Stage 13c). |
| `BetBoardText` | `RiftRoulette/Betting/BetBoardText.cs` | Pure text for the betting leaderboard board. |
| `BetRow` | `RiftRoulette/Betting/BetBoardText.cs` | Pure text for the betting leaderboard board. |
| `Bet` | `RiftRoulette/Betting/BetBook.cs` | Pure chip bookkeeping for round betting. |
| `BetBook` | `RiftRoulette/Betting/BetBook.cs` | Pure chip bookkeeping for round betting. |
| `BetOutcome` | `RiftRoulette/Betting/BetBook.cs` | Pure chip bookkeeping for round betting. |
| `BetResult` | `RiftRoulette/Betting/BetBook.cs` | Pure chip bookkeeping for round betting. |
| `BetSettlement` | `RiftRoulette/Betting/BetBook.cs` | Pure chip bookkeeping for round betting. |
| `BettingPlugin` | `RiftRoulette/Betting/BettingPlugin.cs` | Thin host for round betting (`BettingService`). |
| `BettingService` | `RiftRoulette/Betting/BettingService.cs` | Round betting in Random mode. |
| `BoardLayout` | `RiftRoulette/Draft/BoardLayout.cs` | Where the draft-area boards sit (Stage 13c, extracted from `DraftService`). |
| `DraftBoardText` | `RiftRoulette/Draft/DraftBoardText.cs` | Text for the draft boards and pool listings. |
| `DraftPlugin` | `RiftRoulette/Draft/DraftPlugin.cs` | Thin plugin class (Name `Rift Roulette Draft`) for the hero draft. |
| `DraftPools` | `RiftRoulette/Draft/DraftPools.cs` | The two Rift Roulette hero pools. |
| `DraftService` | `RiftRoulette/Draft/DraftService.cs` | Core draft operations: pick, unpick, reset, hero enforcement, starting progression, board redraw, and draft listings. |
| `DraftState` | `RiftRoulette/Draft/DraftState.cs` | The draft picks: which heroes are taken and which player (Steam ID) took each. |
| `DuelPlugin` | `RiftRoulette/Duel/DuelPlugin.cs` | Thin plugin class for 1v1 mode (`Name` = "Rift Roulette Duel"). |
| `DuelService` | `RiftRoulette/Duel/DuelService.cs` | 1v1 mode orchestration (Stage 13g, `HeroMode.Duel`, `/match_mode 1v1`). |
| `KothRule` | `RiftRoulette/Duel/KothRule.cs` | Winner-stays-on decisions for 1v1 mode (Stage 13j). |
| `StreakBoard` | `RiftRoulette/Duel/StreakBoard.cs` | Best streak per player for one 1v1 match. |
| `AutoStartAction` | `RiftRoulette/GameLoop/AutoStartRule.cs` | Pure decision for match auto-start (Stage 13d). |
| `AutoStartRule` | `RiftRoulette/GameLoop/AutoStartRule.cs` | Pure decision for match auto-start (Stage 13d). |
| `AutoStartService` | `RiftRoulette/GameLoop/AutoStartService.cs` | Starts the match when enough human players are connected and ends it when too few remain (Stage 13d), so no admin has to be online. |
| `GameLoopPlugin` | `RiftRoulette/GameLoop/GameLoopPlugin.cs` | Thin plugin class for the match loop (`Name` = "Rift Roulette Game Loop"). |
| `HeroMode` | `RiftRoulette/GameLoop/MatchConfig.cs` | Match configuration set by the server (Stage 13b). |
| `MatchConfig` | `RiftRoulette/GameLoop/MatchConfig.cs` | Match configuration set by the server (Stage 13b). |
| `MatchFormat` | `RiftRoulette/GameLoop/MatchConfig.cs` | Match configuration set by the server (Stage 13b). |
| `MatchProbe` | `RiftRoulette/GameLoop/MatchProbe.cs` | Writes a snapshot of the match to its own log (`probe-YYYYMMDD.log`, `[RiftRoulette.Probe]`) so playtests can be checked from the logs. |
| `MatchService` | `RiftRoulette/GameLoop/MatchService.cs` | The continuous playtest match loop (Stage 13a). |
| `MatchPhase` | `RiftRoulette/GameLoop/MatchState.cs` | Pure match bookkeeping for the continuous playtest match: phase, round number, score, ties. |
| `MatchState` | `RiftRoulette/GameLoop/MatchState.cs` | Pure match bookkeeping for the continuous playtest match: phase, round number, score, ties. |
| `ShopAccess` | `RiftRoulette/GameLoop/ShopAccess.cs` | Owns `citadel_allow_purchasing_anywhere`. |
| `ShopRule` | `RiftRoulette/GameLoop/ShopRule.cs` | Pure rule for when players may buy items. |
| `SoulRule` | `RiftRoulette/GameLoop/SoulRule.cs` | Pure rule for which currency gains are blocked. |
| `GunGameLadder` | `RiftRoulette/GunGame/GunGameLadder.cs` | Kill ladder for one Gun Game match (Stage 13k). |
| `LadderStep` | `RiftRoulette/GunGame/GunGameLadder.cs` | Kill ladder for one Gun Game match (Stage 13k). |
| `GunGamePlugin` | `RiftRoulette/GunGame/GunGamePlugin.cs` | Thin host for Gun Game (`GunGameService`, Stage 13k). |
| `GunGameService` | `RiftRoulette/GunGame/GunGameService.cs` | Gun Game (Stage 13k): a match format on top of Random mode. |
| `AccessFile` | `RiftRoulette/Lobby/AccessList.cs` | In-memory form of `bublock/access.json`: open/private mode plus the banned and allowed Steam64 ID sets. |
| `AccessList` | `RiftRoulette/Lobby/AccessList.cs` | In-memory form of `bublock/access.json`: open/private mode plus the banned and allowed Steam64 ID sets. |
| `AccessPlugin` | `RiftRoulette/Lobby/AccessPlugin.cs` | Thin plugin class (Name `Rift Roulette Access`) for the admin join-access commands. |
| `AccessRule` | `RiftRoulette/Lobby/AccessRule.cs` | Pure join-access rule for bans and private mode. |
| `AccessVerdict` | `RiftRoulette/Lobby/AccessRule.cs` | Pure join-access rule for bans and private mode. |
| `AccessService` | `RiftRoulette/Lobby/AccessService.cs` | Join access for bans and private mode. |
| `AdminSeat` | `RiftRoulette/Lobby/AdminSeat.cs` | The reserved 13th connection for admins (Stage 13f). |
| `AdminSeatRule` | `RiftRoulette/Lobby/AdminSeatRule.cs` | Pure rules for the reserved admin seat (Stage 13f). |
| `BigUlts` | `RiftRoulette/Lobby/BigUlts.cs` | The teamfight ultimates that send the stream camera top-down (`StreamCam.OnBigUlt`). |
| `CommandInfo` | `RiftRoulette/Lobby/CommandList.cs` | Builds the player command list shown by `/commands`. |
| `CommandList` | `RiftRoulette/Lobby/CommandList.cs` | Builds the player command list shown by `/commands`. |
| `HeroLock` | `RiftRoulette/Lobby/HeroLock.cs` | Reusable "you may not change hero" guard (extracted from Random mode in Stage 13g). |
| `LobbyPlugin` | `RiftRoulette/Lobby/LobbyPlugin.cs` | Thin plugin class (Name `Rift Roulette Lobby`) for player connection lifecycle, server setup, and the lobby commands. |
| `LobbyService` | `RiftRoulette/Lobby/LobbyService.cs` | Core Lobby operations: server setup, admitting and removing players, kick, team changes, and player descriptions. |
| `OverviewRule` | `RiftRoulette/Lobby/OverviewRule.cs` | Pure timing for the stream camera's top-down view. |
| `Participants` | `RiftRoulette/Lobby/Participants.cs` | Who takes part in the game (Stage 13f): connected players that are neither bots nor in the admin seat (`AdminSeat`). |
| `RiftRouletteTeams` | `RiftRoulette/Lobby/RiftRouletteTeams.cs` | Rift Roulette team numbers and names. |
| `CamState` | `RiftRoulette/Lobby/StreamCam.cs` | Automatic stream camera for a seated admin (observer pawn). |
| `StreamCam` | `RiftRoulette/Lobby/StreamCam.cs` | Automatic stream camera for a seated admin (observer pawn). |
| `TeamBalance` | `RiftRoulette/Lobby/TeamBalance.cs` | Pure team placement (Stage 13b, moved to Lobby in Stage 13c). |
| `RiftRouletteLocations` | `RiftRoulette/Locations/RiftRouletteLocations.cs` | Rift Roulette's teleport targets as typed `MovementLocation`s. |
| `BenchRule` | `RiftRoulette/RandomMode/BenchRule.cs` | Pure decisions for Random mode's rotating bench: with an odd number of players, one sits out each round so the fighting teams stay even. |
| `HeroDraw` | `RiftRoulette/RandomMode/HeroDraw.cs` | Pure random hero draw for Random mode. |
| `RandomAssignment` | `RiftRoulette/RandomMode/RandomModeService.cs` | Random mode orchestration (Stage 13b, joiners and hero guard in 13c). |
| `RandomModeService` | `RiftRoulette/RandomMode/RandomModeService.cs` | Random mode orchestration (Stage 13b, joiners and hero guard in 13c). |
| `RandomPlugin` | `RiftRoulette/RandomMode/RandomPlugin.cs` | Thin plugin class for Random mode (`Name` = "Rift Roulette Random"). |
| `ResolveResult` | `RiftRoulette/Rift/RiftGameRules.cs` | Access to the game's KOTH (rift) scheduler on `CCitadelGameRules`, and the two scheduler steps of the known-good rift sequence. |
| `RiftGameRules` | `RiftRoulette/Rift/RiftGameRules.cs` | Access to the game's KOTH (rift) scheduler on `CCitadelGameRules`, and the two scheduler steps of the known-good rift sequence. |
| `RiftPlugin` | `RiftRoulette/Rift/RiftPlugin.cs` | Thin plugin class for the rift: admin command wrappers around `RiftService`. |
| `RiftOutcome` | `RiftRoulette/Rift/RiftRoundResult.cs` | Pure types describing how a rift round ended. |
| `RiftRoundResult` | `RiftRoulette/Rift/RiftRoundResult.cs` | Pure types describing how a rift round ended. |
| `RiftPhase` | `RiftRoulette/Rift/RiftService.cs` | Core rift operations: start a rift through the game's KOTH scheduler, watch for the outcome, end the round, cancel, and clean up. |
| `RiftRoundSteps` | `RiftRoulette/Rift/RiftService.cs` | Core rift operations: start a rift through the game's KOTH scheduler, watch for the outcome, end the round, cancel, and clean up. |
| `RiftService` | `RiftRoulette/Rift/RiftService.cs` | Core rift operations: start a rift through the game's KOTH scheduler, watch for the outcome, end the round, cancel, and clean up. |
| `RiftSnapshot` | `RiftRoulette/Rift/RiftService.cs` | Core rift operations: start a rift through the game's KOTH scheduler, watch for the outcome, end the round, cancel, and clean up. |
| `RiftSide` | `RiftRoulette/Rift/RiftSide.cs` | The two rift sides in rotation and their spawn positions. |
| `RiftSides` | `RiftRoulette/Rift/RiftSide.cs` | The two rift sides in rotation and their spawn positions. |
| `RiftObservation` | `RiftRoulette/Rift/RiftWatch.cs` | The per-tick outcome decision of the archive rift watcher, taken out of the `Timer.Sequence` lambda so it can be unit tested. |
| `RiftWatch` | `RiftRoulette/Rift/RiftWatch.cs` | The per-tick outcome decision of the archive rift watcher, taken out of the `Timer.Sequence` lambda so it can be unit tested. |
| `RoundFlow` | `RiftRoulette/Round/RoundFlow.cs` | The composed Rift Roulette round: Rift ops (spawn, park, watch, cleanup) from `RiftService`, plus the Draft + Movement steps that move players. |
| `RoundLocations` | `RiftRoulette/Round/RoundLocations.cs` | Which start locations each team uses for a rift side. |
| `SlotSpots` | `RiftRoulette/Round/SlotSpots.cs` | Per-slot teleport spots: every player slot (0-12) has its own watch-spot position and its own rift start position, so nobody stands on anyone else. |
| `SpotOffsets` | `RiftRoulette/Round/SlotSpots.cs` | Per-slot teleport spots: every player slot (0-12) has its own watch-spot position and its own rift start position, so nobody stands on anyone else. |
| `SpotCheck` | `RiftRoulette/Round/SpotCheck.cs` | Admin tooling for the per-slot spots (`SlotSpots`): list them, and walk an admin through one group in game to catch spots inside walls or without a floor. |
| `SpotGroup` | `RiftRoulette/Round/SpotCheck.cs` | Admin tooling for the per-slot spots (`SlotSpots`): list them, and walk an admin through one group in game to catch spots inside walls or without a floor. |
| `SpotsPlugin` | `RiftRoulette/Round/SpotsPlugin.cs` | Thin plugin class (Name `Rift Roulette Spots`) for the per-slot spot admin commands. |
| `WatchGuard` | `RiftRoulette/Round/WatchGuard.cs` | Keeps waiting (restrained) players at the watch spot. |
| `WatchGuardRule` | `RiftRoulette/Round/WatchGuardRule.cs` | Pure rule for "this waiting player has left the watch spot". |
| `WatchLayout` | `RiftRoulette/Round/WatchLayout.cs` | Pure geometry for what surrounds a watch spot (boards, camera). |
| `WatchSpot` | `RiftRoulette/Round/WatchSpot.cs` | The spot up top where inactive players wait and watch (Stage 13i). |
| `WatchSpotRule` | `RiftRoulette/Round/WatchSpotRule.cs` | Which rift the watch spot sits above (Stage 13i). |
| `EventCounters` | `RiftRoulette/SelfTest/EventCounters.cs` | Counts hook and event calls since the DLL loaded, so the self-test can tell whether a hook still fires after a game update. |
| `ConVarExpectation` | `RiftRoulette/SelfTest/GameDependencies.cs` | The game dependencies `SelfTestService` checks, as plain data. |
| `GameDependencies` | `RiftRoulette/SelfTest/GameDependencies.cs` | The game dependencies `SelfTestService` checks, as plain data. |
| `SelfTestPlugin` | `RiftRoulette/SelfTest/SelfTestPlugin.cs` | Thin plugin class for the patch-day self-test (`Name` = "Rift Roulette Self-Test"). |
| `CheckResult` | `RiftRoulette/SelfTest/SelfTestResult.cs` | Result types and console formatting for the self-test. |
| `CheckStatus` | `RiftRoulette/SelfTest/SelfTestResult.cs` | Result types and console formatting for the self-test. |
| `SelfTestReport` | `RiftRoulette/SelfTest/SelfTestResult.cs` | Result types and console formatting for the self-test. |
| `SelfTestService` | `RiftRoulette/SelfTest/SelfTestService.cs` | Checks every live game dependency Bublock relies on and reports PASS / WARN / FAIL per check. |
| `SessionPlugin` | `RiftRoulette/Session/SessionPlugin.cs` | Small Rift Roulette plugin class that records the session lifecycle in the master log. |
| `StatsBoardText` | `RiftRoulette/Stats/StatsBoardText.cs` | Pure text for the side boards: one team's K / D / A (Random mode, Stage 13c) or the 1v1 best-streak leaderboard. |
| `StatsRow` | `RiftRoulette/Stats/StatsBoardText.cs` | Pure text for the side boards: one team's K / D / A (Random mode, Stage 13c) or the 1v1 best-streak leaderboard. |
| `StreakRow` | `RiftRoulette/Stats/StatsBoardText.cs` | Pure text for the side boards: one team's K / D / A (Random mode, Stage 13c) or the 1v1 best-streak leaderboard. |
| `Participant` | `RiftRoulette/Stats/StatsLedger.cs` | Pure per-match kill / death / assist counts by Steam ID (Stage 13c). |
| `PlayerStats` | `RiftRoulette/Stats/StatsLedger.cs` | Pure per-match kill / death / assist counts by Steam ID (Stage 13c). |
| `StatsLedger` | `RiftRoulette/Stats/StatsLedger.cs` | Pure per-match kill / death / assist counts by Steam ID (Stage 13c). |
| `StatsPlugin` | `RiftRoulette/Stats/StatsPlugin.cs` | Thin plugin class (`Name` = "Rift Roulette Stats"). |
| `StatsService` | `RiftRoulette/Stats/StatsService.cs` | Match kill / death / assist tracking and the stats boards shown when the draft is off (Stage 13c; 1v1 mode added in 13g). |
| `AdminAuth` | `Shared/Auth/AdminAuth.cs` | Single Steam-ID admin gate for every Bublock DLL. |
| `AdminCommand` | `Shared/Auth/AdminCommand.cs` | Shared gate and reply helper for admin `[Command]` wrappers. |
| `PlayerChat` | `Shared/Chat/PlayerChat.cs` | Sends a chat line to one player. |
| `Cheats` | `Shared/Cheats/Cheats.cs` | Runs an action with `sv_cheats` temporarily enabled. |
| `ServerConVars` | `Shared/ConVars/ServerConVars.cs` | Sets a server convar and reports when the convar does not exist. |
| `ExecutionMode` | `Shared/Execution/ExecutionMode.cs` | Execution mode passed explicitly to operations (`.rules` §5). |
| `BublockLog` | `Shared/Logging/BublockLog.cs` | Per-DLL entry point to logging. |
| `LogFormatter` | `Shared/Logging/LogFormatter.cs` | Pure text formatting for log lines. |
| `LogHub` | `Shared/Logging/LogHub.cs` | Owns one DLL's log folder: session id, round id, the master logger, feature loggers, and their rolling files. |
| `LogLevel` | `Shared/Logging/LogLevel.cs` | Log severity, same names and order as .NET `Microsoft.Extensions.Logging.LogLevel` (without `None`). |
| `LogPaths` | `Shared/Logging/LogPaths.cs` | Resolves the server log root. |
| `LogRetention` | `Shared/Logging/LogRetention.cs` | Deletes old rolled log files so disk use stays bounded. |
| `Logger` | `Shared/Logging/Logger.cs` | Writes structured lines for one feature (or the master log). |
| `PlayerRef` | `Shared/Logging/PlayerRef.cs` | Plain snapshot of the player a log line is about. |
| `PlayerRefExtensions` | `Shared/Logging/PlayerRefExtensions.cs` | `controller.ToPlayerRef()` builds a `PlayerRef` from any `CBasePlayerController` (including `CCitadelPlayerController`). |
| `RollingFileWriter` | `Shared/Logging/RollingFileWriter.cs` | Appends lines to one rolling log file family (`<base>-YYYYMMDD[.N].log`). |

## Docs (36)

| Doc | Title | Summary |
|---|---|---|
| `CLAUDE.md` | Bublock (redock fork) | Theo (GitHub `vand0525`) built this repo. |
| `Modules/Hud/Hud.md` | Hud.projitems | MSBuild shared-items file that compiles `HudService` (no commands) into a consuming project. |
| `Modules/Hud/HudCommands.md` | HudCommands.projitems | MSBuild shared-items file that adds `HudPlugin` (the `/hud_announce` and `/hud_say` admin commands) to a consuming project. |
| `Modules/Loadout/Loadout.md` | Loadout.projitems | MSBuild shared-items file that compiles the Loadout service (no commands) into a consuming project, and embeds `Data/hero-builds.json` as the resource `Bublock.Modules.Loadout.hero-builds.json`. |
| `Modules/Loadout/LoadoutCommands.md` | LoadoutCommands.projitems | MSBuild shared-items file that compiles `LoadoutPlugin` (the `/loadout_*` admin commands). |
| `Modules/Movement/Movement.md` | Movement.projitems | MSBuild shared-items file that compiles the Movement **registry and service** (no commands) into a consuming project. |
| `Modules/Movement/MovementCommands.md` | MovementCommands.projitems | MSBuild shared-items file that adds `MovementPlugin` (the `/mv_*` admin commands) to a consuming project. |
| `Modules/Queue/Queue.md` | Queue.projitems | MSBuild shared-items file that compiles `PlayerQueue` into a consuming project. |
| `Modules/Restraint/Restraint.md` | Restraint.projitems | MSBuild shared-items file that compiles `RestraintService` (no commands) into a consuming project. |
| `Modules/Restraint/RestraintCommands.md` | RestraintCommands.projitems | MSBuild shared-items file that adds `RestraintPlugin` (the per-frame `Sustain` hook and the `/restrain*`, `/status_*` admin commands) to a consuming project. |
| `Modules/Spectate/Spectate.md` | Spectate.projitems | MSBuild shared-items file that compiles `SpectateRule` and `SpectateService` (no commands) into a consuming project. |
| `Modules/WorldText/WorldText.md` | WorldText.projitems | MSBuild shared-items file that compiles the WorldText **service and types** (no commands) into a consuming project. |
| `Modules/WorldText/WorldTextCommands.md` | WorldTextCommands.projitems | MSBuild shared-items file that adds `WorldTextPlugin` (the `/wt_*` admin commands) to a consuming project. |
| `README.md` | Bublock | Deadworks server plugins for Deadlock. |
| `RiftRoulette/reference/admin-commands.md` | Rift Roulette — Admin Commands | Admin / debug / force commands. |
| `RiftRoulette/reference/behavior-inventory.md` | Bublock — Behavioral Inventory (Stage 5) | Every behavior the archive plugins have today, with the place it will live, the named operation that owns it, the command(s) that reach it, who may run it, and the stage that extracts it. |
| `RiftRoulette/reference/chat-handoff.md` | RIFT RUMBLE — CHAT HANDOFF CONTEXT | Last updated: 2026-09-26 This is context from a previous long development conversation. |
| `RiftRoulette/reference/endless-mode.md` | Endless mid-rift mode (shelved proposal) | Status: shelved 2026-09-27. |
| `RiftRoulette/reference/master-plan.md` | Bublock — Master Plan | Living roadmap for recreating archive plugins under Bublock and fitting them into a shared, granular architecture. |
| `RiftRoulette/reference/parity-review.md` | Stage 12 — Parity review (archive vs Bublock) | Side-by-side check of every archive behavior (rows from `behavior-inventory.md` §1–§3) against where it lives in Bublock. |
| `RiftRoulette/reference/patch-day.md` | Patch day runbook | What to do when a big Deadlock patch lands. |
| `RiftRoulette/reference/resources.md` | Rift Roulette — Resources | Canonical links and a living discoveries log for hard-to-find or highly useful research findings. |
| `RiftRoulette/reference/user-commands.md` | Rift Roulette — User Commands | Player-facing commands. |
| `Shared/Shared.md` | Shared.projitems | MSBuild shared-items file that compiles `Bublock/Shared/` sources into a consuming project. |
| `knowledge/README.md` | Bublock knowledge base | What we know about modding Deadlock through Deadworks, written so a person or an agent can load the right context in one pass. |
| `knowledge/changelog.md` | Changelog (redock fork) | Changes in this fork (`scho0124/bublock_redock`), newest first. |
| `knowledge/effects-catalog.md` | Effects catalog | Every lever a game mode can pull, grouped by what you want to do to the game. |
| `knowledge/game-mode-recipes.md` | Game-mode recipes | A game mode is a loop (when does a round start and end), a setup (who plays what, where), rules (what is blocked or boosted) and feedback (what players see). |
| `knowledge/generated/deadworks-api.md` | Deadworks API index (v0.4.18) | Generated by `scripts/api-index.cs`; do not edit by hand. |
| `knowledge/generated/indexes.md` | Indexes | Every command, game dependency, hook and type in the code, with where it lives. |
| `knowledge/generated/tree.md` | Repo tree | Plugins and test projects, the modules they compile in, their feature folders and files. |
| `knowledge/glossary.md` | Glossary | Terms used in the code, logs, docs and in game. |
| `knowledge/mental-models/how-deadworks-mods-work.md` | How Deadworks mods work | the game process. |
| `knowledge/mental-models/rift-roulette-architecture.md` | Rift Roulette architecture | The authority is Theo's `RiftRoulette/FEATURE.md` (composition diagram) and `.rules` §6. |
| `knowledge/mental-models/ship-and-operate.md` | Ship and operate | Local deploys use the same script: `scripts/deploy.sh --confirm`. |
| `knowledge/mental-models/talking-to-the-game.md` | Talking to the game | Deadlock has no official modding API. |
