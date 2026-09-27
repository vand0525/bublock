# WorldTextCommands.projitems

MSBuild shared-items file that adds `WorldTextPlugin` (the `/wt_*` admin
commands) to a consuming project.

```xml
<Import Project="..\Modules\WorldText\WorldText.projitems" />
<Import Project="..\Modules\WorldText\WorldTextCommands.projitems" />
```

- Requires `WorldText.projitems` and `Shared.projitems`.
- Import it in exactly one DLL per server; two DLLs registering `/wt_*` would
  clash. Today that DLL is `RiftRoulette.dll`.
