# AccessStore

The join-access file, shared by every mode DLL (namespace
`Bublock.Modules.Access`). Static. The source of truth is `access.json` in
the `bublock/` folder on the server (the parent of `LogPaths.ResolveRoot()`,
i.e. `game/bin/win64/bublock/access.json`;
`Z:\gameserver\server\game\bin\win64\bublock\access.json` on this server).
See `AccessList.md` for the file shape. Game rules (statues, kicks, pause)
stay in the mode DLL (Rift Roulette: `Lobby/AccessService`; Crystal
Crackers: `AccessGate`).

## Operations

| Op | Behavior | Returns |
|---|---|---|
| `FilePath` | Absolute path of `access.json` | path |
| `Load()` | Missing file: open mode with empty lists. Otherwise re-reads the file only when its UTC write time changed; logs Information on each load. Unreadable or bad JSON: Error log, keeps the last good lists | `AccessList` |
| `Ban` / `Unban` / `Allow` / `Disallow(steamId, mode)` | Changes one list, saves, Info + master line; no save when nothing changed | reply |
| `SetPrivate(privateMode, mode)` | Sets the mode, saves, Info + master line | reply |
| `SetStatueModifier(name, mode)` | Sets `statueModifier` (trimmed; blank or null clears it), saves, Info + master line | reply |
| `Describe()` | Mode, statue modifier and path, then both lists on one line each | 3 lines |
| `DescribeIds(title, ids, suffix)` | Count, then one ID per line (with the name when connected), plus the optional per-ID suffix | lines |

## Side effects

- `Save` writes `access.json.tmp`, then moves it over `access.json`
  (creates the `bublock/` folder if needed). A failed write logs an Error,
  forces a re-read on the next `Load`, and throws `CommandException` so the
  admin sees it.
- Logs to the consuming DLL's `access` feature log (`[<Dll>.Access]`).

## Invariants

- Hand edits apply on the next connection or command without a rebuild or
  reload.
- The file never grants admin; admins are the compiled `AdminAuth` set.
- Deploys only upload DLLs, so `access.json` survives them and mode swaps.
- A mode that is always private uses `AccessList.Check(..., forcePrivate:
  true)`; it never writes `private` to the file.
