# Shared.projitems

MSBuild shared-items file that compiles `Bublock/Shared/` sources into a
consuming project.

## Usage

Consumers add, inside their `.csproj`:

```xml
<Import Project="..\Shared\Shared.projitems" />
```

## Behavior

- Adds each Shared `.cs` file as a `Compile` item (linked under `Shared/` in
  the IDE).
- Produces no DLL of its own; code lands inside each consumer's assembly.

## Invariants

- New Shared source files must be listed here explicitly.
- Consumers must reference `DeadworksManaged.Api` (Shared uses `ConVar`)
  and `Google.Protobuf` (`PlayerChat` sends a net message).
- Never build Shared as a separate library DLL for the server plugins folder.
