---
type: generated-reference
generator: scripts/knowledge-graph.py
---

# Repo tree

Plugins and test projects, the modules they compile in, their feature folders and
files. Each file shows its doc's first sentence, the commands it registers and
the game dependencies it touches. Generated; do not edit.

## CleanSlate.dll (plugin)

Strip default lane/NPC/midboss/urn/shop noise so custom modes (e.g. Rift Roulette) can run on a cleaner map.

Compiles in: Shared

<pre>
├── <b>CleanSlatePlugin.cs</b> — Thin plugin host for map cleanup: disables trooper/NPC/midboss/urn spawning, removes lane bosses, powerup spawners and shop kiosks, and disables the shop buy zones shortly after startup.  <i>[cmds: /cleanup_run; game: OnLoad, OnStartupServer]</i>
├── <b>CleanSlateService.cs</b> — Static service holding the CleanSlate map-cleanup operations.  <i>[game: citadel_active_lane, citadel_crate_disable_early_spawn, citadel_crate_respawn_interval, citadel_crate_spawn_enabled, citadel_crate_spawn_initial_delay, citadel_midboss_initial_spawn_time_override, citadel_npc_spawn_enabled, citadel_trooper_spawn_enabled, citadel_herotest_orbspawner, citadel_item_powerup_spawner, citadel_shop_prop_dynamic, npc_barrack_boss, npc_boss_tier2, npc_trooper_boss, trigger_item_shop, trigger_item_shop_safe_zone]</i>
└── <b>CleanupResult.cs</b> — Immutable result of one `CleanSlateService.RemoveMapEntities` run.
</pre>

## DevTools.dll (plugin)

Server-side discovery and diagnostics for Deadlock/Deadworks experimentation (entity find, inspect, remove, snapshot/diff, hero watch, log path).

Compiles in: Shared

<pre>
└── <b>DevToolsPlugin.cs</b> — Deadworks admin/diagnostics plugin: entity find/inspect/remove, snapshot and diff, hero watch, log path.  <i>[cmds: /dev_herowatch /dev_logpath /ent_diff /ent_find /ent_info /ent_remove /ent_snapshot; game: OnLoad, OnStartupServer, OnUnload]</i>
</pre>

## GunGame.dll (plugin)

Gun Game, a game type of its own (`GunGame.dll`, redock fork).

Compiles in: Arena, DevMode, Economy, Hud, Loadout, Movement, RandomLoadout, Session, Teams, Shared

<pre>
├── <b>ArenaPlugin.cs</b> — Thin host for the arena.  <i>[cmds: /gg_arena; game: player_respawned, player_spawn, OnLoad]</i>
├── <b>DevPlugin.cs</b> — Dev / prod for Gun Game (`Modules/DevMode`), plus the dev-only environment tools.  <i>[cmds: /gg_bots /gg_exec /gg_map /pause /play /stop]</i>
├── <b>GunGameRules.cs</b> — Gun Game's own rules and player-facing text.
├── <b>GunGameService.cs</b> — The whole Gun Game game type as a composition of engine modules; the plugin classes only forward hooks here.  <i>[game: bot_kick_all, citadel_active_lane, citadel_allow_duplicate_heroes, citadel_allow_purchasing_anywhere, citadel_koth_enabled, citadel_spawn_practice_bots, citadel_spawn_practice_bots_count, citadel_team_size]</i>
├── <b>LobbyPlugin.cs</b> — Thin host for joins, leaves and the server rules.  <i>[game: OnClientConCommand, OnClientDisconnect, OnClientFullConnect, OnLoad, OnModifyCurrency, OnStartupServer]</i>
└── <b>MatchPlugin.cs</b> — Thin host for kills and match commands.  <i>[cmds: /gg_end /gg_reroll /gg_status /gg_time /points; game: player_death]</i>
</pre>

## RiftRoulette.dll (plugin)

Custom Deadlock game mode plugin: draft/staging, hero select, real Rift (KOTH) rounds, return to draft.

Compiles in: Hud, Loadout, Movement, Queue, Restraint, Spectate, WorldText, Shared

