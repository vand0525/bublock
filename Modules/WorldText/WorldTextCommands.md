# WorldTextCommands.projitems

Adds `WorldTextPlugin` (the `/wt_*` admin commands).

```xml
<Import Project="..\Modules\WorldText\WorldText.projitems" />
<Import Project="..\Modules\WorldText\WorldTextCommands.projitems" />
```

- Requires `WorldText.projitems` and `Shared.projitems`.
- Import it in exactly one DLL per server (command names would clash across
  DLLs). Today that DLL is `RiftRoulette.dll`.
