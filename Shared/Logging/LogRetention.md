# LogRetention

Deletes old rolled log files so disk use stays bounded.

## Behavior

- `DeleteOlderThan(directory, utcNow, days = 7)` scans `*.log` directly in
  `directory` (not recursive).
- Only files named `<base>-YYYYMMDD.log` or `<base>-YYYYMMDD.N.log` are
  considered; the date comes from the file name, not file timestamps.
- Deletes files whose date is before `utcNow.Date - days` (today plus the
  previous 7 days are kept). Returns the number deleted.

## Side effects

- Deletes files. Per-file delete failures are skipped silently.

## Invariants

- Called by `LogHub` when it starts (once per DLL load).
