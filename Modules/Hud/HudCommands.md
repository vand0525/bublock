# HudCommands.projitems

Adds `HudPlugin` (the `/hud_announce` and `/hud_say` admin commands).

```xml
<Import Project="..\Modules\Hud\Hud.projitems" />
<Import Project="..\Modules\Hud\HudCommands.projitems" />
```

- Requires `Hud.projitems` and `Shared.projitems`.
- Import it in exactly one DLL per server (command names would clash across
  DLLs). Today that DLL is `RiftRoulette.dll`.
