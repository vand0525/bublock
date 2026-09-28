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

## RiftRoulette.dll (plugin)

Custom Deadlock game mode plugin: draft/staging, hero select, real Rift (KOTH) rounds, return to draft.

Compiles in: Hud, Loadout, Movement, Queue, Restraint, Spectate, WorldText, Shared

<pre>
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

## Restraint (module)

Reusable, game-agnostic "can't fight" state: silence, item block, shooting block and a melee block that stay on a player until released, through death and hero swaps.

<pre>
├── <b>RestraintPlugin.cs</b> — Thin host for `RestraintService`: the per-frame hook and admin commands.  <i>[cmds: /restrain /restrain_list /restrain_release /status_add /status_remove; game: OnGameFrame]</i>
└── <b>RestraintService.cs</b> — Keeps chosen players silenced and unable to use items, shoot or melee until they are released (Stage 13h), and ignored by NPC targeting.  <i>[game: EModifierState.IgnoredByNpcTargeting, EModifierState.ItemsDisabled, EModifierState.MeleeDisabled, EModifierState.ShootingDisabled, EModifierState.Silenced, modifier_citadel_silenced]</i>
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

## Modules.Tests (test-project)

Compiles in: Hud, Loadout, Movement, Queue, Spectate, WorldText, Shared

<pre>
├── <b>HeroBuildCatalogTests.cs</b> — Unit tests for `Modules/Loadout/HeroBuildData` and `HeroBuildCatalog`.
├── <b>HudServiceTests.cs</b> — Unit tests for `Modules/Hud/HudService.ParseAnnouncement` (the only pure part of the Hud module).
├── <b>LoadoutPlannerTests.cs</b> — Unit tests for `Modules/Loadout/LoadoutPlanner` (pure).
├── <b>LocationRegistryTests.cs</b> — Unit tests for `Modules/Movement/LocationRegistry` (pure; no game needed).
├── <b>MovementLocationTests.cs</b>
├── <b>PlayerQueueTests.cs</b> — Unit tests for `Modules/Queue/PlayerQueue`.
├── <b>ProgressionTests.cs</b> — Unit tests for `Modules/Loadout/Progression` (pure).
├── <b>SpectateRuleTests.cs</b> — Unit tests for `Modules/Spectate/SpectateRule`.
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
├── <b>GunGameLadderTests.cs</b> — Unit tests for `RiftRoulette/GunGame/GunGameLadder`.
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
