# WorldText.projitems

MSBuild shared-items file that compiles the WorldText **service and types**
(no commands) into a consuming project.

```xml
<Import Project="..\Modules\WorldText\WorldText.projitems" />
```

- Includes `WorldTextColor`, `WorldTextSpec`, `WorldTextPlacement`,
  `WorldTextFormat`, `WorldTextService`.
- Requires `Shared.projitems` (logging, `ExecutionMode`) and a
  `DeadworksManaged.Api` reference.
- Import this alone when another DLL needs boards but must not register the
  `/wt_*` commands (command names would clash across DLLs).
- Produces no DLL of its own; never ship as a library in `plugins/`.
