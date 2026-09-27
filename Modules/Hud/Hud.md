# Hud.projitems

MSBuild shared-items file that compiles `HudService` (no commands) into a
consuming project.

```xml
<Import Project="..\Modules\Hud\Hud.projitems" />
```

- Requires `Shared.projitems` (logging, `ExecutionMode`, `PlayerRef`) and a
  `DeadworksManaged.Api` reference.
- Import this alone when another DLL needs banners but must not register
  `/hud_announce` / `/hud_say` (command names would clash across DLLs).
- Produces no DLL of its own; never ship as a library in `plugins/`.