<pre>
├── <b>Balance/</b> — Keeps Random mode matches from becoming one-sided (Stage 13c).
│   ├── <b>BalancePicker.cs</b> — Pure choice of who moves when auto-balance triggers (Stage 13c).
│   ├── <b>BalancePlugin.cs</b> — Thin plugin class (`Name` = "Rift Roulette Balance").  <i>[cmds: /balance_auto /balance_now /balance_status]</i>
│   ├── <b>BalanceService.cs</b> — Auto-balance for Random mode (Stage 13c).
│   └── <b>BalanceTracker.cs</b> — Pure counters and the auto-balance trigger (Stage 13c).
├── <b>Betting/</b> — Round betting in Random mode, to give players something to do between rounds (and the player sitting out something to do during one).
│   ├── <b>BetBoardText.cs</b> — Pure text for the betting leaderboard board.
│   ├── <b>BetBook.cs</b> — Pure chip bookkeeping for round betting.
│   ├── <b>BettingPlugin.cs</b> — Thin host for round betting (`BettingService`).  <i>[cmds: /bet /bet_status /chips; game: OnChatMessage]</i>
│   └── <b>BettingService.cs</b> — Round betting in Random mode.
├── <b>Draft/</b> — The Rift Roulette hero draft: two hero pools, one pick per player, team assignment from the pick, starting gold, hero enforcement, the draft boards, and the pick commands.
│   ├── <b>BoardLayout.cs</b> — Where the draft-area boards sit (Stage 13c, extracted from `DraftService`).
│   ├── <b>DraftBoardText.cs</b> — Text for the draft boards and pool listings.
│   ├── <b>DraftPlugin.cs</b> — Thin plugin class (Name `Rift Roulette Draft`) for the hero draft.  <i>[cmds: /draft_assign /draft_boards /draft_release /draft_reset /draft_status /heroes /pick /picks /unpick; game: player_hero_changed, OnLoad, OnStartupServer]</i>
│   ├── <b>DraftPools.cs</b> — The two Rift Roulette hero pools.
│   ├── <b>DraftService.cs</b> — Core draft operations: pick, unpick, reset, hero enforcement, starting progression, board redraw, and draft listings.
│   └── <b>DraftState.cs</b> — The draft picks: which heroes are taken and which player (Steam ID) took each.
├── <b>Duel/</b> — 1v1 mode (Stage 13g).
│   ├── <b>DuelPlugin.cs</b> — Thin plugin class for 1v1 mode (`Name` = "Rift Roulette Duel").  <i>[cmds: /duel_clear /duel_copy /duel_queue /duel_queue_add /duel_queue_remove /duel_status /queue /unqueue; game: player_respawned, player_spawn]</i>
│   ├── <b>DuelService.cs</b> — 1v1 mode orchestration (Stage 13g, `HeroMode.Duel`, `/match_mode 1v1`).
│   ├── <b>KothRule.cs</b> — Winner-stays-on decisions for 1v1 mode (Stage 13j).
│   └── <b>StreakBoard.cs</b> — Best streak per player for one 1v1 match.
├── <b>GameLoop/</b> — Runs a continuous playtest match (Stage 13a).
│   ├── <b>AutoStartRule.cs</b> — Pure decision for match auto-start (Stage 13d).
│   ├── <b>AutoStartService.cs</b> — Starts the match when enough human players are connected and ends it when too few remain (Stage 13d), so no admin has to be online.
│   ├── <b>GameLoopPlugin.cs</b> — Thin plugin class for the match loop (`Name` = "Rift Roulette Game Loop").  <i>[cmds: /match_auto /match_config /match_end /match_format /match_intermission /match_mode /match_start /match_status /score; game: OnClientConCommand, OnGameFrame, OnLoad, OnModifyCurrency, OnTakeDamage]</i>
│   ├── <b>MatchConfig.cs</b> — Match configuration set by the server (Stage 13b).
│   ├── <b>MatchProbe.cs</b> — Writes a snapshot of the match to its own log (`probe-YYYYMMDD.log`, <i>[RiftRoulette.Probe]</i>) so playtests can be checked from the logs.  <i>[game: citadel_shop_prop_dynamic, npc_barrack_boss, npc_boss_tier2, npc_trooper_boss]</i>
│   ├── <b>MatchService.cs</b> — The continuous playtest match loop (Stage 13a).
│   ├── <b>MatchState.cs</b> — Pure match bookkeeping for the continuous playtest match: phase, round number, score, ties.
│   ├── <b>ShopAccess.cs</b> — Owns `citadel_allow_purchasing_anywhere`.  <i>[game: citadel_allow_purchasing_anywhere]</i>
│   ├── <b>ShopRule.cs</b> — Pure rule for when players may buy items.
│   └── <b>SoulRule.cs</b> — Pure rule for which currency gains are blocked.
├── <b>Lobby/</b> — Everything about players being on the server outside the draft and rift logic: server convars at startup, admitting players to the draft area on connect, returning them to draft on spawn, cleaning up on disconnect, kick,
│   ├── <b>AccessList.cs</b> — In-memory form of `bublock/access.json`: open/private mode plus the banned and allowed Steam64 ID sets.
│   ├── <b>AccessPlugin.cs</b> — Thin plugin class (Name `Rift Roulette Access`) for the admin join-access commands.  <i>[cmds: /access_mode /allow_add /allow_list /allow_remove /ban_add /ban_list /ban_remove /player_ban; game: OnLoad]</i>
│   ├── <b>AccessRule.cs</b> — Pure join-access rule for bans and private mode.
│   ├── <b>AccessService.cs</b> — Join access for bans and private mode.
│   ├── <b>AdminSeat.cs</b> — The reserved 13th connection for admins (Stage 13f).
│   ├── <b>AdminSeatRule.cs</b> — Pure rules for the reserved admin seat (Stage 13f).
│   ├── <b>BigUlts.cs</b> — The teamfight ultimates that send the stream camera top-down (`StreamCam.OnBigUlt`).  <i>[game: citadel_ability_bebop_laser_beam, citadel_ability_bull_leap, citadel_ability_lash_ultimate, citadel_ability_rocket_barrage, citadel_ability_self_vacuum, citadel_ability_storm_cloud, citadel_ability_tengu_airlift]</i>
│   ├── <b>CommandList.cs</b> — Builds the player command list shown by `/commands`.
│   ├── <b>HeroLock.cs</b> — Reusable "you may not change hero" guard (extracted from Random mode in Stage 13g).
│   ├── <b>LobbyPlugin.cs</b> — Thin plugin class (Name `Rift Roulette Lobby`) for player connection lifecycle, server setup, and the lobby commands.  <i>[cmds: /commands /lobby_setup /player_info /player_kick /player_list /player_team /seat_play /seat_spec /seat_status /spec_auto /spec_overview /spec_status /status; game: player_death, player_spawn, player_used_ability, OnClientConnect, OnClientDisconnect, OnClientFullConnect, OnLoad, OnStartupServer]</i>
│   ├── <b>LobbyService.cs</b> — Core Lobby operations: server setup, admitting and removing players, kick, team changes, and player descriptions.  <i>[game: citadel_allow_duplicate_heroes, citadel_koth_enabled, citadel_team_size]</i>
│   ├── <b>OverviewRule.cs</b> — Pure timing for the stream camera's top-down view.
│   ├── <b>Participants.cs</b> — Who takes part in the game (Stage 13f): connected players that are neither bots nor in the admin seat (`AdminSeat`).
│   ├── <b>RiftRouletteTeams.cs</b> — Rift Roulette team numbers and names.
│   ├── <b>StreamCam.cs</b> — Automatic stream camera for a seated admin (observer pawn).
│   └── <b>TeamBalance.cs</b> — Pure team placement (Stage 13b, moved to Lobby in Stage 13c).
├── <b>Locations/</b> — Rift Roulette-specific map positions for the generic Movement module (draft area, the watch spots above each rift, and rift start spawns), kept out of `Modules/Movement` so the module stays game-agnostic.
│   └── <b>RiftRouletteLocations.cs</b> — Rift Roulette's teleport targets as typed `MovementLocation`s.
├── <b>RandomMode/</b> — Random hero mode for the continuous match (Stage 13b, the default `MatchConfig.HeroMode`).
│   ├── <b>BenchRule.cs</b> — Pure decisions for Random mode's rotating bench: with an odd number of players, one sits out each round so the fighting teams stay even.
│   ├── <b>HeroDraw.cs</b> — Pure random hero draw for Random mode.
│   ├── <b>RandomModeService.cs</b> — Random mode orchestration (Stage 13b, joiners and hero guard in 13c).
│   └── <b>RandomPlugin.cs</b> — Thin plugin class for Random mode (`Name` = "Rift Roulette Random").  <i>[cmds: /random_reroll /random_status; game: player_respawned, player_spawn]</i>
├── <b>Rift/</b> — The rift itself: force the game's KOTH (rift) to spawn on the next side (green / yellow alternating), park the natural scheduler, watch for a finish (troopers spawn) or a tie (the cash-in disappears), then after 3 second
│   ├── <b>RiftGameRules.cs</b> — Access to the game's KOTH (rift) scheduler on `CCitadelGameRules`, and the two scheduler steps of the known-good rift sequence.  <i>[game: citadel_koth_enabled, citadel_gamerules, m_pGameRules, m_timeKothGiveUp, m_timeNextKothSpawn, m_timeNextKothSpawnWindowTime, m_vNextKothLocation]</i>
│   ├── <b>RiftPlugin.cs</b> — Thin plugin class for the rift: admin command wrappers around `RiftService`.  <i>[cmds: /rift_cancel /rift_cleanup /rift_next /rift_start /rift_status]</i>
│   ├── <b>RiftRoundResult.cs</b> — Pure types describing how a rift round ended.
│   ├── <b>RiftService.cs</b> — Core rift operations: start a rift through the game's KOTH scheduler, watch for the outcome, end the round, cancel, and clean up.  <i>[game: citadel_item_koth_spawner, citadel_koth_cashin, npc_trooper]</i>
│   ├── <b>RiftSide.cs</b> — The two rift sides in rotation and their spawn positions.
│   └── <b>RiftWatch.cs</b> — The per-tick outcome decision of the archive rift watcher, taken out of the `Timer.Sequence` lambda so it can be unit tested.
├── <b>Round/</b> — The parity round, composed from Draft, Movement, and Rift ops: start a rift, move each drafted team to its side's start, and return players to draft when the rift ends or is cancelled.
│   ├── <b>RoundFlow.cs</b> — The composed Rift Roulette round: Rift ops (spawn, park, watch, cleanup) from `RiftService`, plus the Draft + Movement steps that move players.
│   ├── <b>RoundLocations.cs</b> — Which start locations each team uses for a rift side.
│   ├── <b>SlotSpots.cs</b> — Per-slot teleport spots: every player slot (0-12) has its own watch-spot position and its own rift start position, so nobody stands on anyone else.
│   ├── <b>SpotCheck.cs</b> — Admin tooling for the per-slot spots (`SlotSpots`): list them, and walk an admin through one group in game to catch spots inside walls or without a floor.
│   ├── <b>SpotsPlugin.cs</b> — Thin plugin class (Name `Rift Roulette Spots`) for the per-slot spot admin commands.  <i>[cmds: /spots_list /spots_walk]</i>
│   ├── <b>WatchGuard.cs</b> — Keeps waiting (restrained) players at the watch spot.
│   ├── <b>WatchGuardRule.cs</b> — Pure rule for "this waiting player has left the watch spot".
│   ├── <b>WatchLayout.cs</b> — Pure geometry for what surrounds a watch spot (boards, camera).
│   ├── <b>WatchSpot.cs</b> — The spot up top where inactive players wait and watch (Stage 13i).
│   └── <b>WatchSpotRule.cs</b> — Which rift the watch spot sits above (Stage 13i).
├── <b>SelfTest/</b> — Tells within minutes what a Deadlock / Deadworks update broke.
│   ├── <b>EventCounters.cs</b> — Counts hook and event calls since the DLL loaded, so the self-test can tell whether a hook still fires after a game update.
│   ├── <b>GameDependencies.cs</b> — The game dependencies `SelfTestService` checks, as plain data.  <i>[game: citadel_active_lane, citadel_allow_duplicate_heroes, citadel_allow_purchasing_anywhere, citadel_crate_disable_early_spawn, citadel_crate_respawn_interval, citadel_crate_spawn_enabled, citadel_crate_spawn_initial_delay, citadel_koth_early_warning_time, citadel_koth_enabled, citadel_koth_warning_time, citadel_midboss_initial_spawn_time_override, citadel_npc_spawn_enabled, citadel_player_override_spawn_time, citadel_team_size, citadel_trooper_spawn_enabled, citadel_gamerules, citadel_herotest_orbspawner, citadel_item_powerup_spawner, citadel_shop_prop_dynamic, info_koth_spawn_location, info_super_trooper_spawn, item_crate_spawn, npc_barrack_boss, npc_boss_tier2, npc_trooper_boss, trigger_item_shop, trigger_item_shop_safe_zone]</i>
│   ├── <b>SelfTestPlugin.cs</b> — Thin plugin class for the patch-day self-test (`Name` = "Rift Roulette Self-Test").  <i>[cmds: /selftest_live /selftest_run]</i>
│   ├── <b>SelfTestResult.cs</b> — Result types and console formatting for the self-test.
│   └── <b>SelfTestService.cs</b> — Checks every live game dependency Bublock relies on and reports PASS / WARN / FAIL per check.  <i>[game: m_pGameRules]</i>
├── <b>Session/</b> — Session lifecycle lines in the Rift Roulette master log (load, map startup, unload), so every log file shares one session story.
│   └── <b>SessionPlugin.cs</b> — Small Rift Roulette plugin class that records the session lifecycle in the master log.  <i>[cmds: /session_info; game: OnLoad, OnStartupServer, OnUnload]</i>
└── <b>Stats/</b> — Counts every player's kills, deaths and assists from match start (Stage 13c) and, in Random mode, shows them on two boards in the draft area: Sapphire's on the Sapphire side and Amber's on the Amber side, each with the t
    ├── <b>StatsBoardText.cs</b> — Pure text for the side boards: one team's K / D / A (Random mode, Stage 13c) or the 1v1 best-streak leaderboard.
    ├── <b>StatsLedger.cs</b> — Pure per-match kill / death / assist counts by Steam ID (Stage 13c).
    ├── <b>StatsPlugin.cs</b> — Thin plugin class (`Name` = "Rift Roulette Stats").  <i>[cmds: /stats /stats_board /stats_reset; game: player_death]</i>
    └── <b>StatsService.cs</b> — Match kill / death / assist tracking and the stats boards shown when the draft is off (Stage 13c; 1v1 mode added in 13g).
