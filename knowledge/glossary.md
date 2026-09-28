---
id: glossary
type: glossary
tags: [terms, start-here]
related:
  - README.md
  - mental-models/rift-roulette-architecture.md
  - effects-catalog.md
---

# Glossary

Terms used in the code, logs, docs and in game. Game terms first, then
Deadworks and engine terms, then Bublock terms. Each entry says where the
term is defined or used.

## Deadlock (the game)

| Term | Meaning | Where |
|---|---|---|
| **Amber / Sapphire** | The two teams. Amber is team number 2, Sapphire is team 3; team 1 is spectators. | `patch-day.md` §5 |
| **Rift (KOTH)** | The game's king-of-the-hill objective. The engine calls it KOTH. Rift Roulette forces one per round. | `RiftRoulette/Rift/FEATURE.md` |
| **Rift spawner** | `citadel_item_koth_spawner`: places the rift, then removes itself about 1 s later. | `Rift/RiftService.cs` |
| **Cash-in** | `citadel_koth_cashin`: the live rift entity. When it exists, the rift is up. | `resources.md` |
| **Capture** | A team takes the rift. The game spawns `npc_trooper` entities on the winning team, which is how we detect the winner. | `Rift/RiftService.md` |
| **Tie / give-up** | The cash-in disappears (about 60 s after spawn) with no troopers: nobody captured. | `endless-mode.md` |
| **Green / Yellow** | The two rift sides on `dl_midtown`, at (7612, 0, 444) and (-7560, 0, 424). | `Rift/RiftSide.cs` |
| **Souls / gold** | The in-game currency (`ECurrencyType.EGold`). Builds are priced in souls. | `GameLoop/SoulRule.cs` |
| **Ability points / unlocks** | Currencies that level abilities (`EAbilityPoints`, `EAbilityUnlocks`). | `Modules/Loadout/Progression.cs` |
| **Boon** | A level-up at a soul threshold (36 thresholds). | `Progression.cs` |
| **Imbue** | Binding an item to an ability (`ImbueItem`). | `Modules/Loadout/LoadoutService.cs` |
| **Signature 1-4** | A hero's four abilities (`EAbilitySlot.Signature1..4`). Signature 4 is the ultimate. | `Lobby/BigUlts.cs` |
| **Troopers** | Lane NPCs (`npc_trooper`). Off in Rift Roulette except the ones a capture spawns. | `CleanSlate/CleanSlateService.cs` |
| **dl_midtown** | The only map. Entity dump and mesh index in `reference/maps/dl_midtown/`. | `resources.md` |

## Deadworks and the engine

| Term | Meaning | Where |
|---|---|---|
| **Deadworks** | Community framework that loads .NET plugins into a Deadlock dedicated server. | [github.com/Deadworks-net/deadworks](https://github.com/Deadworks-net/deadworks) |
| **Plugin class** | A type implementing `IDeadworksPlugin`. One DLL can host many; each gets its own timers, config and hooks. | `resources.md` |
| **Load context** | Each plugin DLL loads into its own `AssemblyLoadContext`. Types and statics are never shared across DLLs. | `resources.md` |
| **Hot reload** | Uploading a changed DLL reloads it: `OnLoad(isReload: true)` runs, `OnStartupServer` does not, and old timers are dropped. | `.rules` §0.2 |
| **Hook** | A `DeadworksPluginBase` override the game calls (`OnTakeDamage`, `OnGameFrame`, ...). Some can return `HookResult.Stop` to block the action. | `generated/deadworks-api.md` |
| **Game event** | A named engine event (`player_death`) received with `[GameEventHandler("name")]`. | `generated/deadworks-api.md` |
| **Convar** | A console variable (`citadel_team_size`). Set through `ServerConVars.TrySet`. | `Shared/ConVars/ServerConVars.cs` |
| **Designer name** | An entity's class name in the map (`point_worldtext`, `npc_trooper`). | `Entities.ByDesignerName` |
| **Schema field** | A raw engine field on a native class (`CCitadelGameRules.m_vNextKothLocation`), read or written by name. | `Rift/RiftGameRules.cs` |
| **Modifier / modifier state** | A buff or debuff (`modifier_citadel_silenced`) and the state flags it sets (`EModifierState.Silenced`). | `Modules/Restraint/` |
| **Net message** | A protobuf message sent to clients (`CCitadelUserMsg_HudGameAnnouncement`). | `NetMessages.Send` |
| **Controller / pawn** | The controller is the player's connection (slot, Steam ID, team); the pawn is their hero in the world. | `Lobby/Participants.cs` |
| **Slot** | A player's connection index. Admin commands take `<slot>`. | `.rules` §6 |
| **Observer** | The spectator pawn (`observer`) created by `MakeObserver()`. | `Modules/Spectate/` |

## Bublock and Rift Roulette

| Term | Meaning | Where |
|---|---|---|
| **Bublock** | This repo: Theo's multi-plugin home. Three DLLs: RiftRoulette, DevTools, CleanSlate. | `README.md` |
| **Rift Roulette** | The game mode: round-based rift fights with random heroes and real builds. Renamed from Rift Rumble on 2026-09-27. | `RiftRoulette/FEATURE.md` |
| **Module** | Reusable, game-agnostic source (`Modules/<Name>`) compiled into a DLL via `.projitems`. Never ships alone. | `.rules` §0 |
| **Service / operation** | Plain C# class holding the logic; plugin classes are thin wrappers around it. | `.rules` §6 |
| **Clean / Debug mode** | Execution mode. Lifecycle runs Clean (quiet logs); admin commands run Debug (verbose). Gameplay is identical. | `Shared/Execution/` |
| **Match / round / intermission** | A match is a series of rounds. Between rounds is a 5 s intermission when heroes and builds are handed out. | `GameLoop/MatchService.cs` |
| **Hero mode** | `random` (default), `draft` or `duel` (1v1). | `GameLoop/MatchConfig.cs` |
| **Up top / watch spot** | The skybox floor (z 1536) above the current rift, where players wait between rounds, restrained. | `Round/WatchSpot.cs` |
| **Restrained** | Silenced, no items, no shooting, no melee, ignored by NPCs, takes no damage. | `Modules/Restraint/` |
| **Per-slot spots** | Each slot has its own offset from an anchor, so a group never teleports onto one point. | `Round/SlotSpots.cs`, `Round/Data/spots.json` |
| **Bench** | With an odd player count, one player sits out a round so teams stay even. | `RandomMode/BenchRule.cs` |
| **Admin seat** | The 13th connection, admin only, on the spectator side. Admins land here on connect. | `Lobby/AdminSeat.cs` |
| **Stream camera** | Automatic camera for a seated admin: follows fights, cuts to kills, top-down on big ults. | `Lobby/StreamCam.cs` |
| **Winner stays on** | 1v1 mode: the first two in the queue fight, the loser goes to the back. | `Duel/DuelService.cs` |
| **Chips** | Betting currency. Never affects gameplay. | `Betting/BetBook.cs` |
| **Stage** | A numbered step in `master-plan.md`. Work goes one stage at a time. | `master-plan.md` |
| **Archive / oracle** | Frozen known-good source kept outside the repo, used to check parity. | `.rules` §0.2 |
| **Self-test** | `dw_selftest_run`: an in-game check of every game dependency, one PASS / WARN / FAIL line each. | `SelfTest/` |
