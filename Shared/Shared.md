# Shared.projitems

Adds every `Bublock/Shared/` source file (linked under `Shared/` in the IDE).
New Shared source files must be listed here explicitly.

```xml
<Import Project="..\Shared\Shared.projitems" />
```

- Requires `DeadworksManaged.Api` (Shared uses `ConVar`) and
  `Google.Protobuf` (`PlayerChat` sends a net message) references.
