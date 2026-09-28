# Arena.projitems

MSBuild shared-items file that compiles `ArenaSpots` and `ArenaService`
into a consuming project.

```xml
<Import Project="..\Modules\Movement\Movement.projitems" />
<Import Project="..\Modules\Teams\Teams.projitems" />
<Import Project="..\Modules\Arena\Arena.projitems" />
```

- Needs `Shared.projitems`, `Movement.projitems` and `Teams.projitems` in
  the same project.
- The arena itself is the consumer's embedded JSON asset (see
  `ArenaSpots.md`).
- No commands; produces no DLL of its own.
