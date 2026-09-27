# ServerConVars

Sets a server convar and reports when the convar does not exist.

## Operations

- `TrySet(name, value, log)`: `ConVar.Find(name)?.SetInt(value)`. Returns
  `false` when the convar is missing and logs one Warning
  `Convar missing, value not applied Name= Value=` on the caller's logger
  (so it is also copied to the master log).

## Invariants

- Warns once per convar name per DLL load; later misses return `false`
  quietly, so per-round callers (`citadel_koth_enabled`) do not spam.
- A game patch that renames or removes a convar shows up as this Warning in
  `master-*.log` instead of failing silently.
- Convars set through `Server.ExecuteCommand` (cheat / dev-only ones) do not
  go through here; `dw_selftest_run` checks those.
