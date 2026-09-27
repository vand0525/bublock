# Queue.projitems

MSBuild shared-items file that compiles `PlayerQueue` into a consuming
project.

```xml
<Import Project="..\Modules\Queue\Queue.projitems" />
```

- No dependencies: not even `Shared.projitems` or `DeadworksManaged.Api`.
- The module has no commands; the consumer adds its own (Rift Roulette's
  `/queue`, `/unqueue`, `/duel_queue*` live on `DuelPlugin`).
- Produces no DLL of its own; never ship as a library in `plugins/`.
