# BublockLog

Per-DLL entry point to logging. Each Bublock DLL compiles its own copy, so
each DLL gets its own hub and folder.

## Operations

- `BublockLog.Master` — master logger (`master-YYYYMMDD.log`).
- `BublockLog.For("Draft")` — feature logger (`draft-YYYYMMDD.log`,
  prefix `<Dll>.Draft`).
- `SessionId` (read), `RoundId` (read/write), `Directory` (this DLL's log
  folder), `Hub` (the underlying `LogHub`).

## Behavior

- The hub is created lazily on first use; no `Initialize` call and no
  dependency on plugin-class load order.
- DLL tag = the consuming assembly name (`RiftRoulette`, `DevTools`,
  `CleanSlate`), used for the folder and the master prefix.
- Folder: `LogPaths.ResolveRoot()/<DllTag>/`.
- Registers `AssemblyLoadContext.Unloading` for the DLL's load context to
  close files on unload / hot reload.

## Invariants

- One session id per DLL load; a hot reload starts a new session.
- Never throws into callers (path failures fall back, write failures
  disable file logging with one console line).
