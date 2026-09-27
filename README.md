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

## Build

Needs the Deadworks API next to this folder: `../lib/DeadworksManaged.Api.dll`,
`../lib/Google.Protobuf.dll` and `../Directory.Build.props`.
`scripts/update.sh` builds, `scripts/test.sh` runs the [tests](Tests/FEATURE.md).
Deploying needs `scripts/server.env` (copy `server.env.example`).

## License

[PolyForm Strict 1.0.0](LICENSE): source available for non-commercial use;
no redistribution and no modified versions. Hero build data, map extracts and
game data belong to their owners (Valve, build authors) and are not covered by
this license.
