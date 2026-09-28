# MovementCommands.projitems

Adds `MovementPlugin` (the `/mv_*` admin commands).

```xml
<Import Project="..\Modules\Movement\Movement.projitems" />
<Import Project="..\Modules\Movement\MovementCommands.projitems" />
```

- Requires `Movement.projitems` and `Shared.projitems`.
- Import it in exactly one DLL per server (command names would clash across
  DLLs). Today that DLL is `RiftRoulette.dll`.
