# Loadout.projitems

Adds the Loadout service (no commands): `HeroBuildData`, `HeroBuildCatalog`,
`LoadoutPlanner`, `LoadoutService`. Embeds `Data/hero-builds.json` as the
resource `Bublock.Modules.Loadout.hero-builds.json` (`LogicalName`).

```xml
<Import Project="..\Modules\Loadout\Loadout.projitems" />
```

- Requires `Shared.projitems` and a `DeadworksManaged.Api` reference.
