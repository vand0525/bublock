# Access.projitems

Adds `AccessRule`, `AccessList` and `AccessStore` (no commands).

```xml
<Import Project="..\Modules\Access\Access.projitems" />
```

- Needs `Shared.projitems` (logging, `LogPaths`, `CommandException`) and
  `DeadworksManaged.Api` (`Players` for connected names).
