# Movement.projitems

MSBuild shared-items file that compiles the Movement **registry and service**
(no commands) into a consuming project.

```xml
<Import Project="..\Modules\Movement\Movement.projitems" />
```

- Includes `MovementLocation`, `LocationRegistry`, `MovementService`.
- Requires `Shared.projitems` (logging, `ExecutionMode`, `PlayerRef`) and a
  `DeadworksManaged.Api` reference.
- Import this alone when another DLL needs teleports but must not register
  the `/mv_*` commands (command names would clash across DLLs).
- Produces no DLL of its own; never ship as a library in `plugins/`.
