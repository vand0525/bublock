# Loadout.projitems

MSBuild shared-items file that compiles the Loadout service (no commands)
into a consuming project, and embeds `Data/hero-builds.json` as the resource
`Bublock.Modules.Loadout.hero-builds.json`.

```xml
<Import Project="..\Modules\Loadout\Loadout.projitems" />
```

- Compiles `HeroBuildData`, `HeroBuildCatalog`, `LoadoutPlanner`, and
  `LoadoutService`.
- Requires `Shared.projitems` (logging, `ExecutionMode`, `PlayerRef`) and a
  `DeadworksManaged.Api` reference.
- Produces no DLL of its own; never ship as a library in `plugins/`.
