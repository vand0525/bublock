# RestraintCommands.projitems

Adds `RestraintPlugin` (the per-frame `Sustain` hook and the `/restrain*`,
`/status_*` admin commands).

```xml
<Import Project="..\Modules\Restraint\Restraint.projitems" />
<Import Project="..\Modules\Restraint\RestraintCommands.projitems" />
```

- Requires `Restraint.projitems` and `Shared.projitems`.
- Import it in exactly one DLL per server (command names would clash across
  DLLs). Today that DLL is `RiftRoulette.dll`.