</pre>

## Arena (module)

Contained fight areas for brawler-style game types: each team gets an anchor, each slot an offset from it (a whole team lands in rows instead of on one point), and an optional bounds box keeps players inside.

<pre>
├── <b>ArenaService.cs</b> — Sends a player to their own arena spot through `Modules/Movement`.
└── <b>ArenaSpots.cs</b> — Where a game type's players spawn: one anchor per team plus a per-slot offset, read from a JSON asset the game type embeds.
</pre>

## DevMode (module)

Dev and prod for game types (redock fork).

<pre>
├── <b>DebugSnapshot.cs</b> — A point-in-time dump for debugging a live session: map, player and bot counts, then one line per player (slot, name, bot, team, alive and health, position), then the game type's own lines.
├── <b>DevRules.cs</b> — Dev / prod for game types.
└── <b>PositionMemory.cs</b> — Remembers where players stood in dev so ending a live session puts them back there.
</pre>

## Economy (module)

Currency rules a game type applies from its `OnModifyCurrency` hook.

<pre>
└── <b>SoulRule.cs</b> — Which currency gains to block in a game type where power comes only from the build a player is given.
</pre>

## Hud (module)

Reusable, game-agnostic on-screen announcements: the game's HUD banner (big title, smaller description) instead of chat.

