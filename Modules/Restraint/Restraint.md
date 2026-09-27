# Restraint.projitems

MSBuild shared-items file that compiles `RestraintService` (no commands)
into a consuming project.

```xml
<Import Project="..\Modules\Restraint\Restraint.projitems" />
```

- Requires `Shared.projitems` (logging, `ExecutionMode`, `PlayerRef`) and a
  `DeadworksManaged.Api` reference.
- Without `RestraintCommands.projitems` nothing calls `Sustain` every frame;
  the consumer must then call it from its own `OnGameFrame`.
- Produces no DLL of its own; never ship as a library in `plugins/`.
