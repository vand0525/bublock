# MovementCommands.projitems

MSBuild shared-items file that adds `MovementPlugin` (the `/mv_*` admin
commands) to a consuming project.

```xml
<Import Project="..\Modules\Movement\Movement.projitems" />
<Import Project="..\Modules\Movement\MovementCommands.projitems" />
```

- Requires `Movement.projitems` and `Shared.projitems`.
- Import it in exactly one DLL per server; two DLLs registering `/mv_*` would
  clash. Today that DLL is `RiftRoulette.dll`.
