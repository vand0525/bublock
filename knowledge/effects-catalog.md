---
id: effects-catalog
type: catalog
tags: [effects, levers, game-modes, verified, untested]
related:
  - mental-models/talking-to-the-game.md
  - game-mode-recipes.md
  - generated/deadworks-api.md
  - generated/indexes.md
  - glossary.md
---

# Effects catalog

Every lever a game mode can pull, grouped by what you want to do to the
game. Status:

- **verified**: running in Bublock on a live server; the "Where" file shows how.
- **untested**: exists in Deadworks v0.4.18 (see [generated/deadworks-api.md](generated/deadworks-api.md)); nobody here has tried it.
- **avoid**: tried, and it broke something.

Promote an entry from untested to verified in the same change that ships it,
and add the dependency to `SelfTest/GameDependencies` and `patch-day.md` §5.

## 1. Put players somewhere

| Effect | How | Status | Where |
|---|---|---|---|
| Teleport a player (position + facing) | `pawn.Teleport(...)` | verified | `Modules/Movement/MovementService.cs` |
| Teleport a group without stacking | anchor + per-slot offset from `spots.json` | verified | `Round/SlotSpots.cs` |
| Contain players to an arena | bounds box in `arena.json`, strays sent back each second | built, awaiting playtest | `Modules/Arena/ArenaService.cs` (`ReturnStrays`) |
| Make only one lane active | `citadel_active_lane <n>` (1 west, 4 middle, 6 east) | untested | `GunGame/Data/arena.json` |
| Point the client camera | `CCitadelUserMsg_SetClientCameraAngles` | verified | `MovementService.SetViewAngle` |
| Move a player to a team | `ChangeTeam(team, false)` (2 Amber, 3 Sapphire) | verified | `Lobby/LobbyService.cs` |
| Make a player a spectator | `ChangeTeam(1, false)` then `MakeObserver()` next tick | verified | `Lobby/AdminSeat.cs` |
| Spectator follow / fly cam | `ObserverServices`, client `spec_mode 4`, `spec_player` | verified | `Modules/Spectate/SpectateService.cs` |
| Detect entering / leaving an area | `Utils.Zone.FromCenter(center, size)`, `Entered` / `Left` events | untested | API: `Utils.Zone` |
| Check line of sight, find the floor | `Trace.Ray(start, end, mask, ignore)` | untested | API: `Trace` |
| Pull back, zoom or aim a player's camera | `controller.Camera` → `PlayerCamera.LerpDistance` / `LerpFov` / `LerpTarget` / `Maintain` | untested | API: `PlayerCamera` |
| Spectate with `ChangeTeam` alone | leaves the hero pawn; the client drops 12-23 s later | **avoid** | `Lobby/AdminSeat.md` |
| `IsValidObserverTarget` | rejects team 3 (Sapphire) | **avoid** | `.rules` §0.2 |

## 2. Limit what players can do

| Effect | How | Status | Where |
|---|---|---|---|
| Silence (no abilities) | modifier `modifier_citadel_silenced` + `EModifierState.Silenced` (15) | verified | `Modules/Restraint/RestraintService.cs` |
| No items / no shooting / no melee | `EModifierState.ItemsDisabled` (14), `ShootingDisabled` (62), `MeleeDisabled` (106), re-set every frame | verified | same |
| NPCs ignore a player | `EModifierState.IgnoredByNpcTargeting` (33) | verified | same |
| Block damage to a player | `OnTakeDamage` → `HookResult.Stop` | verified | `GameLoop/GameLoopPlugin.cs` |
| Block chat, or read it as a command | `OnChatMessage` → `Stop` | verified | `Betting/BettingPlugin.cs` |
| Watch console commands (`selecthero`, `changeteam`, `respawn`) | `OnClientConCommand` (can return `Stop`) | verified (observe) / untested (block) | `GameLoopPlugin.OnClientConCommand` |
| Lock a hero | kill on a menu change, respawn with the assigned hero | verified | `Lobby/HeroLock.cs` |
| Refuse a connection | `OnClientConnect` → `false` | verified | `Lobby/AccessService.cs`, `AdminSeat.cs` |
| Block specific abilities or items from being cast | `OnAbilityAttempt` → set `BlockedButtons` | untested | API hook |
| Stop a modifier from landing | `OnAddModifier` → `Stop` | untested | API hook |
| Stun, root, slow, freeze, sleep | `EModifierState.Stunned` (18), `Slowed` (61), `Frozen` (193), `IsAsleep` (75) | untested | API enum |
| Disarm | `EModifierState.Disarmed` (12) also blocks reloading | **avoid** | `.rules` §0.2 |

## 3. Protect or empower players

| Effect | How | Status | Where |
|---|---|---|---|
| Heal to full | set pawn `Health` from max | verified | `Round/RoundFlow.cs` |
| Damage a player (enforcement) | `pawn.Hurt(amount)` | verified | `Lobby/HeroLock.cs` |
| Invulnerable / unkillable / bullet-proof | `EModifierState.Invulnerable` (19), `Unkillable` (36), `BulletInvulnerable` (92), `NoIncomingDamage` (142) | untested (142 is the documented fallback in `patch-day.md`) | API enum |
| Deal no damage | `EModifierState.NoOutgoingDamage` (141) | untested | API enum |
| Invisible to enemies | `EModifierState.InvisibleToEnemy` (31), `InvisibleOnMinimap` (206) | untested | API enum |
| Unstoppable, status immune | `EModifierState.Unstoppable` (25), `StatusImmune` (24) | untested | API enum |
| No healing / no regen | `EModifierState.HealingDisabled` (57), `HealthRegenDisabled` (55) | untested | API enum |