<pre>
├── <b>HudPlugin.cs</b> — Thin Deadworks plugin class (`Name` = `Hud`) exposing the admin `/hud_announce` and `/hud_say` commands.  <i>[cmds: /hud_announce /hud_say]</i>
└── <b>HudService.cs</b> — Static service that shows the game's on-screen announcement banner (title plus smaller description) to one player or to everyone.
</pre>

## Loadout (module)

Reusable Deadlock hero loadouts built from real build data: the top 3 builds per hero (by matches played, from the Deadlock API), applied to a pawn as the first 9 items in build order plus the build's ability order.

<pre>
├── <b>HeroBuildCatalog.cs</b> — Lookup over `HeroBuildData`: builds per hero, the playable hero pool, display names, and item components.
├── <b>HeroBuildData.cs</b> — Records for the build data file (`Data/hero-builds.json`, written by `scripts/fetch-builds.py`) and its JSON parser.
├── <b>LoadoutPlanner.cs</b> — Pure planning for a build: which items to grant and which upgrade bits to set on each ability.
├── <b>LoadoutPlugin.cs</b> — Thin admin command host for the Loadout module.  <i>[cmds: /loadout_copy /loadout_give /loadout_info /loadout_list]</i>
├── <b>LoadoutService.cs</b> — Applies a stored hero build to a live pawn, and swaps a player to a hero and then applies a build.
├── <b>LoadoutSnapshot.cs</b> — Plain records describing one player's exact hero state, used to copy a hero from one player onto another (`LoadoutService.Capture` / `ApplySnapshot`).
└── <b>Progression.cs</b> — Deadlock's level table: how many boons, ability unlocks and ability points a hero has at a given soul count.
</pre>

