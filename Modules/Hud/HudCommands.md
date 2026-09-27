# HudCommands.projitems

MSBuild shared-items file that adds `HudPlugin` (the `/hud_announce` and `/hud_say` admin
commands) to a consuming project.

```xml
<Import Project="..\Modules\Hud\Hud.projitems" />
<Import Project="..\Modules\Hud\HudCommands.projitems" />
```

- Requires `Hud.projitems` and `Shared.projitems`.
- Import it in exactly one DLL per server. Today that DLL is
  `RiftRoulette.dll`.
