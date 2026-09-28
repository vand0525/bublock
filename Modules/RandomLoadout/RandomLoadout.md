# RandomLoadout.projitems

MSBuild shared-items file that compiles `HeroRoll` and `RandomLoadouts`
into a consuming project.

```xml
<Import Project="..\Modules\Loadout\Loadout.projitems" />
<Import Project="..\Modules\RandomLoadout\RandomLoadout.projitems" />
```

- Needs `Shared.projitems` and `Loadout.projitems` (which embeds
  `hero-builds.json`) in the same project.
- No commands; produces no DLL of its own.
