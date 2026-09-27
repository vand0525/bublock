# DraftState

The draft picks: which heroes are taken and which player (Steam ID) took
each. Static, pure data; no game calls, no logging. Extracted from the
Legacy `SelectedHeroes` / `PlayerSelections` fields in Stage 8 so Lobby can
release picks on disconnect / kick and show them in `/status`.

## State

- `Selected`: `HashSet<Heroes>` of drafted heroes (same collection type as
  the archive, so `SelectedHeroes` enumerates in the same order `/selected`
  printed).
- `Picks`: `Dictionary<ulong, Heroes>` from Steam ID to hero.

## Operations

| Op | Behavior |
|---|---|
| `SelectedHeroes` | Drafted heroes (live view of the set) |
| `AllPicks` | Every `(SteamId, Hero)` pick (live view; used by `/picks`, `/draft_status`) |
| `IsSelected(hero)` | Whether anyone has drafted the hero |
| `HasPick(steamId)` / `TryGetPick(steamId, out hero)` | The player's pick, if any |
| `Add(steamId, hero)` | Records a pick in both collections. Throws if the player already has one (`Dictionary.Add`, same as the archive); callers check `HasPick` first |
| `Release(steamId, out hero)` | Removes the player's pick from both collections; false if they had none |
| `Clear()` | Empties both (draft reset) |

## Invariants

- A hero is in `Selected` exactly when some Steam ID maps to it in `Picks`,
  as long as all changes go through these ops.
- Keyed by Steam ID, not slot or entity index.
- Lives for one DLL load (static; reset on plugin reload, as the archive's
  instance fields were).
- Game-thread only (not thread-safe).
