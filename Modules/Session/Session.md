# Session.projitems

MSBuild shared-items file that compiles `Scoreboard`, `SessionRule` and
`TimedSession` into a consuming project.

```xml
<Import Project="..\Modules\Session\Session.projitems" />
```

- `TimedSession` needs `Shared.projitems` (logging) and
  `DeadworksManaged.Api` (timers); `Scoreboard` and `SessionRule` are pure.
- No commands; produces no DLL of its own.
