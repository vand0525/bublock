# RestraintCommands.projitems

MSBuild shared-items file that adds `RestraintPlugin` (the per-frame
`Sustain` hook and the `/restrain*`, `/status_*` admin commands) to a
consuming project.

```xml
<Import Project="..\Modules\Restraint\Restraint.projitems" />
<Import Project="..\Modules\Restraint\RestraintCommands.projitems" />
```

- Requires `Restraint.projitems` and `Shared.projitems`.
- Import it in exactly one DLL per server. Today that DLL is
  `RiftRoulette.dll`.