## Movement (module)

Reusable, game-agnostic player movement: a registry of named locations (position + camera angle), teleport one player / a list of players, set a player's camera angle.

<pre>
├── <b>LocationRegistry.cs</b> — Pure name -> `MovementLocation` registry.
├── <b>MovementLocation.cs</b> — Named teleport target.
├── <b>MovementPlugin.cs</b> — Thin Deadworks plugin class (`Name` = `Movement`) exposing the admin `/mv_*` commands.  <i>[cmds: /mv_angle /mv_list /mv_remove /mv_save /mv_tp /mv_tp_all /mv_tp_team /mv_where]</i>
└── <b>MovementService.cs</b> — Core teleport and camera operations.  <i>[game: CCitadelUserMsg_SetClientCameraAngles]</i>
</pre>

## Queue (module)

A reusable join queue of players (Stage 13j).

<pre>
└── <b>PlayerQueue.cs</b> — An ordered line of unique Steam IDs.
</pre>

## RandomLoadout (module)

"Give this player a new random hero with a real build" as one call, for any game type: Gun Game (a new hero on every kill), and later zombie or random-hero modes.

<pre>
├── <b>HeroRoll.cs</b> — Picks a random hero for one player.
└── <b>RandomLoadouts.cs</b> — A random hero plus one of that hero's stored top builds per player, given through `Modules/Loadout` (`LoadoutService.Swap`: `SelectHero`, then the budgeted build 1 s later).
</pre>

