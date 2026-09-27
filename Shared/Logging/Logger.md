# Logger

Writes structured lines for one feature (or the master log).

## Operations

- `Trace` / `Debug` / `Info` / `Warn` / `Error` / `Critical`, each with an
  optional leading `PlayerRef` (the affected player). `Error` and
  `Critical` also accept an `Exception`, written with its stack.
- `Log(level, player, exception, template, args)` is the general form.
- `WithMode(ExecutionMode)` returns a logger for the same file:
  `Clean` → Information and above, `Debug` → Trace and above.
- `IsEnabled(level)` for guarding expensive argument building.

## Output

- Feature logger: `<feature>-YYYYMMDD.log`, prefix `<Dll>.<Feature>`.
- Master logger: `master-YYYYMMDD.log`, prefix `<Dll>`.
- Feature `Warning`+ lines are also written to master with the feature
  prefix and ` (see <feature>.log)` appended; the exception stack stays in
  the feature file only.

## Invariants

- The level check runs before the template is rendered.
- Never throws into callers.
- Obtain loggers via `BublockLog.Master` / `BublockLog.For("Feature")`.
