---
id: building-a-game-type
type: mental-model
tags: [game-types, engine, modules, scaffold, extensibility]
related:
  - rift-roulette-architecture.md
  - talking-to-the-game.md
  - ship-and-operate.md
  - ../effects-catalog.md
  - ../game-mode-recipes.md
---

# Building a game type

## Engine and game types

```text
Deadworks (host)  ─>  engine: Shared/ + Modules/*   ─>  game types: RiftRoulette.dll, GunGame.dll, <next>.dll
                                                        tools:      DevTools.dll, CleanSlate.dll
```

- **Engine** (`Shared/`, `Modules/*`): game-agnostic source compiled into
  each DLL through `.projitems`. Knows Deadlock, not any one game.
- **Game type**: one DLL with its own rules. Imports the engine, never
  another game type's code. Rift Roulette is one; Gun Game is another.
- **Tools**: CleanSlate (map cleanup) and DevTools run beside any game type.
- One game type per server at a time: `scripts/server.env` `DW_PLUGINS`
  and `DW_PARKED`; restart after parking one (a removed DLL stays loaded).

## Engine modules a game type can use

| Module | Gives | Status |
|---|---|---|
| `Session` | continuous timed matches, break, restart, scoreboard, callbacks | verified in build; Gun Game live |
| `Arena` | team anchors + per-slot spots from an `arena.json` asset, bounds, containment | spots mesh-checked |
| `Teams` | team numbers / names, smaller-team placement, hero / team change guard | tested |
| `RandomLoadout` | random hero + stored top build, pending until spawn | mid-fight swap verified in playtest |
| `Loadout` | a stored build on a pawn (items, imbues, level, ranks) | verified (Rift Roulette) |
| `Economy` | soul rule: power only from builds | tested |
| `Hud` | banners | verified |
| `Movement` | teleports, camera angle | verified |
| `WorldText` | boards in the world | verified |
| `Restraint` | silence / no shooting / no melee / immune | verified |
| `Queue` | wait-your-turn queues | verified |
| `Spectate` | spectator camera | verified |

Assets the engine reads: `Modules/Loadout/Data/hero-builds.json` (builds),
a game type's `Data/arena.json` (arena), the map dump under
`RiftRoulette/reference/maps/` (for designing arenas).

## Make one

```bash
./scripts/new-game-type.sh Grifball grif "Grifball"   # working timed TDM in the arena
python3 scripts/check-arena.py Grifball/Data/arena.json
./scripts/update.sh && ./scripts/test.sh
```

Then change three files, in this order:

1. `<Name>Rules.cs`: what scores, match length, banner text (pure, tested).
2. `<Name>Service.cs`: which engine modules to wire and when (join, spawn,
   kill, tick). Add module imports in `<Name>.csproj`.
3. `Data/arena.json`: where the fight happens (`--probe X Y` for floors).

Keep plugin classes thin (hooks and commands only), give every `.cs` a
`.md`, and add the game type's first stage to `master-plan.md`.

## When the engine is missing something

If a rule belongs to one game, keep it in the game type. If two game types
would need it, add or extend a module (game-agnostic, tested, no commands),
update `templates/game-type` if every new game type should get it, and
note it in the table above.
