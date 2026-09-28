---
id: how-deadworks-mods-work
type: mental-model
tags: [deadworks, plugins, lifecycle, hot-reload]
related:
  - ../glossary.md
  - talking-to-the-game.md
  - rift-roulette-architecture.md
  - ../generated/deadworks-api.md
---

# How Deadworks mods work

## The picture

```text
Deadlock dedicated server (Windows build; our host runs it under Wine at Z:\gameserver\server)
  game/bin/win64/deadworks.exe          starts the game with the .NET host
  game/bin/win64/managed/
    DeadworksManaged.dll                the host (loader, command registry, timers)
    DeadworksManaged.Api.dll            the API every plugin compiles against   <- our lib/
    Google.Protobuf.dll                 shared, for net messages                <- our lib/
    plugins/*.dll                       EVERY dll here is loaded as a plugin    <- deploy.sh uploads here
  game/bin/win64/configs/               host-written plugin configs (sibling of managed/)
  game/bin/win64/bublock/               our data: logs/<Dll>/, access.json
```

1. The server boots through Deadworks, which starts a .NET runtime inside
   the game process.
2. The host loads every `managed/plugins/*.dll`, each in its **own
   collectible `AssemblyLoadContext`**. Only the API, the host and protobuf
   are shared. Two of our DLLs cannot call each other or share statics
   (verified, `resources.md` 2026-09-26).
3. In each DLL, **every** non-abstract `IDeadworksPlugin` type is
   instantiated. That is why one DLL (RiftRoulette) holds about 20 small
   plugin classes (Lobby, Draft, Rift, GameLoop, ...) that share statics
   freely.
4. The host calls our hooks (`OnLoad`, `OnGameFrame`, `OnTakeDamage`, ...)
   and routes `[Command]` methods on plugin classes to `/name`, `!name` and
   `dw_name`.
5. Uploading a changed DLL **hot-reloads** it: `OnUnload`, a fresh load
   context, then `OnLoad(isReload: true)`. `OnStartupServer` does not run
   again and old timers are gone, so startup work must also run on reload.

## Consequences we design around

| Fact | So we |
|---|---|
| Every DLL in `plugins/` is a plugin | compile shared code in as source (`Shared/`, `Modules/*` via `.projitems`), never as a library DLL |
| Commands register only from plugin-class methods | keep `[Command]` wrappers on plugin classes; logic lives in services |
| Separate load contexts | never compose features across DLLs; never `Server.ExecuteCommand("dw_...")` to reach another feature |
| `Timer` belongs to a plugin instance | services take an `ITimer` from the calling plugin |
| A plugin's `Assembly.Location` is empty (loaded from bytes) | find paths from the API assembly (`LogPaths.ResolveRoot`) |
| Hot reload skips `OnStartupServer` | re-run CleanSlate cleanup, convars and boards in `OnLoad(isReload: true)` |
| Enum values are compiled into our DLL | rebuild against the server's exact `DeadworksManaged.Api.dll` after every Deadworks update |

## Where to look things up

- Every hook, event, enum value and type: [generated/deadworks-api.md](../generated/deadworks-api.md).
- Official docs: <https://docs.deadworks.net/>. Schema and map explorers: <https://deadworks.net/db/schema>, <https://deadworks.net/db/map>.
- Source of the host (how loading really works): `managed/PluginLoader.cs` in the Deadworks repo.
- Verified pitfalls: `RiftRoulette/reference/resources.md` → Discoveries.
