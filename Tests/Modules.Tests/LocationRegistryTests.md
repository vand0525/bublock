# LocationRegistryTests

Unit tests for `Modules/Movement/LocationRegistry` (pure; no game needed).

- Lookup ignores case; unknown names are not found.
- Registering an existing name (any case) replaces it; the list keeps one entry.
- Names must match `^[A-Za-z0-9_.-]+$`; invalid names (empty, spaces,
  slashes) fail `IsValidName` and make `Register` throw `ArgumentException`.
- Code-registered locations cannot be removed (`Unregister` returns false and
  the location stays).
- Saved locations report `IsSaved`, can be removed once (case-insensitive),
  and a second removal returns false.
- `List` is sorted by name (case-insensitive) and marks which entries are saved.