## Restraint (module)

Reusable, game-agnostic "can't fight" state: silence, item block, shooting block and a melee block that stay on a player until released, through death and hero swaps.

<pre>
├── <b>RestraintPlugin.cs</b> — Thin host for `RestraintService`: the per-frame hook and admin commands.  <i>[cmds: /restrain /restrain_list /restrain_release /status_add /status_remove; game: OnGameFrame]</i>
└── <b>RestraintService.cs</b> — Keeps chosen players silenced and unable to use items, shoot or melee until they are released (Stage 13h), and ignored by NPC targeting.  <i>[game: EModifierState.IgnoredByNpcTargeting, EModifierState.ItemsDisabled, EModifierState.MeleeDisabled, EModifierState.ShootingDisabled, EModifierState.Silenced, modifier_citadel_silenced]</i>
</pre>

## Session (module)

The match structure for continuous game types: one session that never ends while players are on, cut into fixed-length matches (default 2 minutes) with a short results break between them, and a per-player scoreboard.

<pre>
├── <b>Scoreboard.cs</b> — Points per player for one match.
├── <b>SessionRule.cs</b> — Pure decisions and types for `TimedSession`.
└── <b>TimedSession.cs</b> — One continuous session of fixed-length matches: no rounds, just `Waiting` → `Playing` (the match clock) → `Break` (results) → `Playing` again while enough players stay.
</pre>

## Shared (module)

Cross-cutting helpers compiled into every Bublock plugin DLL as source (`Shared.projitems`).

<pre>
├── <b>AdminAuth.cs</b> — Single Steam-ID admin gate for every Bublock DLL.
├── <b>AdminCommand.cs</b> — Shared gate and reply helper for admin <i>[Command]</i> wrappers.
├── <b>BublockLog.cs</b> — Per-DLL entry point to logging.
├── <b>Cheats.cs</b> — Runs an action with `sv_cheats` temporarily enabled.  <i>[game: sv_cheats]</i>
├── <b>ExecutionMode.cs</b> — Execution mode passed explicitly to operations (`.rules` §5).
├── <b>LogFormatter.cs</b> — Pure text formatting for log lines.
├── <b>Logger.cs</b> — Writes structured lines for one feature (or the master log).
├── <b>LogHub.cs</b> — Owns one DLL's log folder: session id, round id, the master logger, feature loggers, and their rolling files.
├── <b>LogLevel.cs</b> — Log severity, same names and order as .NET `Microsoft.Extensions.Logging.LogLevel` (without `None`).
├── <b>LogPaths.cs</b> — Resolves the server log root.
├── <b>LogRetention.cs</b> — Deletes old rolled log files so disk use stays bounded.
├── <b>PlayerChat.cs</b> — Sends a chat line to one player.  <i>[game: CCitadelUserMsg_ChatMsg]</i>
├── <b>PlayerRef.cs</b> — Plain snapshot of the player a log line is about.
├── <b>PlayerRefExtensions.cs</b> — `controller.ToPlayerRef()` builds a `PlayerRef` from any `CBasePlayerController` (including `CCitadelPlayerController`).
├── <b>RollingFileWriter.cs</b> — Appends lines to one rolling log file family (`&lt;base>-YYYYMMDD[.N].log`).
└── <b>ServerConVars.cs</b> — Sets a server convar and reports when the convar does not exist.
</pre>

## Spectate (module)

Reusable, game-agnostic spectator camera control: follow a player's view (in-eye, what that player sees) or park a free camera at a position and angle.