## 4. Heroes, builds and economy

| Effect | How | Status | Where |
|---|---|---|---|
| Change a player's hero | `SelectHero(Heroes.X)`, only while alive | verified | `Lobby/LobbyService.cs`; never while dead (`.rules` §8) |
| Swap a living player's hero mid-fight, then give a build | `SelectHero` then `LoadoutService.Swap` applies 1 s later; route it through `RandomModeService.Reroll` so the hero lock allows it | verified (owner playtest, 2026-09-28) | `Modules/RandomLoadout/RandomLoadouts.cs`, used by `GunGame/` |
| Allow duplicate heroes | convar `citadel_allow_duplicate_heroes 1` | verified | `LobbyService` |
| Give a full build (items, imbues, ability ranks, level) | `ResetHero`, `AddItem`, `ImbueItem`, ability upgrade bits, currencies | verified | `Modules/Loadout/LoadoutService.cs` |
| Copy one player's exact hero state to another | `LoadoutSnapshot` | verified | `Modules/Loadout/LoadoutSnapshot.cs` |
| Real top builds per hero | `hero-builds.json` from deadlock-api.com (offline, embedded) | verified | `scripts/fetch-builds.py` |
| Block soul income | `OnModifyCurrency` → `Stop` by `ECurrencySource` | verified | `GameLoop/SoulRule.cs` |
| Shop anywhere / shops off | `citadel_allow_purchasing_anywhere`; CleanSlate disables shop triggers | verified | `GameLoop/ShopAccess.cs`, `CleanSlate/` |
| Precache a hero or effect before use | `Precache.AddHero`, `AddResource` in `OnPrecacheResources` | untested | API: `Precache` |

## 5. Shape the map and objectives

| Effect | How | Status | Where |
|---|---|---|---|
| Force a rift at a side, now | write `CCitadelGameRules.m_vNextKothLocation`, zero spawn window / time | verified | `Rift/RiftGameRules.cs` |
| Detect capture vs tie | new `npc_trooper` on the winning team, or cash-in gone without troopers | verified | `Rift/RiftService.cs` |
| Turn the rift system on/off | `citadel_koth_enabled` | verified | `LobbyService`, `RiftGameRules` |
| Lanes, troopers, NPCs, crates, mid-boss off | `citadel_trooper_spawn_enabled 0`, `citadel_npc_spawn_enabled 0`, `citadel_active_lane 0`, crate and mid-boss convars | verified | `CleanSlate/CleanSlateService.cs` |
| Remove map entities | `Entities.ByDesignerName(...)` → `Remove()` | verified | `CleanSlateService` |
| Disable a trigger | `entity.AcceptInput("Disable")` | verified | `CleanSlateService` |
| Team size, max players | `citadel_team_size`, `maxplayers`, `sv_visiblemaxplayers` | verified | `LobbyService` |
| Remove `info_super_trooper_spawn` | crashes the server | **avoid** | `.rules` §8 |
| Rift at a non-map location (mid) | same schema write at another position | untested | `reference/endless-mode.md` |
| Read or change game state / clock | `GameRules.GameState`, `ChangeGameState`, `OnGameStateChanging` veto | untested | API: `GameRules`, hooks |
| React to map triggers and I/O | `[EntityOutputHook]`, `[EntityInputHook]`, `OnEntityStartTouch` | untested | API |

## 6. Tell players things

| Effect | How | Status | Where |
|---|---|---|---|
| Banner (title + description) | `controller.HudAnnounce(title, desc)` per player | verified | `Modules/Hud/HudService.cs` |
| Chat line to one or all | `CCitadelUserMsg_ChatMsg` | verified | `Shared/Chat/PlayerChat.cs` |
| Text boards in the world | `CPointWorldText.Create` / `point_worldtext`, updated in place | verified | `Modules/WorldText/WorldTextService.cs` |
| Glowing lines, boxes, paths | `CBeam.Create`, `CreateBox`, `CreatePolyline` | untested | API: `CBeam` |
| Particle effects | `CParticleSystem.Create(name).AtPosition(...).Spawn()` (precache first) | untested | API: `CParticleSystem` |
| Sounds | `Sounds.Play(name, recipients, volume, pitch)`, `PlayAt(...)` | untested | API: `Sounds.Sounds` |
| Custom HUD panels (Panorama) | `UI.Panel(id)` → `BuildLayout`, `Animate`, buttons routed back as `dw_ui`; needs a client content addon | untested | API: `UI.UI`, `ContentAddons` |
| Hide entities from one player | `OnCheckTransmit` → `Hide` | untested | API hook |

## 7. Time and lifecycle

| Effect | How | Status | Where |
|---|---|---|---|
| Run later / repeatedly / as a sequence | `Timer.Once`, `Every`, `Sequence`, `NextTick` | verified | everywhere; `resources.md` |
| React to deaths, spawns, hero changes, ability use | `[GameEventHandler("player_death")]` etc. (145 events) | verified (5 used) | [generated/indexes.md](generated/indexes.md) |
| Per-frame upkeep | `OnGameFrame` | verified | `Modules/Restraint/RestraintPlugin.cs` |
| Survive hot reload | redo startup work in `OnLoad(isReload: true)` | verified | `CleanSlate/CleanSlatePlugin.cs` |
| Start a match inside `OnClientFullConnect` | hero swaps are lost | **avoid** | `.rules` §0.2 |
| Start a match on disconnect | crashed the server | **avoid** | `.rules` §0.2 |
| Remove a game type by deleting its DLL only | the loaded plugin keeps running until a restart | **avoid** (restart after parking) | `resources.md` 2026-09-28 |
