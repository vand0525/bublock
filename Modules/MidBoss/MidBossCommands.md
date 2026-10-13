# MidBossCommands.projitems

Adds `MidBossPlugin` (the `/midboss_*` admin commands and the watcher hooks).

```xml
<Import Project="..\Modules\MidBoss\MidBoss.projitems" />
<Import Project="..\Modules\MidBoss\MidBossCommands.projitems" />
```

- Needs `MidBoss.projitems` (the service) and `Shared.projitems`.
- Command names are `midboss_*`; only one DLL on the server may import it.
