# Bublock

Deadworks server plugins for Deadlock. The main one is **Rift Roulette**:
round-based rift fights with random heroes and real builds, a continuous
match loop, auto-balance, and a winner-stays-on 1v1 mode.

## Currently hosted

Join from the Deadlock console:

- **Bublocks Rift Roulette**: `connect 66.94.102.245:27015`

## Commands

- [Player commands](RiftRoulette/reference/user-commands.md)
- [Admin commands](RiftRoulette/reference/admin-commands.md)

## Plugins (one DLL each)

Game types run one at a time per server (`DW_PLUGINS` / `DW_PARKED` in
`scripts/server.env`); the tool plugins run beside any of them.

- [GunGame](GunGame/FEATURE.md): game type (redock fork). Continuous 2-minute
  matches in a mid-lane brawl arena; every kill gives you a new random
  hero and build; most kills wins. New game types: `scripts/new-game-type.sh`.

- [RiftRoulette](RiftRoulette/FEATURE.md): the game mode
- [CleanSlate](CleanSlate/FEATURE.md): strips lanes, NPCs and shops from the map
- [DevTools](DevTools/FEATURE.md): discovery and diagnostics

## Modules (compiled into plugins as source)

- [Shared](Shared/FEATURE.md): logging, admin auth, clean/debug mode
- [WorldText](Modules/WorldText/FEATURE.md): in-game text boards
- [Movement](Modules/Movement/FEATURE.md): named locations and teleports
- [Hud](Modules/Hud/FEATURE.md): on-screen banners
- [Loadout](Modules/Loadout/FEATURE.md): hero builds from real build data
- [Restraint](Modules/Restraint/FEATURE.md): "can't fight" state
- [Queue](Modules/Queue/FEATURE.md): player join queue
- [Spectate](Modules/Spectate/FEATURE.md): spectator camera control (follow a player, park a free camera)
- [Teams](Modules/Teams/FEATURE.md): team numbers, smaller-team placement, hero / team change guard
- [Arena](Modules/Arena/FEATURE.md): contained fight areas from an `arena.json` asset
- [Session](Modules/Session/FEATURE.md): continuous timed matches and a scoreboard
- [Economy](Modules/Economy/FEATURE.md): soul rules (power only from builds)
- [RandomLoadout](Modules/RandomLoadout/FEATURE.md): random hero + stored build per player

## Build

Needs the Deadworks API next to this folder: `../lib/DeadworksManaged.Api.dll`,
`../lib/Google.Protobuf.dll` and `../Directory.Build.props`. Without that
workspace, `scripts/fetch-deadworks.sh` puts a pinned Deadworks release in
`lib/` and the scripts use it.
`scripts/update.sh` builds, `scripts/test.sh` runs the [tests](Tests/FEATURE.md).
Deploying needs `scripts/server.env` (copy `server.env.example`). CI builds
and tests every push; deploys are manual ([CI / CD](.github/workflows/README.md)).

## Knowledge base

[knowledge/README.md](knowledge/README.md): glossary, mental models, an
effects catalog for building game modes, and generated indexes and a graph
of the code. `scripts/knowledge.sh` rebuilds them and the docs webpage
(`site/index.html`).

## License

[PolyForm Noncommercial 1.0.0](LICENSE): noncommercial use, including
changes, new works, and sharing copies. Anyone who receives a copy must keep
this license and the copyright notice. Hero build data, map extracts and game
data belong to their owners (Valve, build authors) and are not covered by this
license.
