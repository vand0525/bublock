# LoadoutCommands.projitems

Adds `LoadoutPlugin` (the `/loadout_*` admin commands).

```xml
<Import Project="..\Modules\Loadout\LoadoutCommands.projitems" />
```

- Requires `Loadout.projitems`.
- Import it in only one Bublock DLL (command names would clash across DLLs).
