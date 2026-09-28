# Movement.projitems

Adds the Movement registry and service (no commands): `MovementLocation`,
`LocationRegistry`, `MovementService`.

```xml
<Import Project="..\Modules\Movement\Movement.projitems" />
```

- Requires `Shared.projitems` and a `DeadworksManaged.Api` reference.
- Import this alone when another DLL needs teleports but must not register
  the `/mv_*` commands (command names would clash across DLLs).