<pre>
├── <b>SpectateRule.cs</b> — Pure decisions for an automatic spectator camera.
└── <b>SpectateService.cs</b> — Drives a spectating player's camera: follow a player's view or park a free camera at a position.  <i>[game: observer]</i>
</pre>

## Teams (module)

Deadlock team basics any game type needs: the team numbers and names, placing a new player on the smaller team, and refusing client hero or team changes when the game type decides them.

<pre>
├── <b>ChoiceGuard.cs</b> — Which client console commands a game type refuses when it decides heroes or teams itself.
└── <b>DeadlockTeams.cs</b> — Deadlock's team numbers and names, and new-player placement.
</pre>

## WorldText (module)

Reusable, game-agnostic in-game text boards (`point_worldtext`): create, update, remove, list, and clear by string id.

<pre>
├── <b>WorldTextColor.cs</b> — RGBA color for a text board (`byte` channels, alpha defaults to 255).
├── <b>WorldTextFormat.cs</b> — Pure text helpers for board commands.
├── <b>WorldTextPlacement.cs</b> — Pure math for placing a board in front of a viewer.
├── <b>WorldTextPlugin.cs</b> — Thin Deadworks plugin class (`Name` = `World Text`) exposing the admin `/wt_*` commands.  <i>[cmds: /wt_clear /wt_create /wt_list /wt_remove /wt_update]</i>
├── <b>WorldTextService.cs</b> — Core operations for in-game text boards (`point_worldtext`).  <i>[game: point_worldtext]</i>
└── <b>WorldTextSpec.cs</b> — Immutable description of one text board.
</pre>

## GunGame.Tests (test-project)

Compiles in: Arena, DevMode, Movement, Session, Teams, Shared

<pre>
└── <b>GunGameRulesTests.cs</b> — Unit tests for `GunGame/GunGameRules` and the game type's embedded arena asset.
</pre>

## Modules.Tests (test-project)

Compiles in: Arena, DevMode, Economy, Hud, Loadout, Movement, Queue, RandomLoadout, Session, Spectate, Teams, WorldText, Shared

<pre>
├── <b>ArenaSpotsTests.cs</b> — Unit tests for `Modules/Arena/ArenaSpots` with an inline arena.
├── <b>DevModeTests.cs</b> — Unit tests for `Modules/DevMode/DevRules`: a dev-only command is allowed in dev and refused in prod with the `/stop first` message; mode names parse (`dev`, `prod`, any case, trimmed; anything else is refused).
├── <b>EconomyTests.cs</b> — Unit tests for `Modules/Economy/SoulRule`.
├── <b>HeroBuildCatalogTests.cs</b> — Unit tests for `Modules/Loadout/HeroBuildData` and `HeroBuildCatalog`.
├── <b>HeroRollTests.cs</b> — Unit tests for `Modules/RandomLoadout/HeroRoll` with a three-hero pool.
├── <b>HudServiceTests.cs</b> — Unit tests for `Modules/Hud/HudService.ParseAnnouncement` (the only pure part of the Hud module).
├── <b>LoadoutPlannerTests.cs</b> — Unit tests for `Modules/Loadout/LoadoutPlanner` (pure).
├── <b>LocationRegistryTests.cs</b> — Unit tests for `Modules/Movement/LocationRegistry` (pure; no game needed).
├── <b>MovementLocationTests.cs</b>
├── <b>PlayerQueueTests.cs</b> — Unit tests for `Modules/Queue/PlayerQueue`.
├── <b>ProgressionTests.cs</b> — Unit tests for `Modules/Loadout/Progression` (pure).
├── <b>SessionTests.cs</b> — Unit tests for the pure parts of `Modules/Session` (`SessionRule`, `Scoreboard`).
├── <b>SpectateRuleTests.cs</b> — Unit tests for `Modules/Spectate/SpectateRule`.
├── <b>TeamsTests.cs</b> — Unit tests for `Modules/Teams` (`DeadlockTeams`, `ChoiceGuard`).
├── <b>WorldTextFormatTests.cs</b> — Unit tests for `Modules/WorldText/WorldTextFormat`.
└── <b>WorldTextPlacementTests.cs</b> — Unit tests for `Modules/WorldText/WorldTextPlacement`.
</pre>

## RiftRoulette.Tests (test-project)

Compiles in: Movement, Queue, Shared

