# LogHub

Owns one DLL's log folder: session id, round id, the master logger, feature
loggers, and their rolling files.

## Behavior

- Constructor creates `directory`, runs `LogRetention` (default 7 days),
  and generates `SessionId` (8 hex chars).
- `Master` writes to `master-YYYYMMDD.log` with prefix `<DllTag>`.
- `For(feature)` returns a cached Clean-mode logger writing to
  `<feature lowercased>-YYYYMMDD.log` with prefix `<DllTag>.<Feature>`.
- `RoundId` is settable; lines show `round=-` while it is null.
- `Dispose` closes all open files.

## Failure handling

- Any exception from directory creation, retention, or a write disables file
  logging for this hub, closes its files, and emits exactly one
  `[Bublock.Logging] <DllTag>: file logging disabled (...)` line through the
  fallback (default `Console.WriteLine`). Later writes are dropped.
- Never throws into callers after construction.

## Invariants

- All writes are serialized with one lock (timers and async continuations
  may log from different threads).
- Constructor arguments (clock, max bytes, retention, fallback) exist for
  tests; plugins use `BublockLog`, which builds one hub per DLL.
