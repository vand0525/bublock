# LocationRegistry

Pure name -> `MovementLocation` registry. No game types, so it is unit tested
in `Tests/Modules.Tests` (`LocationRegistryTests`).

## Operations

| Op | Behavior |
|---|---|
| `IsValidName(name)` | `true` for one-argument names: letters, digits, `_`, `.`, `-` |
| `Register(location, saved = false)` | Adds or replaces the location with that name (case-insensitive). Throws `ArgumentException` for an invalid name |
| `Unregister(name)` | Removes a **saved** location; returns `false` for unknown names and for code-registered locations (protected) |
| `TryGet(name, out location)` | Case-insensitive lookup |
| `IsSaved(name)` | `true` if the name exists and was registered with `saved: true` |
| `List()` | All locations sorted by name, each with its saved flag |

## State

- Code-registered locations (`saved: false`) come from game code such as
  `RiftRouletteLocations.RegisterAll()`.
- Saved locations (`saved: true`) come from `/mv_save`; they live until the
  DLL reloads (not persisted).

## Invariants

- Instance class; the shared instance is `MovementService.Locations`. Tests
  create their own instances.
- Re-registering a name replaces it and takes the new saved flag.
