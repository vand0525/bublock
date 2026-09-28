# Restraint.projitems

Adds `RestraintService` (no commands).

```xml
<Import Project="..\Modules\Restraint\Restraint.projitems" />
```

- Requires `Shared.projitems` and a `DeadworksManaged.Api` reference.
- Without `RestraintCommands.projitems` nothing calls `Sustain` every frame;
  the consumer must then call it from its own `OnGameFrame`.
