# WorldText.projitems

Adds the WorldText service and types (no commands): `WorldTextColor`,
`WorldTextSpec`, `WorldTextPlacement`, `WorldTextFormat`, `WorldTextService`.

```xml
<Import Project="..\Modules\WorldText\WorldText.projitems" />
```

- Requires `Shared.projitems` and a `DeadworksManaged.Api` reference.
- Import this alone when another DLL needs boards but must not register the
  `/wt_*` commands (command names would clash across DLLs).
