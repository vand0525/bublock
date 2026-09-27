# patch-check.py

Offline check of what a Deadlock / Deadworks update changed, filtered to
what Bublock uses. Part of `RiftRoulette/reference/patch-day.md`.

## Usage

```bash
python3 Bublock/scripts/patch-check.py --save-baseline   # before a patch (patch-baseline.sh)
python3 Bublock/scripts/patch-check.py                   # after: diff vs newest baseline
python3 Bublock/scripts/patch-check.py --baseline DIR    # diff vs a given snapshot folder
```

Needs network (deadlock-api.com, raw.githubusercontent.com); about 20 s.
Exits 1 when there is any `HIT`, 0 otherwise.

## Snapshot (`reference/baseline/<UTC date>/snapshot.json`)

- `heroes`: `/v1/assets/heroes` by id (class name, name, playable =
  selectable, not in development, not disabled).
- `items`: `/v1/assets/items` by class name (type, shopable).
- `cvars`: the upstream cvar list (name to flags).
- `mapCounts`: counts of map-placed entity names in the local
  `maps/dl_midtown/entities.json`.
- `api`: public members per type and every enum value of
  `lib/DeadworksManaged.Api.dll`, decompiled with `~/.dotnet/tools/ilspycmd`
  (skipped with a NOTE when ilspycmd is missing).
- `meta`: save time, `lib` SHA-256, `hero-builds.json` fetch time. A copy of
  `hero-builds.json` sits next to it.

## Checks (after a patch)

The source scan reads every `.cs` under `Shared`, `Modules`, `RiftRoulette`,
`DevTools`, `CleanSlate` for identifiers, `ConVar.Find` / `ExecuteCommand`
convar names and entity-name strings.

| Area | HIT when |
|---|---|
| Convar | a convar we set was in the baseline list and is gone (flag changes and never-listed convars are NOTEs; the upstream list can lag a patch) |
| Hero | a `hero-builds.json` hero is no longer playable or its class name changed; a new playable hero is not in the `Heroes` enum; a `Heroes.X` used in Draft / Lobby is not in the enum or has no hero in the asset (new playable heroes are NOTEs: rerun `fetch-builds.py`) |
| Item / Ability | an item (build, category, imbue, component, banned list) or ability in `hero-builds.json` is gone from the items asset |
| Map | a map-placed entity count differs from the baseline, for names our source uses (NOTE for the others). Only changes after a fresh map dump |
| API | a public member of a type our source names is gone or its signature changed, when our source also uses that member name; a type we name is gone |
| Enum | a value of an enum member our source uses changed or was removed |

## Constraints

- Read-only except `--save-baseline`, which writes under
  `RiftRoulette/reference/baseline/`.
- The "used" filter is by identifier name, so it can miss a use or report a
  same-named member; read the HIT before changing code.