<pre>
├── <b>AccessListTests.cs</b> — Unit tests for `RiftRoulette/Lobby/AccessList`.
├── <b>AccessRuleTests.cs</b> — Unit tests for `RiftRoulette/Lobby/AccessRule`.
├── <b>AdminSeatRuleTests.cs</b> — Unit tests for `Lobby/AdminSeatRule`.
├── <b>AutoStartRuleTests.cs</b> — Unit tests for `GameLoop/AutoStartRule`.
├── <b>BalancePickerTests.cs</b> — Unit tests for `Balance/BalancePicker`.
├── <b>BalanceTrackerTests.cs</b> — Unit tests for `Balance/BalanceTracker`.
├── <b>BenchRuleTests.cs</b> — Unit tests for `RiftRoulette/RandomMode/BenchRule`.
├── <b>BetBoardTextTests.cs</b> — Unit tests for `RiftRoulette/Betting/BetBoardText`.
├── <b>BetBookTests.cs</b> — Unit tests for `RiftRoulette/Betting/BetBook`.
├── <b>BigUltsTests.cs</b> — Unit tests for `RiftRoulette/Lobby/BigUlts`.
├── <b>CommandListTests.cs</b> — Unit tests for `RiftRoulette/Lobby/CommandList`.
├── <b>DraftBoardTextTests.cs</b> — Unit tests for `RiftRoulette/Draft/DraftBoardText`.
├── <b>DraftPoolsTests.cs</b> — Unit tests for `RiftRoulette/Draft/DraftPools`.
├── <b>DraftStateTests.cs</b> — Unit tests for `RiftRoulette/Draft/DraftState` (static pick data).
├── <b>HeroDrawTests.cs</b> — Unit tests for `RandomMode/HeroDraw` (pure, seeded `Random`).
├── <b>KothRuleTests.cs</b> — Unit tests for `RiftRoulette/Duel/KothRule`.
├── <b>MatchConfigTests.cs</b> — Unit tests for the pure parts of `GameLoop/MatchConfig`.
├── <b>MatchStateTests.cs</b> — Unit tests for `RiftRoulette/GameLoop/MatchState` (with `Rift/RiftRoundResult`).
├── <b>OverviewRuleTests.cs</b> — Unit tests for `RiftRoulette/Lobby/OverviewRule`.
├── <b>RiftRouletteTeamsTests.cs</b> — Unit tests for `RiftRoulette/Lobby/RiftRouletteTeams`.
├── <b>RiftSidesTests.cs</b> — Unit tests for `RiftRoulette/Rift/RiftSide` (`RiftSides`).
├── <b>RiftWatchTests.cs</b> — Unit tests for `RiftRoulette/Rift/RiftWatch`.
├── <b>RoundLocationsTests.cs</b> — Unit tests for `RiftRoulette/Round/RoundLocations`.
├── <b>SelfTestTests.cs</b>
├── <b>ShopRuleTests.cs</b> — Unit tests for `RiftRoulette/GameLoop/ShopRule`.
├── <b>SlotSpotsTests.cs</b>
├── <b>SoulRuleTests.cs</b> — Unit tests for `RiftRoulette/GameLoop/SoulRule`.
├── <b>StatsBoardTextTests.cs</b> — Unit tests for `Stats/StatsBoardText`.
├── <b>StatsLedgerTests.cs</b> — Unit tests for `Stats/StatsLedger`.
├── <b>StreakBoardTests.cs</b> — Unit tests for `RiftRoulette/Duel/StreakBoard`.
├── <b>TeamBalanceTests.cs</b> — Unit tests for `Lobby/TeamBalance` (pure, seeded `Random`).
├── <b>WatchGuardRuleTests.cs</b> — Unit tests for `RiftRoulette/Round/WatchGuardRule`.
├── <b>WatchLayoutTests.cs</b> — Unit tests for `RiftRoulette/Round/WatchLayout`.
└── <b>WatchSpotRuleTests.cs</b> — Unit tests for `RiftRoulette/Round/WatchSpotRule`.
</pre>

## Shared.Tests (test-project)

Compiles in: Shared

<pre>
├── <b>LogFormatterTests.cs</b> — Checks `LogFormatter` output exactly:
├── <b>LogHubTests.cs</b> — End-to-end checks of `LogHub` + `Logger` writing into a temp folder:
├── <b>LogRetentionTests.cs</b> — Checks `LogRetention.DeleteOlderThan` with a fixed date (2026-09-26):
├── <b>RollingFileWriterTests.cs</b> — Checks `RollingFileWriter` against a temp folder with a fake clock:
└── <b>TempDir.cs</b> — Test helper: creates a unique folder under the system temp directory (`bublock-tests/&lt;guid>`) and deletes it on `Dispose`.
</pre>
