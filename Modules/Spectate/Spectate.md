# Spectate.projitems

MSBuild shared-items file that compiles `SpectateRule` and
`SpectateService` (no commands) into a consuming project.

```xml
<Import Project="..\Modules\Spectate\Spectate.projitems" />
```

- Requires `Shared.projitems` (logging, `ExecutionMode`, `PlayerRef`),
  `Movement.projitems` (`MovementService.SetViewAngle`) and a
  `DeadworksManaged.Api` reference.
- Has no plugin class or commands; the consumer owns the timer and the
  commands (Rift Roulette: `Lobby/StreamCam`, `Lobby/LobbyPlugin`).
- Produces no DLL of its own; never ship as a library in `plugins/`.
