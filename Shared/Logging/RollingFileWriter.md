# RollingFileWriter

Appends lines to one rolling log file family (`<base>-YYYYMMDD[.N].log`).

## Behavior

- Date comes from the injected UTC clock; a new UTC day opens a new file.
- Size roll: once the current file reaches `maxBytes`, the next line goes to
  `<base>-YYYYMMDD.1.log`, then `.2`, and so on.
- On open, skips existing files for the day that are already full, so a
  restart keeps appending to the newest non-full file.
- Files are UTF-8 without BOM, opened with `FileMode.Append`,
  `FileShare.ReadWrite | FileShare.Delete`, `AutoFlush = true` (every line
  is on disk immediately; readers and retention can touch open files).

## Side effects

- Creates the directory if missing. Writes files.

## Invariants

- Not thread-safe on its own; `LogHub` serializes calls.
- Throws on I/O failure; `LogHub` catches and disables file logging.
- `Dispose` closes the current file; must be called before the plugin's load
  context unloads (handled by `BublockLog`).
