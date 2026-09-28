# Economy.projitems

MSBuild shared-items file that compiles `SoulRule` into a consuming
project.

```xml
<Import Project="..\Modules\Economy\Economy.projitems" />
```

- Needs `DeadworksManaged.Api` (currency enums).
- No commands; produces no DLL of its own.
