# Hud.projitems

Adds `HudService` (no commands).

```xml
<Import Project="..\Modules\Hud\Hud.projitems" />
```

- Requires `Shared.projitems` and a `DeadworksManaged.Api` reference.
- Import this alone when another DLL needs banners but must not register
  `/hud_announce` / `/hud_say` (command names would clash across DLLs).
