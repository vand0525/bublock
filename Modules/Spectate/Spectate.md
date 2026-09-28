# Spectate.projitems

Adds `SpectateRule` and `SpectateService` (no plugin class or commands; the
consumer owns the timer and the commands, in Rift Roulette `Lobby/StreamCam`
and `Lobby/LobbyPlugin`).

```xml
<Import Project="..\Modules\Spectate\Spectate.projitems" />
```

- Requires `Shared.projitems`, `Movement.projitems`
  (`MovementService.SetViewAngle`) and a `DeadworksManaged.Api` reference.
