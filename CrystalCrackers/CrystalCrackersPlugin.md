# CrystalCrackersPlugin

Thin plugin class (Name `Crystal Crackers`) for the Crystal Crackers mode
DLL. Today it only proves the shared modules load in a second mode DLL; no
gameplay yet (see `RiftRoulette/reference/crystal-crackers.md`).

## Hooks (Clean mode)

| Hook | Does |
|---|---|
| `OnLoad(isReload)` | Master `Module host started Reload=... LogFolder=...` |
| `OnStartupServer` | Master `Server startup Map=...` |
| `OnUnload` | Master `Module host stopping (plugin unload)` |

## Commands

None of its own. The imported module plugin classes register theirs
(`/wt_*`, `/mv_*`, `/hud_*`, `/loadout_*`, `/restrain*`, `/status_*`).

## Logging

Folder `bublock/logs/CrystalCrackers/`, file `master-YYYYMMDD.log`, prefix
`[CrystalCrackers]`.

## Constraints

- Never deploy next to `RiftRoulette.dll`: both register the same module
  commands and each keeps its own copy of module statics.
