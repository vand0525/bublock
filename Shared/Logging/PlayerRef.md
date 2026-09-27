# PlayerRef

Plain snapshot of the player a log line is about.

## Fields

- `Slot` — player slot (`CBasePlayerController.Slot`).
- `SteamId` — `PlayerSteamId`.
- `Name` — `PlayerName` at the time of logging.

## Invariants

- No Deadworks types, so it can be built in tests. Use
  `PlayerRefExtensions.ToPlayerRef()` to create one from a controller.
- Rendered in lines as `player="<Name>" steam=<SteamId> slot=<Slot>`.
