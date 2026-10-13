# MidBoss.projitems

Adds `MidBossRule` and `MidBossService` (no commands).

```xml
<Import Project="..\Modules\MidBoss\MidBoss.projitems" />
```

- Requires `Shared.projitems` and a `DeadworksManaged.Api` reference.
- Without `MidBossCommands.projitems` nothing calls the watchers
  (`ObserveSpawned`, `ObserveDeleted`, `ObserveKilled`, `WatchCounts`) or
  `ScaleHit`; the consumer must wire its own hooks.
