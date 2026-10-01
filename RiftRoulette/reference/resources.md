# Rift Roulette — Resources

Canonical links and a living discoveries log for hard-to-find or highly useful
research findings. Prefer linking official docs/schema over restating them.

## Canonical links

| Resource | URL |
|----------|-----|
| Deadworks schema / modding DB | https://deadworks.net/db/schema |
| Deadworks docs | https://docs.deadworks.net/ |
| Deadworks GitHub | https://github.com/Deadworks-net/deadworks |
| Deadlock CVar list (upstream) | https://github.com/Mikooboy/deadlock-cvar-list/blob/main/cvarlist.md |
| Deadworks map explorer (`dl_midtown`) | https://deadworks.net/db/map |

## Local mirrors and workspace anchors

| Path | Purpose |
|------|---------|
| `Bublock/RiftRoulette/reference/master-plan.md` | Rebuild roadmap — pull one stage at a time |
| `Bublock/RiftRoulette/reference/cvarlist.md` | Local mirror of the CVar list |
| `Bublock/RiftRoulette/reference/patch-day.md` | What to do when a Deadlock patch lands: steps, feature catalogue, symptom-to-fix table, dependency tables |
| `Bublock/RiftRoulette/reference/baseline/<date>/` | Known-good snapshot for `scripts/patch-check.py` (heroes, items, cvars, API surface, map counts) |
| `Bublock/RiftRoulette/reference/maps/dl_midtown/` | `dl_midtown` entity dump and world-mesh index (build 6698) |
| `Bublock/RiftRoulette/reference/chat-handoff.md` | Prior verified session context |
| `Bublock/RiftRoulette/reference/.rules` | Development rules (source of truth) |
| `Bublock/Shared/` | Auth, chat, cheats, convars, execution mode, logging |
| `Bublock/Modules/` | Reusable game-agnostic modules |
| `Bublock/RiftRoulette/` | Game mode plugin (active) |
| `Bublock/DevTools/` | Discovery / diagnostics plugin (active) |
| `Bublock/CleanSlate/` | Map cleanup plugin (active) |
| `Bublock/logs/` | Cursor log pull root (`<Dll>/` subfolders), filled by `scripts/pull-logs.sh` |
| `/server/game/bin/win64/bublock/logs/` | Server log folder (written by Bublock DLLs) |
| `Bublock/Tests/` | Local xUnit tests (`scripts/test.sh`) |
| `Bublock/RiftRoulette/logs/` | Old pull path (unused; kept only for the git-ignored `.gitkeep`) |
| `archive/` | Frozen oracles (`RiftRumble/`, the pre-rename game plugin; DevTools; CleanSlate) |
| `lib/` | Shared Deadworks / build libraries |
| `ref/` | Shared reference assemblies |

## Discoveries

New research finds that are **highly useful** or **difficult to find** go here
in the same change. Detailed verified narrative from earlier sessions lives in
`chat-handoff.md`; Discoveries is for searchable, non-obvious facts going forward.

### Entry template

```markdown
### YYYY-MM-DD — Short title

- **Why hard / useful:** …
- **Verified fact:** …
- **Link / path:** …
```

### Entries

### 2026-09-26 — Each plugin DLL is an isolated load context

