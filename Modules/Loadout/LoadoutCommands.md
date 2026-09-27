# LoadoutCommands.projitems

MSBuild shared-items file that compiles `LoadoutPlugin` (the `/loadout_*`
admin commands). Import it together with `Loadout.projitems`, in only one
Bublock DLL, so command names never clash.

```xml
<Import Project="..\Modules\Loadout\LoadoutCommands.projitems" />
```
