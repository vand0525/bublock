# LogHubTests

End-to-end checks of `LogHub` + `Logger` writing into a temp folder:

- Feature logger writes its own file with `<Dll>.<Feature>` prefix and
  player fields (exact line).
- Master logger uses the `<Dll>` prefix.
- Feature `Warning`+ copied to master with `(see <feature>.log)`; stack
  stays in the feature file; Information stays out of master.
- Clean mode drops Debug/Trace; `WithMode(Debug)` keeps them.
- `RoundId` shows in lines once set.
- `For` caches loggers case-insensitively; session id is 8 chars.
- Constructor runs 7-day retention.
- An unwritable folder disables file logging and emits exactly one
  fallback line.
