# LogLevel

Log severity, same names and order as .NET `Microsoft.Extensions.Logging.LogLevel`
(without `None`).

`Trace` < `Debug` < `Information` < `Warning` < `Error` < `Critical`

## Invariants

- The enum name is written verbatim into each log line (`[Information]`).
- `Warning` and above from a feature logger are also copied to the master log.
