---
id: talking-to-the-game
type: mental-model
tags: [game-interaction, levers, no-native-api]
related:
  - how-deadworks-mods-work.md
  - ../effects-catalog.md
  - ../game-mode-recipes.md
  - ../generated/deadworks-api.md
---

# Talking to the game

Deadlock has no official modding API. Deadworks gives us a thin, typed layer
over the engine, and everything a game mode does comes down to eight
channels. Each is a way to **observe** the game, **change** it, or
**block** something before it happens.

| # | Channel | Observe | Change | Block | Our main use |
|---|---|---|---|---|---|
| 1 | **Hooks** (plugin overrides) | `OnGameFrame`, `OnEntitySpawned`, `OnModifierEvent` | — | `OnTakeDamage`, `OnModifyCurrency`, `OnChatMessage`, `OnClientConCommand`, `OnAddModifier` return `Stop`; `OnClientConnect` returns `false`; `OnAbilityAttempt` sets `BlockedButtons` | damage immunity up top, soul block, chat bets, admin seat |
| 2 | **Game events** (`[GameEventHandler]`) | `player_death`, `player_spawn`, `player_respawned`, `player_hero_changed`, `player_used_ability` (145 exist) | — | — | stats, respawn handling, stream camera |
| 3 | **Entities** | `Entities.ByDesignerName`, `Entities.All`, `IsValid` | `Teleport`, `Remove`, `AcceptInput("Disable")`, create (`CPointWorldText.Create`) | — | rifts, troopers, boards, map cleanup |
| 4 | **Players** (controller + pawn) | slot, Steam ID, team, hero, health | `SelectHero`, `ChangeTeam`, `MakeObserver`, `Hurt`, heal, `AddItem`, `ImbueItem`, `ResetHero`, currencies | — | random heroes, loadouts, teams, spectating |
| 5 | **Modifiers and states** | `HasModifierState` | `AddModifier("modifier_...")`, set `EModifierState` flags (304 exist) | — | restraint |
| 6 | **Convars and console** | `ConVar.Find(...)` | `ServerConVars.TrySet`, `Server.ExecuteCommand` (`kickid`), client commands (`spec_mode 4`) | — | team size, lanes off, KOTH on/off, shop access |
| 7 | **Schema fields** (raw engine memory by name) | `CCitadelGameRules.m_timeKothGiveUp` | `m_vNextKothLocation`, `m_timeNextKothSpawn` | — | forcing a rift where and when we want it |
| 8 | **Net messages** (protobuf to clients) | incoming `[NetMessageHandler]` | `HudAnnounce` banner, chat line, `SetClientCameraAngles` | outgoing handlers | banners, chat, camera |

Plus two presentation tools built on the channels above: **world text**
(`point_worldtext` boards) and **timers** (`ITimer.Once`, `Every`,
`Sequence`, `NextTick`).

## Rules of thumb

1. **Prefer blocking to undoing.** Returning `Stop` from `OnTakeDamage` beats
   healing after the hit. Blocking soul gain in `OnModifyCurrency` beats
   draining souls later.
2. **Re-assert state every frame when the game fights you.** Modifier states
   get cleared by the game, so Restraint sets them in `OnGameFrame`.
3. **Never act inside a connect callback.** Hero swaps in
   `OnClientFullConnect` are lost, and starting a match on disconnect
   crashed the server. Defer with `Timer.NextTick` or `Once` (`.rules`
   §0.2, Stage 13d).
4. **The client is not the server.** Server-side observer mode does not move
   the client camera; we send the client command `spec_mode 4` too
   (`Modules/Spectate`).
5. **Every new dependency gets a self-test.** A new convar, entity name,
   schema field, modifier or event goes into `SelfTest/GameDependencies`
   and `patch-day.md` §5 in the same change. Patches rename things.
6. **Don't invent names.** Check [generated/deadworks-api.md](../generated/deadworks-api.md),
   `cvarlist.md`, the schema DB or the entity dump first.

See [effects-catalog.md](../effects-catalog.md) for each effect with its status.
