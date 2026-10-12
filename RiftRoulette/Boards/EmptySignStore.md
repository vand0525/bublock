# EmptySignStore

Keeps the empty-server sign's settings in `bublock/emptysign.json` (next
to `welcomenote.txt`, beside the logs folder), so `/sign_*` changes
survive reloads, map changes and restarts. Static.

## State

- `_current`: the settings, loaded from the file on first use (per DLL
  load).

## Operations

| Operation | Effect |
|---|---|
| `FilePath` | `<bublock>/emptysign.json`, from `LogPaths.ResolveRoot()` |
| `Current` | The settings. First use loads the file: no file means defaults; a file whose values fail `Problem()` logs a Warning and uses defaults; an unreadable or broken file logs an Error and uses defaults. A good load logs Information `Empty sign settings loaded Settings=` |
| `Set(settings, mode)` | Replaces `Current` and writes the file (Debug `Empty sign settings written`). A write failure logs an Error and keeps the settings for this load only |

## Logs

`Boards` feature log (`boards-YYYYMMDD.log`).

## Invariants

- Never throws: every read or write failure falls back (defaults, or
  this load only).
- `Set` does not validate; callers check `Problem()` first
  (`EmptySign.Change`).