- **Why hard / useful:** Not stated in the docs; decides whether plugins can share code or state.
- **Verified fact:** `PluginLoader` creates a collectible `PluginLoadContext` (an `AssemblyLoadContext`) per plugin DLL. Only `DeadworksManaged.Api`, the host `DeadworksManaged` assembly, and `Google.Protobuf` are shared across contexts. Other dependencies resolve per plugin from its `deps.json`, so statics and types from any other assembly are **not** shared between DLLs, and typed calls across DLLs are impossible. The plugin DLL itself is loaded from a byte stream (not file-locked).
- **Link / path:** [managed/PluginLoader.cs](https://github.com/Deadworks-net/deadworks/blob/main/managed/PluginLoader.cs) (`PluginLoadContext`, `BuildSharedAssemblies`, `LoadPlugin`)

### 2026-09-26 — One DLL can host many plugin classes

- **Why hard / useful:** Lets us split features into small reviewable plugin classes without cross-DLL messaging.
- **Verified fact:** `LoadPlugin` scans `assembly.GetTypes()` for every non-abstract `IDeadworksPlugin` type and instantiates **each one**, giving each its own `TimerService`, config, `OnLoad`, hooks, and commands. Classes in the same DLL share one load context, so they share statics and can call each other directly.
- **Link / path:** [managed/PluginLoader.cs](https://github.com/Deadworks-net/deadworks/blob/main/managed/PluginLoader.cs) (`LoadPlugin`, `pluginTypes` loop)

### 2026-09-26 — Every DLL in plugins/ is loaded as a plugin; commands only come from plugin classes

- **Why hard / useful:** Rules out shipping shared code as library DLLs.
- **Verified fact:** `LoadAll` loads every `plugins/*.dll` through `LoadPlugin`. `CommandRegistration.RegisterPluginCommands` only reflects over methods of each plugin instance's type, so `[Command]` methods in a referenced library (or on non-plugin classes) are never registered. Bublock therefore compiles `Shared/` and `Modules/` into each consuming DLL as source via `.projitems`.
- **Link / path:** [managed/PluginLoader.cs](https://github.com/Deadworks-net/deadworks/blob/main/managed/PluginLoader.cs), [managed/Commands/CommandRegistration.cs](https://github.com/Deadworks-net/deadworks/blob/main/managed/Commands/CommandRegistration.cs)

### 2026-09-26 — Timer is per plugin instance

- **Why hard / useful:** Shared services cannot call a static `Timer`.
- **Verified fact:** `DeadworksPluginBase` exposes `protected ITimer Timer => TimerResolver.Get(this)`. Timers belong to the plugin instance and are disposed when that plugin unloads. Services should take an `ITimer` from the calling plugin class.
- **Link / path:** [DeadworksManaged.Api/DeadworksPluginBase.cs](https://github.com/Deadworks-net/deadworks/blob/main/managed/DeadworksManaged.Api/DeadworksPluginBase.cs), [Timer/TimerResolver.cs](https://github.com/Deadworks-net/deadworks/blob/main/managed/DeadworksManaged.Api/Timer/TimerResolver.cs)

### 2026-09-26 — Command attribute options and server-side invocation

- **Why hard / useful:** Useful for internal/admin-only commands and console testing.
- **Verified fact:** `[Command("name", aliases...)]` registers `/name`, `!name`, and `dw_name`. Options: `ServerOnly` (refuses player callers; `dw_` still runs from the server console / `Server.ExecuteCommand`), `ChatOnly`, `ConsoleOnly`, `SuppressChat`, `Hidden` (omit from `dw_help`), `Description`. A caller parameter may be nullable; server-console calls bind it as `null` and replies go to the server console. Throwing `CommandException` replies with its message to the caller.
- **Link / path:** [DeadworksManaged.Api/Commands/CommandAttribute.cs](https://github.com/Deadworks-net/deadworks/blob/main/managed/DeadworksManaged.Api/Commands/CommandAttribute.cs), [managed/Commands/CommandRegistration.cs](https://github.com/Deadworks-net/deadworks/blob/main/managed/Commands/CommandRegistration.cs)

### 2026-09-26 — Plugin Assembly.Location is empty; find managed/ via the API assembly

- **Why hard / useful:** Needed to pick a server path for plugin-written files (logs).
- **Verified fact:** `LoadPlugin` loads plugin DLLs with `LoadFromStream` (bytes, so the file isn't locked). Assemblies loaded from a stream have `Assembly.Location == ""`, so a plugin cannot locate itself. The shared `DeadworksManaged.Api` assembly is loaded from disk, so `Path.GetDirectoryName(typeof(IDeadworksPlugin).Assembly.Location)` gives `game/bin/win64/managed/`. Bublock logs go to `<managed>/../bublock/logs/<Dll>/` (`LogPaths.ResolveRoot`). Verified on 2026-09-27: the server writes to `Z:\gameserver\server\game\bin\win64\bublock\logs\<Dll>\` (Windows host), which SFTP shows as `/server/game/bin/win64/bublock/logs/`; `dw_dev_logpath` prints it.
- **Link / path:** [managed/PluginLoader.cs](https://github.com/Deadworks-net/deadworks/blob/main/managed/PluginLoader.cs) (`LoadPlugin`), `Bublock/Shared/Logging/LogPaths.cs`

### 2026-09-26 — Host keeps data beside managed/, not inside it

- **Why hard / useful:** Files written under `managed/` can be lost.
- **Verified fact:** The host's `ConfigManager`, `DeadworksConfig`, and `PluginStateManager` all write to `<managed>/../configs/` (e.g. `game/bin/win64/configs/plugins.jsonc`, `configs/<PluginClass>/<PluginClass>.jsonc`). A source comment says this is so they "survive the post-build rmdir of managed/". Plugin-owned data (like logs) should follow the same sibling convention.
- **Link / path:** [managed/ConfigManager.cs](https://github.com/Deadworks-net/deadworks/blob/main/managed/ConfigManager.cs) (`Initialize`), [managed/PluginStateManager.cs](https://github.com/Deadworks-net/deadworks/blob/main/managed/PluginStateManager.cs)

### 2026-09-26 — Closing plugin resources on unload without load-order coupling

- **Why hard / useful:** Many plugin classes share one DLL; any one of them closing shared files in `OnUnload` would break the others.
- **Verified fact:** Each plugin DLL has its own collectible `AssemblyLoadContext`; `UnloadPlugin` calls every plugin's `OnUnload`, then `Context.Unload()`, which raises `AssemblyLoadContext.Unloading`. Subscribing via `AssemblyLoadContext.GetLoadContext(typeof(X).Assembly)!.Unloading` lets per-DLL statics (e.g. the Bublock log hub) close files once, after all plugin classes have unloaded.
- **Link / path:** [managed/PluginLoader.cs](https://github.com/Deadworks-net/deadworks/blob/main/managed/PluginLoader.cs) (`UnloadPlugin`), `Bublock/Shared/Logging/BublockLog.cs`

### 2026-09-26 — Plugins can create convars and list loaded plugins

- **Why hard / useful:** Cross-DLL state or health checks, if ever needed (not used for composition).
- **Verified fact:** `ConVar.Create(name, defaultValue, description, serverOnly)` creates a convar; any plugin can read it with `ConVar.Find(name)?.GetString()` / `GetInt()`. `[ConVar("name")]` on a property exposes a plugin convar. `PluginRegistry.GetLoadedPluginNames()` returns loaded plugin names. Whether `Server.ExecuteCommand` runs immediately or is queued is **unverified**.
- **Link / path:** [DeadworksManaged.Api/ConVar.cs](https://github.com/Deadworks-net/deadworks/blob/main/managed/DeadworksManaged.Api/ConVar.cs), [ConCommands/ConVarAttribute.cs](https://github.com/Deadworks-net/deadworks/blob/main/managed/DeadworksManaged.Api/ConCommands/ConVarAttribute.cs), [PluginRegistry.cs](https://github.com/Deadworks-net/deadworks/blob/main/managed/DeadworksManaged.Api/PluginRegistry.cs)

### 2026-09-26 — World text boards can be updated in place

- **Why hard / useful:** The archive redraws boards by deleting and recreating every `point_worldtext`; in-place updates avoid that.
- **Verified fact:** `CPointWorldText.Create(message, position, fontSize = 100, worldUnitsPerPx = null, fontName = null, r, g, b, a, reorientMode = 0)` returns the board. Boards support `SetMessage(text)`, `SetColor(r, g, b[, a])`, and properties `Enabled`, `Fullbright`, `FontSize`, `WorldUnitsPerPx`, `DepthOffset`, `FontName`, `ColorABGR`, `JustifyHorizontal`, `JustifyVertical`. For keys `Create` doesn't expose, spawn `point_worldtext` with `CEntityKeyValues` (`message_text`, `font_name`, `fullbright`, `reorient_mode`, `depth_render_offset`, `justify_horizontal`). Without `fullbright 1` text is tinted by world lighting. Fonts come from the player's OS (use Windows defaults).
- **Link / path:** [docs: World Text](https://docs.deadworks.net/api-reference/world-text), `Bublock/Modules/WorldText/WorldTextService.cs`

### 2026-09-26 — Entity references stay safe across ticks via IsValid

- **Why hard / useful:** Lets services keep board/entity references in a registry without stale-handle bugs.
- **Verified fact:** `CBaseEntity.IsValid` re-resolves the entity through the handle table (serial-number aware), so it becomes `false` after `Remove()`, a disconnect, or a map change. `Remove()` is deferred to end of frame. `EntityData<T>` is per-entity storage that auto-evicts when the entity is deleted.
- **Link / path:** [docs: Entities](https://docs.deadworks.net/api-reference/entities) (NativeEntity, EntityData)

### 2026-09-26 — Command argument parsing and caller rules

- **Why hard / useful:** Needed for commands that take free text or run from the server console.
- **Verified fact:** Arguments split on spaces; double quotes keep text together (`\"` and `\\` escapes). `params string[]` collects the remaining arguments; a `string[]` parameter named `rawArgs` gets the raw split. Typed args: `string`, `bool`, `int`, `long`, `float`, `double`, enums; optional parameters work. Too many args are rejected with an automatic usage message. `CCitadelPlayerController` (non-null) means players only; `CCitadelPlayerController?` also allows the server console (`null`). `throw new CommandException(msg)` replies to the caller (chat for chat commands, console for console). `Target` arguments and `Permission` are "coming soon" in the docs.
- **Link / path:** [docs: Commands](https://docs.deadworks.net/api-reference/commands), `Bublock/Shared/Auth/AdminCommand.cs`

### 2026-09-26 — Player eye position and angles

- **Why hard / useful:** Placing things in front of a player (boards, markers).
- **Verified fact:** `CCitadelPlayerPawn` has `EyePosition` (origin + view offset, ~72 units up), `EyeAngles` (networked, ~0.18 degree precision), `ViewAngles` (full precision), and `CameraAngles`. `Teleport(angles: ...)` rotates the model, not the client camera; camera control uses `CCitadelUserMsg_SetClientCameraAngles`.
- **Link / path:** [docs: Players](https://docs.deadworks.net/api-reference/players), [docs: Entities — Transform](https://docs.deadworks.net/api-reference/entities)

### 2026-09-26 — Code that sends net messages needs a Google.Protobuf reference

- **Why hard / useful:** Compiling a module into a new project (e.g. a test project) fails with CS0311 / CS0012 on `NetMessages.Send`.
- **Verified fact:** `NetMessages.Send<T>` constrains `T` to `Google.Protobuf.IMessage<T>`, so any project that compiles code calling it (`MovementService.SetViewAngle`) must reference `$(DeadworksLibDir)/Google.Protobuf.dll` alongside `DeadworksManaged.Api.dll`. The plugin csprojs and `Tests/Modules.Tests` already do. Do not copy the DLL into the plugins folder (the host already provides it).
- **Link / path:** `Bublock/RiftRoulette/RiftRoulette.csproj`, `Bublock/Tests/Modules.Tests/Modules.Tests.csproj`

### 2026-09-26 — Finding players by slot, and team change caveat

- **Why hard / useful:** Slot-targeted admin commands and team moves.
- **Verified fact:** `Players.FromSlot(int)` returns the controller in a slot (or null) and `Players.IsConnected(slot)` checks for a fully connected player; `Players.GetAllControllers()` includes not-yet-connected ones, `Players.GetAll()` only fully connected ones. Bublock keeps `Players.GetAll().FirstOrDefault(p => p.Slot == slot)` (archive `/kick` behavior, fully connected only). `CCitadelPlayerController.ChangeTeam(int)` has a documented visual-update caveat; the docs suggest the `citadel_change_team` modifier for in-match switches. The archive also calls a `ChangeTeam(int, bool)` overload (`ChangeTeam(2, true)`) whose bool is not documented on the Players page.
- **Link / path:** [docs: Players](https://docs.deadworks.net/api-reference/players), `Bublock/RiftRoulette/Lobby/LobbyService.cs`

### 2026-09-26 — player_death gives base controller / pawn types

- **Why hard / useful:** Passing event players into helpers typed for Citadel classes fails to compile.
- **Verified fact:** `PlayerDeathEvent.UseridController` is a `CBasePlayerController` and `UseridPawn` a `CBasePlayerPawn` (not the Citadel subclasses). `Slot`, `LifeState`, `Health`, `Position`, and `ToPlayerRef()` work on the base types; use `.As<CCitadelPlayerController>()` for Citadel members (as `player_spawn` does).
- **Link / path:** `Bublock/RiftRoulette/Lobby/LobbyPlugin.cs`, `LobbyService.LogDeath`

### 2026-09-26 — Timers return cancellable handles

- **Why hard / useful:** Needed to stop a running rift (`/rift_cancel`); the API surface is not documented in the workspace and `lib/` has no XML docs.
- **Verified fact (reflection on `lib/DeadworksManaged.Api.dll`):** `ITimer.Once(Duration, Action)`, `ITimer.Every(Duration, Action)`, and `ITimer.Sequence(Func<IStep, Pace>)` all return `IHandle`; `ITimer.NextTick(Action)` returns void. `IHandle` has `Cancel()`, `IsFinished`, and `CancelOnMapChange()` (returns the handle). `IStep` has `Run` (int), `ElapsedTicks` (long), `Wait(Duration)`, and `Done()`. `CBaseEntity.EntityIndex` is `int`; `Entities.ByDesignerName(string)` returns `IEnumerable<CBaseEntity>`. Implicit usings make `ITimer` ambiguous with `System.Threading.ITimer`; alias it (`using ITimer = DeadworksManaged.Api.ITimer;`).
- **Link / path:** `Bublock/RiftRoulette/Rift/RiftService.cs`; reflect with `Assembly.LoadFrom` in `dotnet fsi`

### 2026-09-27 — Uploading a plugin DLL hot-reloads it

- **Why hard / useful:** Tells you whether a push needs a server restart.
- **Verified fact:** After `deploy.sh` `put` the three DLLs over the live ones (01:18:43 UTC), each plugin logged `Loaded Reload=True` within about 3 s. At 01:18:56 all three loaded again with `Reload=False` and RiftRoulette logged `Server startup Map=dl_midtown` (cause of the second load not identified). Timer-driven startup work (CleanSlate's 2 s cleanup, Draft's next-tick board draw) had not run 25 s later with no players connected, so timers may not tick on an empty server; unverified.
- **Link / path:** `Bublock/logs/*/master-20260927.log`; `Bublock/scripts/deploy.sh`

### 2026-09-26 — dw_help is console-only; plugins cannot read the command index

- **Why hard / useful:** Decided whether players need a chat command list. The docs only say `Hidden` omits a command from `dw_help`.
- **Verified fact:** `ConCommandManager.Initialize` registers `dw_help` as a built-in **console** command (`serverOnly: false`, so players can run it from the game console); there is no chat `/help`. It prints every non-hidden `command` and `chat` entry from `PluginRegistrationTracker`, admin commands included, to the caller's console. `PluginRegistrationTracker` is `internal`, so plugins cannot read it; Bublock's `/commands` reflects `[Command]` attributes instead. `CommandAttribute` has `Names` (string[]), `Description` (defaults to `""`, not null), `ServerOnly`, `ChatOnly`, `ConsoleOnly`, `SuppressChat`, `Hidden`; constructor `(string name, params string[] aliases)`.
- **Link / path:** [ConCommandManager.cs](https://github.com/Deadworks-net/deadworks/blob/main/managed/ConCommandManager.cs), [CommandRegistration.cs](https://github.com/Deadworks-net/deadworks/blob/main/managed/Commands/CommandRegistration.cs); `Bublock/RiftRoulette/Lobby/CommandList.cs`

### 2026-09-26 — Log templates: don't write Name= before {Name} (fixed in formatter)

- **Why hard / useful:** Every log line built as `"Selected hero Hero={Hero}"` rendered `Hero=Hero=Shiv` before the formatter fix, because `RenderTemplate` always writes `Name=value` for `{Name}`.
- **Verified fact:** `LogFormatter.RenderTemplate` now skips the name when the template already ends with `Name=` right before the placeholder (exact name, at a word start), so both `{Hero}` and `Hero={Hero}` render `Hero=Shiv`. Covered by `LogFormatterTests.RenderTemplate_DoesNotRepeatNameWrittenInTemplate`.
- **Link / path:** `Bublock/Shared/Logging/LogFormatter.cs`

### 2026-09-27 — On-screen HUD announcement banner: CCitadelPlayerController.HudAnnounce

- **Why hard / useful:** Shows match state on screen instead of chat; not in the workspace docs.
- **Verified fact (decompiled `lib/DeadworksManaged.Api.dll`):** `public void HudAnnounce(string title = "", string description = "")` on `CCitadelPlayerController` sends `CCitadelUserMsg_HudGameAnnouncement { TitleLocstring = title, DescriptionLocstring = description }` to `Recipients` (`RecipientFilter.Single(Slot)`), so call it per player to reach everyone. `ref/MatchStart.dll` uses it with plain text: `controller.HudAnnounce("Match started", "Started by " + who)` for each of `Players.GetAllControllers()`. The same API also has `CUserMessageHudMsg`, `CUserMessageHudText`, and `CCitadelUserMsg_HudError` messages (unused).
- **Link / path:** `Bublock/Modules/Hud/HudService.cs`; `ref/MatchStart.dll`

### 2026-09-27 — Decompiling the Deadworks API with ilspycmd

- **Why hard / useful:** `lib/` has no XML docs; decompiling shows exact signatures and what a helper sends.
- **Verified fact:** `~/.dotnet/tools/ilspycmd lib/DeadworksManaged.Api.dll > /tmp/dwapi.cs` (about 260k lines) then `rg` it. Found this way: `PluginBase` hooks (`OnTakeDamage`, `OnModifyCurrency`, `OnChatMessage`, `OnEntityCreated` / `Spawned` / `Deleted`, `OnGameStateChanged`, `OnPawnHeroInitialized`, ...); `[GameEventHandler("player_death")]` maps to `PlayerDeathEvent`; `CBaseEntity.Health` and `TeamNum` have setters (`MaxHealth` is read-only; setting health on a hero pawn is untested); `int.Seconds()` / `double.Seconds()` return `Duration` for `ITimer.Once`. Works the same on `ref/*.dll` to see how other plugins call the API.
- **Link / path:** `lib/DeadworksManaged.Api.dll`, `ref/`

### 2026-09-27 — Rift winner team from the first new trooper (to confirm in game)

- **Why hard / useful:** Scoring a captured rift needs the capturing team; KOTH exposes no winner field we know of.
- **Verified fact:** Unconfirmed assumption: the troopers a captured rift spawns belong to the capturing team, so `newTrooper.TeamNum` (Sapphire 3, Amber 2) is the winner. `RiftService` logs it as `Rift finished ... TrooperTeam=` in `rift-*.log` and scores with it; an unexpected value logs a Warning in `match-*.log` and scores nothing. Update this entry after the first playtest.
- **Link / path:** `Bublock/RiftRoulette/Rift/RiftService.cs` (`WatchOutcome`), `Bublock/RiftRoulette/GameLoop/MatchService.cs`

### 2026-09-27 — Renaming a plugin DLL leaves the old one loaded

- **Why hard / useful:** A rename looks like a normal upload, but the server keeps both.
- **Verified fact:** Deadworks loads every `plugins/*.dll`; uploading `RiftRoulette.dll` does not replace `RiftRumble.dll`, so both would load and register the same command names. The old DLL must be deleted from `plugins/` (deleting it unloads the plugin, like an upload reloads it). Logs follow the assembly name (`BublockLog` uses `assembly.GetName().Name`), so the rename also starts a new server log folder and `[<Dll>]` prefix; the old folder is not covered by retention (each DLL only prunes its own folder). `deploy.sh` handles both through `RETIRED_PLUGINS`.
- **Link / path:** `Bublock/scripts/deploy.sh`, `Bublock/Shared/Logging/BublockLog.cs`

### 2026-09-27 — /rift_cancel cannot remove a spawned rift

- **Why hard / useful:** Explains a rift left on the map after a cancel.
- **Verified fact (in game):** `CancelRift` parks the KOTH scheduler and ends our round, but the rift objective that already spawned stays. Parking only stops future spawns. No safe removal is known; removing `citadel_item_koth_spawner` / `citadel_koth_cashin` with `Remove()` is untested and may upset the game's scheduler.
- **Link / path:** `Bublock/RiftRoulette/Rift/RiftService.cs` (`CancelRift`)

### 2026-09-27 — A live rift is its cash-in, not its spawner

- **Why hard / useful:** Looking for the leftover rift by `citadel_item_koth_spawner` never finds it.
- **Verified fact (logs, session ee5212e0):** the spawner appears about 12 ms after the forced spawn, spawns `citadel_koth_cashin` about 1.1 s later, and is gone by the next round (`Existing=0` on every `Spawning rift`, even with a leftover up). The live objective is the cash-in. While a leftover cash-in is up, a forced spawn does nothing: after a live cancel at 05:35:18, the next two rounds timed out, and a new rift spawned only 72 s after the leftover's own spawn (it gives up at about 60 s). So any cash-in present before the forced spawn is a leftover, and `RiftService` adopts it right away. The cash-in position relative to the rift position is not yet logged (the adoption Warning now logs it).
- **Link / path:** `Bublock/RiftRoulette/Rift/RiftService.cs` (`WaitForSpawner`, `FindRiftOnMap`)

### 2026-09-27 — Deadworks item, ability, and hero-reset API

- **Why hard / useful:** Everything needed to give a player a full build, without shop purchases.
- **Verified fact:** On `CCitadelPlayerPawn`:
  - `AddItem(string itemName, bool enhanced = false)` grants an item as owned, for free, by class name (for example `upgrade_rapid_rounds`). It returns the `CCitadelBaseAbility`, or null when refused.
  - Also: `TryAddItem(name, imbueSlot, out item)`, `ImbueItem(item, slot)`, `CanImbue(name, slot)`, `SellItem`, `RemoveItem`.
  - `ResetHero(bool resetAbilities = true)` wipes items and abilities and triggers the starting-souls grant (`EStartingAmount`).
  - `SwapOrReset(hero, onReady)` / `OnceHeroInitialized(action)`.
  - `Level` has a setter. `SetCurrency` / `ModifyCurrency(ECurrencyType, amount, ECurrencySource, silent)`.
  - `Heal(float)`, `GetMaxHealth()`.
  - `AbilityComponent.FindAbilityByName` / `GetAbilityBySlot` / `Abilities`.

  On `CCitadelBaseAbility`: `UpgradeBits` (bit 0 = unlocked, `IsUnlocked`) and `AbilitySlot`. `ItemInfo.Exists` / `CanBeImbued` are static checks by name. The `Heroes` enum values equal Deadlock hero IDs (Haze = 13); `HeroTypeExtensions.ToHeroName` gives `hero_<enum lowercase>`, `ToDisplayName` the game name.
- **Link / path:** `lib/DeadworksManaged.Api.dll` (decompile), `Bublock/Modules/Loadout/LoadoutService.cs`

### 2026-09-27 — Official Deathmatch example: swap hero, then rebuild

- **Why hard / useful:** A known-good order for hero swap + build that we copy instead of guessing.
- **Verified fact:** `examples/plugins/DeathmatchPlugin` in the Deadworks repo runs these steps in order:
  1. `controller.SelectHero(hero)`.
  2. Wait `Timer.Once(1.Seconds())` (hero loading is async).
  3. `pawn.ResetHero()`.
  4. `Heal(GetMaxHealth())`.
  5. Upgrade the signature abilities with `UpgradeBits |= 0b11111`, which maxes them.
  6. `AddItem(name)` for each item.

  On `EStartingAmount` in `OnModifyCurrency` it sets `Level = 36` and gold directly, then runs `ModifyCurrency(EGold, 0, ECheats, silent: true)` to recalculate stats. That avoids per-level UI events. `ItemRotationPlugin` and `ItemTestPlugin` show `AddItem` / `RemoveItem` / `TryAddItem` too.
- **Link / path:** [examples/plugins/DeathmatchPlugin](https://github.com/Deadworks-net/deadworks/tree/main/examples/plugins/DeathmatchPlugin) (clone with `git clone --depth 1 https://github.com/Deadworks-net/deadworks.git`)

### 2026-09-27 — Deadlock API: top builds by real matches

- **Why hard / useful:** Real build data (items in order, ability order) for every hero, plus which builds people actually play.
- **Verified fact:**
  - `GET https://api.deadlock-api.com/v1/analytics/hero-build-stats/{hero_id}?min_unix_timestamp=` returns `hero_build_id`, `matches`, `wins`, `players` per build (the build selected at match start). The analytics endpoints share a limit of 200 requests per minute per IP.
  - `GET /v1/builds?build_id=&only_latest=true` returns `hero_build.details.mod_categories[].mods[].ability_id` (item IDs in build-editor order, with `imbue_target_ability_id`), plus `details.ability_order.currency_changes[]`. In those changes, `currency_type` 2 means unlock and 1 means upgrade, and `delta` is -1/-2/-5 per tier. Some builds repeat the whole order.
  - `/v1/builds?hero_id=&sort_by=weekly_favorites|favorites` is the popularity fallback. `num_favorites` can be null.
  - `GET /v1/assets/items` maps `id` to `class_name`, `type` (`upgrade` / `ability` / `weapon`), `shopable`, `component_items`.
  - `GET /v1/assets/heroes` gives `player_selectable`, `in_development`, `disabled`.
  - The old host `assets.deadlock-api.com` no longer resolves; assets are under `api.deadlock-api.com/v1/assets/`.
  - OpenAPI: `https://api.deadlock-api.com/openapi.json`.
- **Link / path:** `Bublock/scripts/fetch-builds.py`, `Bublock/scripts/fetch-builds.py.md`

### 2026-09-27 — A `Random` namespace hides `System.Random`

- **Why hard / useful:** Folder names become namespaces here; this one breaks `Random.Shared` in sibling code.
- **Verified fact:** A namespace `RiftRoulette.Random` makes the bare name `Random` resolve to that namespace inside every `RiftRoulette.*` namespace, so `Random.Shared` fails to compile. The Random mode folder is therefore `RiftRoulette/RandomMode/`.
- **Link / path:** `Bublock/RiftRoulette/RandomMode/`

### 2026-09-27 — player_death carries up to five assisters; the game's own K/D/A counters can't be reset

- **Why hard / useful:** Needed for match stats; the assister fields are not in the workspace docs.
- **Verified fact (decompiled `lib/DeadworksManaged.Api.dll`):** `PlayerDeathEvent` has `UseridController`, `AttackerController`, and `Assister1controller` ... `Assister5controller`, all `CBasePlayerController?` (null for non-players such as troopers). `PlayerDataGlobal` exposes read-only `PlayerKills`, `PlayerAssists`, `Deaths`, which count the whole game session and have no setter, so per-match stats are tracked by Bublock (`Stats/StatsLedger`). Whether the game actually fills the assister fields is still to confirm in game.
- **Link / path:** `Bublock/RiftRoulette/Stats/StatsService.cs` (`RecordDeath`)

### 2026-09-27 — Killing a pawn: Hurt, not Kill

- **Why hard / useful:** Upstream examples call `Kill()`, which our `lib/` build does not have.
- **Verified fact:** `CBaseEntity.Hurt(float damage, CBaseEntity? attacker = null, CBaseEntity? inflictor = null, CBaseEntity? ability = null, int damageType = 0)` exists in `lib/`; the Random mode hero guard uses `pawn.Hurt(1_000_000f)`. If the pawn is still alive afterwards the guard falls back to rebuilding in place. Reliability to confirm in game.
- **Link / path:** `Bublock/RiftRoulette/Lobby/HeroLock.cs` (`Enforce`)

### 2026-09-27 — Deadlock API: optional item groups, item costs, banned items

- **Why hard / useful:** Lets a build give one pick from each "choose one" group and lets us value loadouts.
- **Verified fact:** In `/v1/builds`, `hero_build.details.mod_categories[].optional` is `true` for option groups and `null` (not `false`) for required groups. `/v1/assets/items` has `cost` per item: tier 1 = 800, 2 = 1600, 3 = 3200, 4 = 6400, 5 = 9999. Monster Rounds is class `upgrade_non_player_bonus`, Cultist Sacrifice is `upgrade_non_player_bonus_sacrifice` (3200 souls, id 709540378) and Golden Goose Egg is `upgrade_goose_egg`; `fetch-builds.py` drops all three (85 entries in the 2026-09-27 04:10 fetch) and writes `itemCosts`. Over the stored builds, the planned 9-item value ranges 9,600-32,000 souls with a median of 18,000.
- **Link / path:** `Bublock/scripts/fetch-builds.py`, `Bublock/Modules/Loadout/LoadoutPlanner.cs`

### 2026-09-27 — Refusing a connection: OnClientConnect returns bool

- **Why hard / useful:** Lets a plugin reserve slots (the admin seat) without kicking after the player loads.
- **Verified fact:** `DeadworksPluginBase.OnClientConnect(ClientConnectEvent args)` returns `bool`; the event carries `Slot`, `Name`, `SteamId`, `IpAddress` before the player is in game. Returning `false` refuses the connection (what the client sees is to confirm in game). `maxplayers` and `sv_visiblemaxplayers` are set through `ConVar.Find(...)?.SetInt`; whether `maxplayers` changes at runtime (vs. a launch parameter) is to confirm with `/seat_status`. Spectator team is assumed to be 1 (Source convention); `citadel_server_max_spectator_slots` defaults to 3.
- **Link / path:** `/tmp/dwapi.cs` decompile (see "Decompiling the Deadworks API"); `Bublock/RiftRoulette/Lobby/AdminSeat.cs`

### 2026-09-27 — Copying a hero exactly: ability points, unlocks, UpgradeBits, imbues

- **Why hard / useful:** A 1v1 copy needs the source player's unspent points and exact ability tiers, not a planned build.
- **Verified fact:** `ECurrencyType` has `EGold = 0`, `EAbilityPoints = 1`, `EAbilityUnlocks = 2`; `pawn.GetCurrency(type)` / `SetCurrency(type, int)` read and write each. `CCitadelBaseAbility` exposes `AbilityName`, `AbilitySlot`, `UpgradeBits` (get / set), `IsSignature` (slots `Signature1`-`Signature4` = 0-3), `IsItem` (name starts `upgrade_`), and `ImbuedAbilities` (names of the abilities an item is imbued onto). `pawn.Level` is settable; `pawn.ImbueItem(item, slot)` takes the target ability's slot. Walking `AbilityComponent.Abilities` gives items in the pawn's order. Whether raising `Level` alone grants ability points is to confirm in game (the 1v1 setup Debug line logs AP and unlocks).
- **Link / path:** `Bublock/Modules/Loadout/LoadoutService.cs` (`Capture`, `ApplySnapshot`)

### 2026-09-27 — dl_midtown map explorer export (entities + world mesh)

- **Why hard / useful:** Positions, classnames, keys, and brush volumes for every `dl_midtown` entity, plus the simplified world mesh the map explorer draws. The page only hands out 24-hour signed URLs.
- **Verified fact:** [Map explorer](https://deadworks.net/db/map) (`/db/map` and `/db/map/dl_midtown`) embeds three gzip files for the shown build in its loader payload. This snapshot is build **6698**, source `faf73184b902d52d719207179051a170a0014e0213ab807fb262e596c1e01217`, fetched 2026-09-27. Wireframe, Flat, and Both are view modes of one mesh (`m=wire|flat`; default both). The hash `c=` is the camera, not a file. Other maps on the page: `dl_hideout`, `new_player_basics`.
  - `entities.json` (6.6 MB): `map`, `entities` (5,405), `models` (664). Each entity has `index`, `classname`, `origin` `[x,y,z]`, `angles`, `scales`, `keys`, `outputs`. Z is up. 87 classnames. Gameplay counts include `info_team_spawn` 89 (teamnumber `2` and `3`: 44 each; `1`: 1), `info_neutral_trooper_spawn` 228, `info_trooper_spawn` 24, `info_super_trooper_spawn` 12, `citadel_zipline_path` 5, `citadel_zipline_path_node` 136. The two rifts: `info_koth_spawn_location` at `(-7560, 0, 424)` and `(7612, 0, 444)`; `citadel_capture_point` `[PR#]cp_york` at `(-8832, 0, 288)` and `[PR#]cp_park` at `(8832, 0, 256)`. Those X values match `WatchYellow` / `WatchGreen` in `Locations/RiftRouletteLocations.cs` (the watch spots sit higher, Z `1536`). 834 entities name a `model`; `models` holds `hulls`, `meshes`, `spheres`, `capsules`. 375 have `keys.box_mins` / `box_maxs`. 97 have `outputs` (`output`, `target`, `input`, `parameter`, `delay`, `times`).
  - `world.json` (79 KB): `levels` `[12, 48, 192]`, `triangles` `[4942410, 1592063, 193022]`, `sourceTriangles` `29306133`, `chunks` (137, keys like `-4,-2`). Each chunk has 3 LOD slots. A filled slot has `center`, `half`, `positions` `[byteOffset, count]`, `indices` `[byteOffset, count, bytesPerElement]` (`2` = uint16, `4` = uint32). The viewer reads `positions` as normalized `Int16` triples from `world.bin` and places the chunk at `center` scaled by `half`. LOD distances are 0 / 7,000 / 22,000 units.
  - `world.bin` holds the vertex and index buffers (40,947,768 bytes gzip, 73,857,088 unpacked, ~4.3 million vertices). A local copy lives at `maps/dl_midtown/world.bin.gz` for `scripts/check-spots.py`; it is git-ignored. Refresh steps below; use `GET` (a signed `HEAD` returns 403). A `curl` of the page with a browser user agent works; the signed URLs in it have `&` escaped as `\u0026`.
  - Refresh: open `https://deadworks.net/db/map/dl_midtown`, read the `streamController.enqueue` payload, and download `urls` for `world.json`, `world.bin`, and `entities.json`. The bucket path is `maps/dl_midtown/<source>/` on `t3.storageapi.dev`. Links expire 24 hours (`X-Amz-Expires=86400`). Files are gzip (`1f 8b`). Do not commit the signed query string.
- **Link / path:** `Bublock/RiftRoulette/reference/maps/dl_midtown/entities.json`, `Bublock/RiftRoulette/reference/maps/dl_midtown/world.json`, [deadworks.net/db/map](https://deadworks.net/db/map)

### 2026-09-27 — Silence, disarm and melee block: modifier names and states (disarm later dropped, see "Disarm blocks reloading")

- **Why hard / useful:** Deadworks has no "silence" helper; the right names are spread over community lists, and states on their own don't stick.
- **Verified fact:** `pawn.AddModifier(string name, KeyValues3? kv)` returns `CBaseModifier?` (null when refused); set the length with `using var kv = new KeyValues3(); kv.SetFloat("duration", seconds)`. `pawn.RemoveModifier(name)` returns bool. The game's own status effects are `modifier_citadel_silenced` and `modifier_citadel_disarmed` (they show the HUD icons). `pawn.ModifierProp` (`CModifierProperty`) has `SetModifierState(EModifierState, bool)`, `HasModifierState`, `HasModifier(name)`; `EModifierState.Silenced` = 15, `Disarmed` = 12, `MeleeDisabled` = 106 (melee has no status effect, so the state is the only block; `boss_victim_no_melee` is a candidate modifier). The docs say many states must be set every tick, so set them from `OnGameFrame(simulating, firstTick, lastTick)`. Modifiers are lost on death / hero reset, so re-add them. Still to confirm in game: that `MeleeDisabled` blocks melee, and whether a 100 000 s modifier shows a timer.
- **Link / path:** [deadlockmodding modifier list](https://deadlockmodding.pages.dev/modifier-list), `/tmp/dwapi.cs` decompile, `Bublock/Modules/Restraint/RestraintService.cs`

### 2026-09-27 — Hot reload runs OnLoad(isReload: true), not OnStartupServer

- **Why hard / useful:** Startup work silently stops happening after an upload. One upload left guardians and walkers on the map, the boards at the old spot, and `maxplayers` at 12.
- **Verified fact (server logs):** an upload hot-reloads each DLL: `OnLoad(true)` runs, `OnStartupServer` does not, and timers set by the previous load (CleanSlate's 2 s removal) are dropped with it. Every plugin whose startup work matters must redo it in `OnLoad` when `isReload` is true (CleanSlate cleanup, Lobby convars, Draft boards).
- **Link / path:** `Bublock/CleanSlate/CleanSlatePlugin.cs`, `Bublock/RiftRoulette/Lobby/LobbyPlugin.cs`, `Bublock/RiftRoulette/Draft/DraftPlugin.cs`

### 2026-09-27 — The archive's yellow rift starts were swapped

- **Why hard / useful:** Sapphire landed on Amber's half on yellow only; green was right, so it looked random.
- **Verified fact:** Sapphire is team 3, base at +y (spawns around y = +10,800 in `entities.json`); Amber is team 2 at -y. `yellow_sapphire` was at y = -2134. Swapped back; `RoundLocationsTests` asserts each team starts on its own half on both lanes. The map is point-symmetric, so the yellow board layout is green's turned half a turn (`Round/WatchLayout`).
- **Link / path:** `Bublock/RiftRoulette/Locations/RiftRouletteLocations.cs`, `Bublock/RiftRoulette/reference/maps/dl_midtown/entities.json`

### 2026-09-27 — Item actives need EModifierState.ItemsDisabled

- **Why hard / useful:** `modifier_citadel_silenced` and `EModifierState.Silenced` block abilities but not item actives (playtest).
- **Verified fact:** `EModifierState.ItemsDisabled` = 14 (next to `Muted` 13, `Silenced` 15). Set every frame like the other states; no curse needed, so the silence icon stays.
- **Link / path:** `/tmp/dwapi.cs` decompile, `Bublock/Modules/Restraint/RestraintService.cs`

### 2026-09-27 — Shops, the urn, and buying

- **Why hard / useful:** Removing shops or the urn the wrong way risks a crash, and items still need to reach players.
- **Verified fact:**
  - Shops on `dl_midtown`: 8 `citadel_shop_prop_dynamic` (kiosk models), 9 `trigger_item_shop` (buy zones, one per kiosk plus a base shop), 2 `trigger_item_shop_safe_zone` (center shops); `citadel_trigger_shop_tunnel` is not a shop. CleanSlate removes the props and sends `AcceptInput("Disable")` to the triggers (`CBaseEntity.AcceptInput(name, activator, caller, value)`).
  - The urn is the "crate" internally (`citadel_spawn_urn`, `citadel_crate_*`): 6 `item_crate_spawn` points, 2 `citadel_trigger_idol_return` delivery points. Turn it off with `citadel_crate_spawn_enabled 0` plus `citadel_crate_spawn_initial_delay` / `citadel_crate_respawn_interval` 999999 (and `citadel_crate_disable_early_spawn 1`, which has no flags in the cvar list and may not exist; `ConVar.Find` then skips it). Do not delete the spawn points.
  - `citadel_allow_purchasing_anywhere` is `sv, cl, rep, cheat`: replicated, switchable at runtime with `ConVar.SetInt`. `pawn.AddItem` does not need a shop or buying, so random builds and the 1v1 copy work with buying off.
- **Link / path:** `Bublock/CleanSlate/CleanSlateService.cs`, `Bublock/RiftRoulette/GameLoop/ShopAccess.cs`, `Bublock/RiftRoulette/reference/cvarlist.md`

### 2026-09-27 — No unstuck command; OnClientConCommand sees client commands

- **Why hard / useful:** Players up top used the escape-menu Unstuck to leave the watch spot.
- **Verified fact:** `reference/cvarlist.md` (which also lists commands) has no unstuck command or convar, and the Deadworks API has none. `DeadworksPluginBase.OnClientConCommand(ClientConCommandEvent)` gets `Controller`, `Command` and `Args` for client console commands and can block one by returning `HookResult.Stop`. Rift Roulette logs restrained players' commands to `watch-*.log` to find what Unstuck sends (if it is a console command at all), and meanwhile sends anyone who drops 300 units below the watch spot back up (`Round/WatchGuard`).
- **Link / path:** `/tmp/dwapi.cs` decompile, `Bublock/RiftRoulette/Round/WatchGuard.cs`

### 2026-09-27 — RiftService.NextSide flips at spawn, not at round end

- **Why hard / useful:** Anything that follows "the rift being fought" (the watch spot) goes to the wrong side if it reads `NextSide` mid-round.
- **Verified fact:** `RiftService.AlternateSide` runs when the rift spawns, so during a live round `NextSide` already points at the other rift; `CurrentSide` holds the side being fought. `CurrentSide` is cleared in `FinishRound`, which runs after the `ReturnPlayersToDraft` step, so at that step the rift still counts as running: pass `NextSide` explicitly there. Rule in `Round/WatchSpotRule.SideFor(running, current, next)`.
- **Link / path:** `Bublock/RiftRoulette/Rift/RiftService.cs`, `Bublock/RiftRoulette/Round/WatchSpot.cs`

### 2026-09-27 — Disarm blocks reloading; use ShootingDisabled

- **Why hard / useful:** Players up top could not reload while disarmed, so every round started with the fighters reloading.
- **Verified fact (playtest):** `modifier_citadel_disarmed` / `EModifierState.Disarmed` (12) stop reloading as well as shooting. The Deadworks API has no ammo or magazine accessor (no `m_iClip1` field or weapon class), so a refill on release is not possible without unverified schema names. `EModifierState` has `ShootingDisabled` = 62 (the restraint now uses it instead of disarm), plus `ReloadDisabled` = 112, `ManualReloadDisabled` = 114, `InfiniteClip` = 69 and `ShootingForcedOn` = 63. Whether `ShootingDisabled` alone blocks the gun is still to confirm in game.
- **Link / path:** `/tmp/dwapi.cs` decompile (`EModifierState`), `Bublock/Modules/Restraint/RestraintService.cs`

### 2026-09-27 — No way to add a single bot

- **Why hard / useful:** The idea was for a bot to capture a rift left on the map by a cancelled round; deferred.
- **Verified fact:** Deadworks has no bot API (`Server.ExecuteCommand` only runs console commands). `reference/cvarlist.md` has no command that adds one bot. The only spawn-type entries are `citadel_spawn_practice_bots` (+ `citadel_spawn_practice_bots_count`) and `citadel_spawn_all_heroes_in_a_line`. Bots can be removed with `bot_kick_all`, `citadel_bot_kick <name>` or `kickid <slot>`. Instead, a round whose rift does not spawn uses the rift already on the map (`RiftService.FindRiftOnMap`).
- **Link / path:** `Bublock/RiftRoulette/reference/cvarlist.md`, `Bublock/RiftRoulette/Rift/RiftService.cs`

### 2026-09-27 — ChangeTeam's bool is keepHero; mid-rift seat change dropped the client

- **Why hard / useful:** The bool looks like a force flag, and a team change mid-rift can drop the client with no server error.
- **Verified fact:** the decompiled API is `CCitadelPlayerController.ChangeTeam(int teamNum, bool keepHero = true)`. `AdminSeat.Sit` calls `ChangeTeam(1, false)`, yet at 05:26:02 (chat `/seat_spec` during the live yellow rift, r2) the log showed `PawnLeft=True`. Auto-start then saw 0 humans and ended the match and the rift in the same tick, and the client dropped 12 s later with no server exception. So `seat_spec` is now `ConsoleOnly` and refused while a rift round runs. `CommandAttribute` options: `ConsoleOnly`, `ChatOnly`, `ServerOnly`, `Hidden`, `SuppressChat` (see the command registration entry above). Still to confirm between rounds: whether team 1 is spectator and whether `keepHero: false` removes the pawn (`/seat_status` shows `TeamNum` and `Pawn`).
- **Link / path:** `Bublock/RiftRoulette/Lobby/AdminSeat.cs`, `Bublock/RiftRoulette/Lobby/LobbyPlugin.cs`, `Bublock/logs/RiftRoulette/lobby-20260927.log`

### 2026-09-27 — Spectate with MakeObserver; ChangeTeam(1, false) alone drops the client

- **Why hard / useful:** `keepHero: false` sounds like it removes the hero, but it does not; the client drops seconds later with no server error, and it happens between rounds too.
- **Verified fact:** in the later playtest, `dw_seat_spec` between rounds still logged `PawnLeft=True` after `ChangeTeam(1, false)`, and the admin's client dropped 12-23 s after each seat (two server crashes as seen by the players). The API has `CCitadelPlayerController.MakeObserver()`: `Pawn?.Remove()`, `SetPawn(null, retainOldPawnTeam: true)`, then spawns the observer pawn. `AdminSeat.Sit` now calls `ChangeTeam(1, false)` then `MakeObserver()` on the next tick (logs `Admin spectating ... HeroPawn= Observer=`). Still to confirm in game: the client stays connected, and `dw_seat_play` (`AdmitPlayer`) spawns a hero again from the observer pawn.
- **Pattern (session `c330f6a2`):** the team-1 hero only dropped the client while a rift was on the map. Seated at 08:30:16 with no rift, the client stayed 19 minutes and dropped at 08:49:48, 13 s after the first rift spawned (08:49:35). Seated 09:17:25, it dropped 13 s after r32 spawned (09:17:35). Seated 09:18:52 while r33's cash-in was still up (a cancel leaves the cash-in), it dropped 12 s later. The 05:26 drop was also mid-rift. The server kept running every time (no new session until the upload). The ~12 s gap looks like a client timeout, so the client likely died the moment a rift and a team-1 hero coincided. To test after `MakeObserver`: spectate with no rift up, then stay spectating while a rift spawns.
- **Confirmed (09:30, session `87131b9e`):** `MakeObserver` leaves the client connected (`HeroPawn=False Observer=observer`). The designer name of the observer pawn is `observer`.
- **Console caller can be null:** at 09:39 the seated admin's `dw_seat_play` logged `Admin command seat_play from server console`, meaning the caller bound as `null` (`ConCommandContext.CallerSlot < 0`). Either it was typed in the server console or the client's command came through without a slot; the logs cannot tell which. Admin console commands that default to "the caller" need a fallback when `caller` is null (`LobbyPlugin.SeatTarget` falls back to the connected player with the admin Steam ID, `AdminAuth.SteamIds`).
- **Link / path:** `Bublock/RiftRoulette/Lobby/AdminSeat.cs`, `/tmp/dwapi.cs` (decompiled `DeadworksManaged.Api`), `Bublock/logs/RiftRoulette/lobby-20260927.log`

### 2026-09-27 — Checking spots against the map mesh; the skybox floor is not in it

- **Why hard / useful:** Lets us place players without standing them in walls, offline, before an upload.
- **Verified fact:** `scripts/check-spots.py` decodes the explorer mesh in plain Python (no numpy needed; about 9 s). For each chunk it uses LOD 0: vertex = `center + int16 / 32767 * half`, then triangles from the index buffer, bucketed on a 128-unit grid. Per spot it casts a floor ray (from 24 above to 64 below), tests a 40-radius body from 18 to 80 above the floor with closest-point-on-triangle, and casts a line from the anchor to the spot at 48 up. The fight floors come out at the anchor heights (z 248 / 256), so the decoding is right. The skybox floor the watch spots stand on (z 1536) is **not** in the mesh; it is invisible collision, so the script only checks body room and line of sight up there. Hero start rows wider than about 200 units sideways clip buildings at the green and yellow starts (`green_sapphire` +300, `green_amber` -300, `yellow_sapphire` back row -200). The offset convention is forward = the anchor's yaw, right = `(sin yaw, -cos yaw)`, up = +z (`MovementLocation.Offset`).
- **Link / path:** `Bublock/scripts/check-spots.py`, `Bublock/RiftRoulette/Round/Data/spots.json`, `Bublock/RiftRoulette/Round/SlotSpots.cs`

### 2026-09-27 — Detecting patch damage: schema offsets, traces, and the API surface

- **Why hard / useful:** After a Deadlock patch, most breakage is silent (a missing convar, a renamed schema field or item just does nothing). These are the handles that make it visible.
- **Verified fact:** `SchemaAccessor<T>` resolves lazily through `NativeInterop.GetSchemaField` and keeps the result in an internal `Offset` property; there is no public "field exists" call, so `SelfTestService` reads `Offset` by reflection and treats <= 0 as not found (`Get` / `Set` would otherwise read or write at the object base). `ItemInfo.Exists(name)` is a native lookup (imbue effects >= 0) usable at any time, so every item name in `hero-builds.json` can be checked on the live server. `Trace.Ray(start, end, mask, ignore)` exists (returns `TraceResult` with `DidHit`, `HitPosition`, `Fraction`; returns no hit when the physics query is not ready). `ConVar.Find` returns null for a missing convar and every old call site ignored that; `Shared/ConVars/ServerConVars.TrySet` now warns. deadlock-api `/v1/assets/heroes` marks `hero_skyrunner` (our lobby hero) `player_selectable` false, so it is an internal hero that a patch could drop. Decompiling `lib/DeadworksManaged.Api.dll` with ilspycmd and keeping public signatures plus enum values (about 1,060 types, 119 enums) gives a diffable API surface; enum values are compiled into our DLLs, so a renumbered enum only shows up after a rebuild.
- **Link / path:** `Bublock/RiftRoulette/SelfTest/SelfTestService.cs`, `Bublock/scripts/patch-check.py`, `Bublock/RiftRoulette/reference/patch-day.md`

### 2026-09-27 — Blocking soul gains: OnModifyCurrency and ECurrencySource

- **Why hard / useful:** Kill bounties and passive income scale with the game clock (`citadel_*_gold_reward_bonus_per_minute`, `citadel_player_gold_comeback_*`, all devonly), which keeps running for hours on a playtest server, so mid-round souls snowballed one team. There is no single convar to turn souls off.
- **Verified fact (from the decompiled API):** `PluginBase.OnModifyCurrency(ModifyCurrencyEvent args)` runs before every currency change. `args` has `Pawn`, `CurrencyType` (`ECurrencyType.EGold`, `EAbilityPoints`, ...), `Amount`, `Source`, `Silent`, `ForceGain`, `SpendOnly`; returning `HookResult.Stop` blocks it. `ECurrencySource` names every origin: `EPlayerKill` 11, `EPlayerKillAssist` 12, `EOrb*` 22-36, `EPlayerKillComeback` 39, `ETeamBonus` 40, `EStartingAmount` 5 (the `ResetHero` grant), `ECheats` 7 (what loadouts pass to `ModifyCurrency(EGold, 0, ...)`), `EItemSale` 2, `EItemPurchase` 0. Which source the passive trickle uses is not confirmed; the self-test counters `soul_blocked_<Source>` show it after a playtest.
- **Link / path:** `Bublock/RiftRoulette/GameLoop/SoulRule.cs`, `Bublock/RiftRoulette/GameLoop/GameLoopPlugin.cs`

### 2026-09-27 — Rift troopers keep spawning after the round ends

- **Why hard / useful:** Troopers piled up over a long match with no error anywhere.
- **Verified fact (logs):** A captured rift's wave is 7 `npc_trooper`, spawned over a few seconds. The round ends 3 s after the first one, so round-end cleanup removed 3-7 (`TroopersRemoved=`), and the rest spawned afterwards. The old cleanup only removed troopers missing from the rift-start snapshot, so each late trooper was "already there" at the next rift and never removed. Lane troopers are off (`citadel_trooper_spawn_enabled 0`), so every `npc_trooper` is a rift trooper: cleanup now removes all of them and sweeps again 5 s and 10 s later. Do not sweep at rift start: `Remove()` is deferred to the end of the frame and a reused entity index could hide the first new trooper from the capture check.
- **Link / path:** `Bublock/RiftRoulette/Rift/RiftService.cs`

### 2026-09-27 — Driving a spectator camera; ult names and ability events

- **Why hard / useful:** Needed for the stream camera. The observer API has a trap, and ultimate class names are not the names shown in game.
- **Verified fact (decompiled API):** `CBasePlayerPawn.ObserverServices` (`CPlayer_ObserverServices`) has `ObserverMode`, `ObserverTarget`, `SetObserverMode(ObserverMode_t)` and `SetObserverTarget(CBaseEntity?)` (returns bool). `ObserverMode_t` is `None, Fixed, InEye, Chase, Roaming`; `InEye` is the game's PlayerView (`citadel_spectator_mode` 3), `Roaming` is free cam. `IsValidObserverTarget` rejects `TeamNum == 3`, which is Sapphire in Deadlock, so never use it. A seated admin's pawn is the `observer` pawn (`controller.Pawn.DesignerName`); compare targets by `EntityHandle`.
- **Corrected (in game, 2026-09-28):** `spec_player`, `spec_mode`, `spec_next`, `spec_prev` are `clientcmd_can_execute`, which is not `server_can_execute`. The client refuses them from `Server.ClientCommand` (console: `Cannot execute concommand spec_mode: missing required FCVAR flag`). Only `echo`, `play` and `cl_soundscape_flush` are `server_can_execute` in the cvarlist, so the server cannot switch a client's spectator camera by command.
- **Verified fact (decompiled API):** the game event `player_used_ability` maps to `PlayerUsedAbilityEvent` with `Player` (pawn), `Caster` (entity), `Abilityname`, `Annotation`. `ability_cast_succeeded` only has `entindex_ability`. `OnAbilityAttempt` sees `InputButton.Ability1..4` presses (not casts).
- **Verified fact (API):** a hero's ultimate class name is `items.signature4` in `https://assets.deadlock-api.com/v2/heroes` (for example Lash `citadel_ability_lash_ultimate`, Seven `citadel_ability_storm_cloud`, Pocket `synth_affliction`). The local `snapshot.json` has ability names but not slots.
- **To confirm in game:** whether the server fires `player_used_ability` (self-test Events count; `lobby-*.log` `Ability name seen for the first time`), whether its `Abilityname` matches the `signature4` names, and whether teleporting a `Roaming` observer pawn moves the camera (`spec_overview`; `spectate-*.log` `Parked ... After=`).
- **Removed (2026-09-28):** the big-ult top-down (`BigUlts`, `OverviewRule`, the `player_used_ability` handler) was deleted; the facts above stay for reference.
- **Link / path:** `Bublock/Modules/Spectate/SpectateService.cs`, `Bublock/RiftRoulette/Lobby/StreamCam.cs`

### 2026-09-27 — Blocking damage and NPC targeting (OnTakeDamage)

- **Why hard / useful:** Deployables that outlive the round (McGinnis turrets) shot players waiting up top. Neither the hook nor the state is in the workspace docs.
- **Verified fact (decompiled API):** `PluginBase.OnTakeDamage(TakeDamageEvent args)` returns `HookResult` (`Continue` 0, `Stop` 1, `Handled` 2); returning `Stop` blocks the hit, as it does for `OnModifyCurrency`. `TakeDamageEvent` has `Entity` (the victim, `CBaseEntity`) and `Info` (`CTakeDamageInfo`: `Attacker`, `Inflictor`, `Ability`, `Originator`, `Damage`, `DamageType`, `DamageFlags`). `entity.As<CCitadelPlayerPawn>()` returns null for non-heroes (`As<T>` checks `Is<T>()`), and `CCitadelPlayerPawn.Controller` gives the player. `EModifierState` also has `IgnoredByNpcTargeting` (33), `Invulnerable` (19), `NoIncomingDamage` (142), `TechUntargetableByEnemies` (22), `InvisibleToEnemy` (31), `OutOfGame` (26).
- **To confirm in game:** that `Stop` blocks turret damage (self-test counter `damage_blocked_restrained`), and whether turrets honor `IgnoredByNpcTargeting`.
- **Link / path:** `Bublock/RiftRoulette/GameLoop/GameLoopPlugin.cs`, `Bublock/Modules/Restraint/RestraintService.cs`

### 2026-09-28 — Client crash when the server and the admin both move the spectator camera (to confirm)

- **Why hard / useful:** The server log shows only a disconnect; the crash is on the admin's PC. Two crashes on 2026-09-28: 02:27:45, 10 s after the stream camera followed a player whose hero was 1 s old; 02:31:59, after the admin flew off while the camera was reparking (parked cameras more than 1500 units away were teleported back every 6 s).
- **Assumption acted on:** server-side observer changes (`SetObserverMode`, `SetObserverTarget`, observer teleport, view angle, client `spec_mode` / `spec_player`) while the client is moving its own camera can crash the client. `StreamCam` now pauses on manual control and waits 5 s before following a fresh hero.
- **To confirm:** no crash after flying off in game; which console commands the client sends when the admin takes the camera (`Admin client command` in `spectate-*.log`).
- **Link / path:** `Bublock/RiftRoulette/Lobby/StreamCam.cs`, `Bublock/Modules/Spectate/SpectateRule.cs`

### 2026-09-27 — Finding modifier names (OnAddModifier)

- **Why hard / useful:** Ability modifiers (Vyper's Petrify for the banned-player statue) are in no data we have: not the API assets, the schema snapshot or the cvar list. Guessing names is not allowed.
- **Verified fact (decompiled API):** `PluginBase.OnAddModifier(AddModifierEvent args)` returns `HookResult`. `args.ModifierVData` is `CCitadelModifierVData` (base `CModifierVData`: `Name`, `Duration`, `IsHidden`); `args.ModifierProperty.Owner` is the entity getting it; also `Caster`, `Ability`, `AbilityHandle`, `Team`, `KeyValues`. `pawn.AddModifier(name, KeyValues3? kv, caster, ability, team)` adds one by name (`kv.SetFloat("duration", s)`). `CModifierProperty.HasModifier(name)` checks it.
- **Used as:** DevTools `ModifierProbe` logs each name once per load to `modifiers-*.log`; cast the ability in game, then read the name. Deadlock API asset ids: Petrify is `ability_viper_ult`, Rabbit Hex `ability_magician_animalcurse` (`api.deadlock-api.com/v1/assets/items`).
- **Link / path:** `Bublock/DevTools/ModifierProbe.cs`, `Bublock/RiftRoulette/Lobby/BanStatueService.cs`

### 2026-09-27 — Modifier names from the datamined ability data

- **Why hard / useful:** Ability modifiers are embedded in the ability entry, not listed in `scripts/modifiers.vdata`, so no local data has them. The public game-file mirror does.
- **Verified fact (datamined, 2026-09-27):** SteamTracking `GameTracking-Deadlock`, `game/citadel/pak01_dir/scripts/abilities.vdata` (raw on `raw.githubusercontent.com`, about 7 MB). Under `ability_viper_petrifybola`, `m_PetrifyModifier` is `_class = "modifier_citadel_petrify"`, `_my_subclass_name = "petrifybola_petrify"` (schema class `CCitadel_Modifier_Petrify`, a `CCitadel_Modifier_Stunned`). The stone particles (`viper_petrify_debuff.vpcf`) live on that embedded entry. `modifier_citadel_silenced` is also absent from `modifiers.vdata` yet `AddModifier` accepts it, so class names work.
- **Verified in game (2026-09-27):** `/status_add <slot> modifier_citadel_petrify 10` works (petrified), but with no Vyper in the match the player rendered as a red wireframe.
- **Precache API (decompiled):** `IDeadworksPlugin.OnPrecacheResources()` runs at map load (not on hot reload); inside it `Precache.AddHero(Heroes hero | string name)` and `Precache.AddResource(string path)` (e.g. a `.vpcf`). `LobbyPlugin` precaches `Heroes.Viper` for the statue look.
- **To confirm:** the stone look after a map load with Vyper precached.
- **Link / path:** [GameTracking-Deadlock](https://github.com/SteamTracking/GameTracking-Deadlock), [schema](https://s2v.app/SchemaExplorer/deadlock/server/CCitadel_Modifier_Petrify)

### 2026-09-27 — Reading player chat (OnChatMessage)

- **Why hard / useful:** Lets players act by typing a plain word (betting: `sapphire` / `amber`) instead of a slash command. Not in the workspace docs.
- **Verified fact (decompiled API):** `PluginBase.OnChatMessage(ChatMessage message)` returns `HookResult`. `ChatMessage` has `SenderSlot` (int), `ChatText` (string), `AllChat` (bool), `LaneColor`, and a `Controller` property that looks up the `CCitadelPlayerController` from the slot (null if gone). It mirrors the client message `CCitadelClientMsg_ChatMsg` (`ChatText`, `AllChat`, `LaneColor`). Returning `Continue` keeps the line in chat.
- **Used as:** reply on the next tick (outside the hook), finding the sender again by Steam ID.
- **To confirm in game:** that the hook fires for normal chat and whether slash commands (`/bet amber`) also reach it (harmless: the betting check needs the whole message to be a team name).
- **Link / path:** `Bublock/RiftRoulette/Betting/BettingPlugin.cs`

### 2026-09-27 — Moving a spectator's camera needs fly cam first

- **Why hard / useful:** The server-side observer mode looks right (`Roaming`) while the client camera stays in the directed view (it sat on the Patron, `npc_boss_tier3`), so a park silently does nothing.
- **Verified fact (in game):** `SetObserverMode(Roaming)` on the server does not change the client's camera mode. A teleport of the observer pawn only moved the camera once the client was in fly cam (C in game). An angle sent with `MovementService.SetViewAngle` in the same tick as the teleport was ignored.
- **Verified fact (enum / cvarlist):** fly cam is `spec_mode 4` (`OBS_MODE_ROAMING` in the Source 2 `ObserverMode_t` enum; CS2's `spec_mode 4` is free cam). `Server.ClientCommand(slot, "spec_mode 4")` does not work: the client refuses it (see the corrected fact under the stream camera entry). The admin has to press C for fly cam. `spec_goto` is not (and ignores pitch and yaw); `citadel_spectator_mode` is devonly.
- **Sequence used:** teleport with no angles 0.25 s after the call (the `spec_mode 4` step was removed on 2026-09-28), then the angle at 0.5 s and 1.0 s (like a hero pawn: teleport first, set the angle after). Re-check the park every 6 s (roaming, no target, near the spot) and redo it if it did not hold.
- **To confirm in game:** that pitch 89 holds with the delayed angle. Fallback: write the observer's view angle field with `SchemaAccessor<T>` (field name to verify first).
- **Link / path:** `Bublock/Modules/Spectate/SpectateService.cs`, `Bublock/RiftRoulette/Lobby/StreamCam.cs`

### 2026-09-27 — Level, boons and ability points per soul count

- **Why hard / useful:** A loadout has to give the level and ability ranks a real hero has at the same net worth. The managed API has no level table, no boon field, and no "spend a point on this ability" call; the wiki page renders the numbers as images.
- **Verified fact:** The raw table is [Data:SoulUnlockData.json](https://deadlock.wiki/index.php?title=Data:SoulUnlockData.json&action=raw) (read by `Module:SoulUnlock`). Its `RequiredSouls` are souls above the 600 starting gold; the displayed threshold is that + 600. 36 rows: 600, 800, 1,100, 1,500, 2,000, 2,600, 3,200, 3,800, then 4,500 ... 49,200. Every row after the first is a boon (`PowerIncrease`, 35 total). Rows 600, 1,100, 2,000 and 3,800 each give an ability unlock (the ultimate only with the fourth); every other row gives one ability point (32 total). Tiers cost 1, 2 and 5 points (8 per ability, so 32 maxes all four). Examples: 18,000 souls is level 24 with 20 points; 20,000 is level 25 with 21 points. Past the table, `citadel_player_gold_per_level_postmax` (2,000) adds HUD "levels" with no boons.
- **Verified fact (decompiled API):** `pawn.Level` writes `m_nLevel` only (no point grant). `CCitadelBaseAbility.UpgradeBits` set calls native `SetUpgradeBits` and deducts no currency. `ECurrencyType.EAbilityPoints` (1) and `EAbilityUnlocks` (2) are separate wallets; the game's own grants arrive as `ELevelUp`, `EStartingAmount` (`ResetHero`) and spends as `EAbilityPurchase`, all through `OnModifyCurrency`. Events: `PlayerAbilityUpgradedEvent`, `AbilityLevelChangedEvent` (`abilitylevel`).
- **Used as:** `Modules/Loadout/Progression` (table), `LoadoutPlanner.AbilityPrefix` (build order walked against the unlock and point budgets), `LoadoutService.Apply` (level from the soul cap since 2026-09-28, prefix bits, both wallets 0), `GameLoop/SoulRule` (ability gains blocked in Random and 1v1 matches).
- **To confirm in game:** partial masks (`0b11`, `0b111`) show the right tiers; the boon stats follow `Level` after the `ECheats` recalc; the `ability_blocked_*` self-test counters show which sources the game sends.
- **Link / path:** `Bublock/Modules/Loadout/Progression.cs`, `Bublock/Modules/Loadout/LoadoutPlanner.cs`, `Bublock/Modules/Loadout/LoadoutService.cs`

### 2026-09-27 — Turning pausing off (convars, commands, net messages)

- **Why hard / useful:** A player kept pausing the lobby. The pause convars are all `devonly` (hidden from `find`), and the pause button may send a net message instead of a console command, so blocking one path alone may not be enough.
- **Verified fact (cvarlist):** `citadel_allow_pausing` (`devonly, sv, cl, rep`, "Determines if pausing is enabled"), `citadel_allow_pause_in_match` ("Allow players to pause in matchmade games"), `citadel_pause_allow_in_pregame` (default false). Do not set `citadel_pause_count` or `citadel_num_team_pauses_allowed` to 0: 0 means unlimited. Commands: `pause` ("Toggle the server pause state"), `setpause`, `citadel_pause` (client "Send a game pause request"), `citadel_toggle_server_pause` (devonly). There is no `unpause` command in Deadlock's list.
- **Verified fact (decompiled API):** `NetMessages.HookIncoming<T>(Func<IncomingMessageContext<T>, HookResult>)` returns an `IHandle` (`Cancel()`); the context has `Message`, `SenderSlot`, `MessageId`, and `HookResult.Stop` drops the message. It throws `InvalidOperationException` when `T` has no registered ID. Pause messages: `CCLCMsg_RequestPause` (`clc_RequestPause` = 33, `PauseType` `RequestPause_t` pause / unpause / toggle, `PauseGroup`) and `CCitadelClientMsg_Pause` (`CITADEL_CM_Pause` = 1008). Game event `citadel_pause_event` (`CitadelPauseEventEvent`: `userid`, `value`, `message`). `GameRules.GamePaused` reads `CGameRules.m_bGamePaused`, `GameRules.ServerPaused` reads `CCitadelGameRules.m_bServerPaused`; `GameRules.GameClock` subtracts paused ticks. `DeadworksPluginBase.OnUnload()` exists for cancelling hooks.
- **Used as:** `Lobby/PauseGuard` (convars 0, `OnClientConCommand` and both message hooks return `Stop`, server `pause` toggle when `GamePaused` anyway), `/pause_allow`.
- **To confirm in game:** which path the pause button takes (`Source=` in `pause-*.log`), and that the server `pause` toggle unpauses (`Game paused while pausing is off` Warnings should not repeat).
- **Link / path:** `Bublock/RiftRoulette/Lobby/PauseGuard.cs`, `Bublock/RiftRoulette/Lobby/PauseRule.cs`, `Bublock/RiftRoulette/Lobby/LobbyPlugin.cs`

### 2026-09-28 — Item slots, flex slots and build sell priority

- **Why hard / useful:** Guides still describe 4 / 4 / 4 category slots plus 4 flex. Loadouts past 9 items fail in `AddItem` while flex slots are locked, and there is no managed API to unlock them.
- **Verified fact (in game, admin):** slots are universal (any item in any slot). 9 are open at match start (one flex slot is already open); with every flex slot open a hero holds 12. deadlock.io agrees on universal slots since the 2025-11-21 update, each enemy Walker unlocking the next slot.
- **Verified fact (cvarlist):** `citadel_hero_demo_unlock_flex_slots` (`sv, cl, a, rep, release`, default false, "Do we start flex slots unlocked") is not a cheat. `citadel_unlock_flex_slots` / `citadel_lock_flex_slots <team>` are `sv, cheat`. `citadel_test_flex_unlocked_message [1-4]` (client, devonly) shows the unlock message. The API only has `CCitadelUserMsg_FlexSlotUnlocked` (message 356), no unlock call.
- **Verified fact (Deadlock API builds):** each build item (`BuildModEntry`) has `sell_priority` and `required_flex_slots`. In 38 top builds (1,643 items) only 18 had a non-zero sell priority and none set flex slots. The one build numbering every item (100 down to 94) put 100 on its first, cheapest item, so a higher number sells first.
- **Verified fact (schema, [GameTracking-Deadlock](https://github.com/SteamTracking/GameTracking-Deadlock/blob/master/DumpSource2/schemas/server/CCitadelTeam.h)):** each team entity has networked `CCitadelTeam.m_nFlexSlotsUnlocked`, type `EFlexSlotTypes_t` (`uint16` bitmask: `EFlexSlot_Slot01` = 1, `Slot02` = 2, `Slot03` = 4, `Slot04` = 8; all four = 15). `SchemaAccessor<T>.Set` notifies the network when the field is networked. The team entity's designer name is not in the map export (created at runtime); find it with `/ent_find team` before writing the field (a wrong entity means a write at a wrong offset).
- **Healing Rite** is `upgrade_health_stimpak` (800, vitality).
- **Used as:** `LobbyService.ApplyServerConvars` (convar 1), `Lobby/FlexSlots` (team field 15), `LoadoutPlanner.Plan` (12 slots, sell order), `scripts/fetch-builds.py` (`sellPriority`).
- **In game (2026-09-28, corrected):** with `citadel_hero_demo_unlock_flex_slots 1` set at load, loadouts at a 100,000 cap logged `Items=12 Failed=0`: the slots were usable on the server, the client's inventory just did not draw them as open. The team field (networked) is expected to fix the display too; confirm after the upload.
- **In game (2026-09-28, first report):** the convar looked like it did nothing, by hand or set at load.
- **Verified (2026-09-28, debug session):** both teams logged `Before=0 After=15` (the convar never sets the field). Instrumented snapshot: team field networked (`SchemaAccessor` `_networked=True`, chain 0, offset 1432 = schema 0x598), `CCitadelGameRules.m_bFlexSlotsForcedUnlocked` = 0 (networked, offset 307) the whole time, `GameMode=Invalid`. The HUD showed the flex slots on heroes built after the write (every loadout `ResetHero`s), with the gamerules flag still 0: so the display reads the team field and refreshes on a hero rebuild; the live hero from before the write kept the old layout. Held items report `AbilitySlot` `None` (passives) or `ActiveItemN` (actives); there is no per-item slot index to fix. Deadworks `SchemaAccessor.Set` calls `NotifyStateChanged(entity, offset, chainOffset, networkStateChangedOffset)` when the schema says the field is networked, and `GameRules` setters use the same accessor on `GameRules.Pointer`. So `Lobby/FlexSlots` writes `m_nFlexSlotsUnlocked = 15` on both team entities (startup, reload, every join, every intermission; `/lobby_flex` to check).
- **Team entity name (inferred, to confirm with `/lobby_flex`):** `citadel_team_manager`. It is in the server's string table ([server_strings.txt](https://github.com/SteamTracking/GameTracking-Deadlock/blob/master/game/citadel/bin/win64/server_strings.txt)) next to the base `team_manager`, the same pattern as CS2's `cs_team_manager`. `FlexSlots` writes only entities with exactly that designer name on team 2 or 3, and skips the write when `SchemaAccessor.GetAddress(entity) == entity` (field offset 0, not in the schema).
- **Link / path:** `Bublock/RiftRoulette/Lobby/FlexSlots.cs`, `Bublock/RiftRoulette/Lobby/LobbyService.cs`, `Bublock/Modules/Loadout/LoadoutPlanner.cs`, `Bublock/scripts/fetch-builds.py`

### 2026-09-28 — When Walkers fall, and items held by net worth (real matches)

- **Why hard / useful:** Loadouts at a mid cap filled all 12 slots, far more items than a real hero holds at that net worth. The slot unlocks depend on team objectives, not souls, so they had to be mapped to net worth from match data.
- **Verified fact (guides, 2026-09):** slots are universal; 9 at the start, and each enemy Walker (lane `Tier2` objective) the team destroys opens one more, to 12. Guardians and shrines no longer unlock slots ([deadlock.io flex slots](https://deadlock.io/en/mechanics/flex-slots), [Walker wiki](https://deadlock.wiki/Walker)).
- **Verified fact (API):** `GET https://api.deadlock-api.com/v1/matches/metadata?include_player_items=true&include_player_stats=true&include_objectives=true&game_mode=normal&match_mode=ranked&limit=500` returns up to 500 matches in one call. `objectives[]` has `team_objective` (`Tier1LaneN` Guardian, `Tier2LaneN` Walker), `team` (the owner) and `destroyed_time_s` (0 = standing). Each player has `stats[]` snapshots every 180 s with `net_worth`, and `items[]` with `game_time_s`, `sold_time_s` (0 = kept) and `item_id`; ability unlocks are in `items[]` too, so filter by `/v1/assets/items` `type == "upgrade"`. Enum query values are lowercase (`Normal` returns HTTP 400).
- **Data (500 ranked matches, median badge 61):** player net worth when the team's 1st / 2nd / 3rd enemy Walker fell, medians 15.7k / 22.0k / 28.5k (quartiles 12.7-19.2k, 18.2-26.9k, 23.9-33.9k). Items held never exceeded the slot count. Median items held: 7 at 14k, 9 at 20k, 10 at 30k; at 14k, 56% of players had no Walker down.
- **Used as:** `Modules/Loadout/ItemSlots` (9 items, 10 from 16,000, 11 from 22,000, 12 from 28,000), recomputed with `scripts/walker-souls.py`.
- **Link / path:** `Bublock/Modules/Loadout/ItemSlots.cs`, `Bublock/scripts/walker-souls.py`

### 2026-09-28 — A hot reload mid-match ends the match (souls flow again)

- **Why hard / useful:** Souls looked unblocked after a `/match_start`. The soul rule was fine; the match was gone.
- **Verified (logs, session `12681a2f` then `81c2ef2d`):** an upload at 04:14:34 hot-reloaded `RiftRoulette.dll` during round 2. The new load starts with `MatchService.State` empty (`Buying anywhere ... MatchRunning=False`), no rounds were scheduled for 6 minutes, and `SoulRule.ShouldBlock` returns false with no match running, so every soul gain passed. Auto-start did not restart it with one human connected. A fresh map load plus `/match_start` at 04:20 blocked souls again.
- **Rule of thumb:** after an upload during a match, run `/match_start` again (or let auto-start do it with 2+ players).
- **Link / path:** `Bublock/RiftRoulette/GameLoop/SoulRule.cs`, `Bublock/RiftRoulette/GameLoop/MatchService.cs`

### 2026-09-28 — Reading the spectator camera's view angle

- **Why hard / useful:** The stream camera saves the admin's framing (position and angle). The observer pawn is not a hero pawn, so the usual angle property is missing.
- **Verified fact (build):** `EyeAngles` / `ViewAngles` / `CameraAngles` are on `CCitadelPlayerPawn` only; `CBasePlayerPawn` (the type of `controller.Pawn`, and the `observer` pawn) has none of them (CS1061).
- **Verified fact (schema, [CBasePlayerPawn.h](https://github.com/SteamTracking/GameTracking-Deadlock/blob/master/DumpSource2/schemas/server/CBasePlayerPawn.h)):** `CBasePlayerPawn` has `QAngle v_angle` and `v_anglePrevious` (not networked). Read with `SchemaAccessor<Vector3>("CBasePlayerPawn"u8, "v_angle"u8)`; `GetAddress(handle) == handle` means the field is missing.
- **Verified fact (logs, 2026-09-28):** pressing C (fly cam) sends no console command to the server; only `spec_next` clicks arrive in `OnClientConCommand`. The observer's `ObserverMode` follows the client: `Roaming` with no target means fly cam, and a server-side `SetObserverMode(Roaming)` still reads `InEye` in the directed view.
- **To confirm in game:** that `v_angle` tracks the fly cam view (`dw_spec_status` `ViewAngle=` while turning), and whether a server follow (`InEye` + target) pulls the client out of fly cam (`lobby-*.log` `Stream camera follow did not take ... FlyCam=True`).
- **Link / path:** `Bublock/Modules/Spectate/SpectateService.cs` (`ViewAngle`, `Pose`), `Bublock/RiftRoulette/Lobby/StreamCam.cs`

### 2026-09-28 — "Couldn't resolve offset" spam after a spectator drops

- **Why hard / useful:** The console floods on an empty server and the engine lines name only an offset and a path, not a field or an owner.
- **Seen (server console, 01:39):** a seated admin idle in spectate timed out (`NETWORK_DISCONNECT_TIMEDOUT`, Steam reason 5003 `problem detected locally`). In the same second and every few seconds after, with nobody connected: `Couldn't resolve offset 488 in CCitadelPlayerPawn at path (4 = '26')`, the same for 496, then `SV: 2762/CCitadelPlayerPawn: requested resolve all N changes, actually resolved only M changes` and `488 path(4 = '26/') not resolved`. Every line is one entity (2762).
- **Reading:** the server has two queued network changes on that pawn at byte offsets with no field in the `CCitadelPlayerPawn` send table (it excludes `m_flPoseParameter` / `m_flEncodedController`, [schema](https://github.com/SteamTracking/GameTracking-Deadlock/blob/master/DumpSource2/schemas/server/CCitadelPlayerPawn.h)). Unresolved changes are never cleared, so the warning repeats each snapshot while the entity exists. We write neither offset. The pawn outlived its client: `RemovePlayer` removed only `GetHeroPawn()` and the controller, and the observer class (`CCitadelObserverPawn`) is a separate entity from the hero.
- **Used as:** `LobbyService.RemovePlayer` also removes `controller.Pawn` when it differs from the hero, logs both designer names and entity indexes (`Disconnect pawns removed`), and `SweepOrphanObservers` removes unowned `observer` pawns 1 s later (also on a disconnect with no controller).
- **To confirm in game:** disconnect from the admin seat on an empty server; the spam should stop. If it continues and the log shows both pawns removed, match the logged indexes against the entity number in the spam.
- **Link / path:** `Bublock/RiftRoulette/Lobby/LobbyService.cs`, `Bublock/RiftRoulette/Lobby/LobbyPlugin.cs`

### 2026-09-28 — A changed API member type silently kills the whole hook

- **Why hard / useful:** Nothing reached our logs. Players who left stayed in game as "disconnected" with their pawns standing, and even a `kickid` left no disconnect line.
- **Verified fact:** the server's Deadworks was updated to v0.4.18 (`495553c`) at the 2026-09-28 06:01 UTC restart (its `managed/` files are dated then). Deadworks [`e0d5990`](https://github.com/Deadworks-net/deadworks/commit/e0d59905ccf6daeec836cf17edd12c63babe2ee4) (2026-09-24) changed `ClientDisconnectedEvent.Reason` from `int` to `ENetworkDisconnectionReason` and added `OnClientDisconnecting` (runs before the game's own cleanup, while the controller and hero still exist). [`cd897a4`](https://github.com/Deadworks-net/deadworks/commit/cd897a4e615ade79d4ef78f8c7c24a00e047175c) (2026-09-27) added `IsMapChange` (reason `NetworkDisconnectShutdown`): every player gets that disconnect as the map shuts down and reconnects to the next map, so it is not a leave. Our DLL, built against the Sep 24 `lib/`, read `args.Reason` as `int` in `OnClientDisconnect`. .NET compiles a method on its first call, so every call threw before its first line (not even the `EventCounters.Hit`). The error only goes to the host console. Building against the server's DLL gave `CS1503: cannot convert from 'ENetworkDisconnectionReason' to 'int'` on that line, and no other error.
- **Used as:** `lib/` now holds the server's `DeadworksManaged.Api.dll`. `OnClientDisconnect` skips `IsMapChange` and passes the enum on. The `.rules` require building against the server's DLL; `patch-day.md` has the symptom row.
- **Link / path:** `Bublock/RiftRoulette/Lobby/LobbyPlugin.cs`, [managed/EntryPoint.cs](https://github.com/Deadworks-net/deadworks/blob/main/managed/EntryPoint.cs) (`OnClientDisconnect`)

### 2026-09-28 — Invisibility and Abrams names for the roaming admin

- **Why hard / useful:** Veil Walker's own modifier has never shown up in the server's modifier log (nobody has triggered it), and the `Heroes` enum uses internal hero names.
- **Verified fact:** the DevTools modifier log shows `upgrade_cloaking_device_active/modifier_invis` and `ability_smoke_bomb/smokebomb_modifier_invis`. The last path segment is the modifier name that `AddModifier` takes, as with `modifier_citadel_silenced` and `modifier_citadel_petrify`. `modifier_invis` is the generic invisibility. Abrams is `Heroes.Atlas` (6; `hero_atlas`). Adding `modifier_invis` to a hero by name has not been checked in game yet: `dw_selftest_live` has a Roam row for it.
- **Used as:** `Lobby/AdminSeat` `RoamModifier` / `RoamHero`: the roaming admin's cloak (3600 s `duration`, put back when it runs out).
- **Link / path:** `Bublock/logs/DevTools/modifiers-*.log`, `Bublock/RiftRoulette/Lobby/AdminSeat.cs`

### 2026-09-28 — Joins stop completing after hours up; map reload API

- **Why hard / useful:** Nothing in our logs showed it: a connection that is let in writes nothing until it leaves, and the failed joiners left with no pawn.
- **Seen (lobby logs, 9-27 and 9-28):** after 1.5 to 3.5 hours up (60 to 115 rounds), no player finished joining again (no `Player admitted` line) while players already in kept playing, 5 at a time, far from the cap. 9-27 13:53 to 22:39 UTC: 0 joins, about 33 failed attempts; 9-28 from 11:47 UTC: 16 failed. Leavers showed `Hero=none Current=none`, and unowned observer pawns piled up (`Orphan observer pawn removed`). Plugin hot reloads did not fix it; only process restarts did. Cause unknown.
- **Verified fact (decompiled `lib/`, v0.4.18):** `Server.ChangeLevel(string map)` runs `changelevel <map>` and sets `Server.IsChangingLevel`; if the change does not start within 5 s it gives up (console `[Server] changelevel didn't start within 5000 ms`). `Server.MapName` is the current map. On a map start `Players` remembers every connected Steam ID, and a reconnect of one of them sets `IsMapChangeReconnect` on `ClientConnectEvent`, `ClientPutInServerEvent` and `ClientFullConnectEvent`: clients reconnect to the new map by themselves and our join hooks run again. `GlobalVars.CurTime` is the engine clock (per map in Source; not checked here).
- **Used as:** `Lobby/AutoRestartService` watches connect to full connect, counts stuck joins (`restart-*.log`), and reloads the map with nobody playing. `AdminSeat.ResetForMap` / the reconnect `Forget` keep the admin seat from surviving the map (plugin statics do, pawns do not).
- **Verified (`/restart_now`, 17:07 UTC):** `OnStartupServer` runs on `changelevel` in every DLL (0.7 s after the call; no new log session, statics kept). The admin reconnected with `MapChangeReconnect=True` 2 s later, full-connected 2.3 s after that, was seated and roamed 2 s later. CleanSlate's single 2 s pass ran before the lane guardians spawned (`npc_trooper_boss` missing from its summary; a cold start removes 6) and left them standing, so it now sweeps at 2, 10 and 30 s. `FlexSlots` finds no `citadel_team_manager` at map start (same on a cold start); the next `AdmitPlayer` opens them.
- **To confirm in game:** whether a map reload (not only a process restart) lets joins complete again. If not, the next step is a process restart (`quit`), which needs the host to start the process again.
- **Cause found (engine console, 9-28):** the server writes the engine console to `/server/game/citadel/console.log` (SFTP; about 60 MB a day). Every stuck join logged, right at `SIGNONSTATE_CONNECTED -> SIGNONSTATE_NEW`: `Message size 632031 is too big.  Max is 524288` then `<name> SendMessages reliable failed with EResult 8`, and the client never answered again. The sign-on message (the string tables sent to a joining client) grew with play on one map: 571,918 bytes at 19:23 UTC (round 65, 77 min after the 18:06 map start) to 721,932 at 19:44 (round 91), about 6 KB a round. 524,288 is Steam networking's fixed `k_cbMaxSteamNetworkingSocketsMessageSizeSend`; it cannot be raised. Every map start logs `CNetworkStringTableContainer::RemoveAllTables: removing 14 tables`, so a map reload does reset it. Same failure in long Dota 2 custom games that change heroes: [GameNetworkingSockets #394](https://github.com/ValveSoftware/gamenetworkingsockets/issues/394). Which table grows is not yet known (engine `dumpstringtable all sv simple`, `modifier_stringtable_dump` needs cheats).
- **Growth per fighter-round (9-28 estimate):** counting one per fighter per round since the map start, the first failure came at 284 fighter-rounds (572 KB) and the last at 414 (722 KB): about 1.15 KB per fighter-round, so 512 KB was crossed near 230. Hero swaps drive it (`AnimAssetData` held about 85% of the string table bytes).
- **Used as (join budget):** `Lobby/MapRefreshService` counts fighter-rounds in Random mode and, at 160 (`MapRefreshRule.DefaultBudget`), ends the match at a round end with a chat warning and reloads the map; auto-start begins a fresh match when players are back. `/restart_budget` shows or changes the budget.
- **Link / path:** `Bublock/RiftRoulette/Lobby/AutoRestartService.cs`, `Bublock/RiftRoulette/Lobby/MapRefreshService.cs`, `Bublock/logs/RiftRoulette/lobby-2026092*.log`, `Bublock/logs/engine/console.log` (pulled by hand)

### 2026-09-30 — Deadworks v0.5.0 / engine 6712

- **Why hard / useful:** City Never Sleeps moved the GC to 6712 while the server stayed on 6701; joins failed until Deadworks **v0.5.0** (published 2026-09-30 01:05 UTC) was on the host. Bublock must rebuild against the new `DeadworksManaged.Api.dll` or `EModifierState` numbers and renamed types silently go wrong.
- **Verified fact (decompiled `lib/`, SHA `1ce2ab6f…`, matches server managed):** `MaskTrace` renamed to `InteractionLayer` (`Trace.Ray` / `SimpleTrace`). `EModifierState` renumbered (e.g. `Silenced` 15→17, `NoIncomingDamage` 142→147; `Count` 302→336). `CBasePlayerController.PlayerSteamId` is now get/set (was get-only expression). `ClientDisconnectedEvent.Reason` remains `ENetworkDisconnectionReason`. `ModifyCurrency(..., silent, forceGain, spendOnly)` still takes bools in C#; the release note "flags as 32-bit" is the native side. `Deadworks.Version` = `"v0.5.0"`.
- **Live (console / lobby):** after the update, server reports version **6722**; GC later wants **6723** while Deadworks keeps `Forcing true`. `maxplayers` and `citadel_hero_demo_unlock_flex_slots` are missing (`ServerConVars.TrySet` Warning); flex unlock stays `Lobby/FlexSlots`. `Heroes.Skyrunner` is not selectable (`playable=False`); lobby placeholder is now `LobbyService.LobbyHero` = Atlas. World text can read solid black after 6712 — `WorldTextService.Create` now sets `Fullbright` and `SetColor` after spawn.
- **Friend crash (кlobbin, 18:18 / 18:19 UTC):** not a ban. Server sent a full update (`can't find client frame for world snapshot`), then SteamNet keepalive timeouts → `NETWORK_DISCONNECT_TIMEDOUT`. Rejoin completed, loadout struggled (`Hero did not change Current=0` for Hornet), then timed out again (`STEAMAUTH` failure code 6). Client-side freeze/crash under a big snapshot, not plugin kick.
- **Used as:** `SelfTestService` floor mask → `InteractionLayer`; rebuild plugins against new `lib/`; `RiftService.NoteTroopers` for Rem assist creeps; self-test `IfMissing=Warn` for the two missing convars; `WorldTextService` post-create color/fullbright stamp; mid rift `MiddlePosition` raised to z 448 and put in rotation (`/rift_mid`).
- **Link / path:** GitHub [Deadworks-net/deadworks v0.5.0](https://github.com/Deadworks-net/deadworks/releases/tag/v0.5.0); `deadworks/lib/DeadworksManaged.Api.dll`; `Bublock/RiftRoulette/SelfTest/SelfTestService.cs`; `Bublock/Modules/WorldText/WorldTextService.cs`

### 2026-09-30 — Mid rift enabled (approximate)

- **Why hard / useful:** Center spawn lived at `(0,0,0)` and was excluded from rotation; watch/fight/boards only knew Green/Yellow.
- **Verified fact (code):** `RiftSide.Center`, `MiddlePosition = (0,0,448)` (side-rift height under mid floor; raised to 576 briefly then reverted — capture under mid was uneven), `NextInRotation` with `MiddleEnabled` (default on, `/rift_mid`), `WatchCenter` + N/S `CenterSapphire` / `CenterAmber` toward team bases.
- **Used as:** `/rift_next center`, `/rift_mid on|off`, `/spots_walk … center`.
- **Link / path:** `Bublock/RiftRoulette/Rift/RiftSide.cs`, `Locations/RiftRouletteLocations.cs`
