# LobbyPlugin (GunGame)

Thin host for joins, leaves and the server rules.

## Hooks

- `OnLoad(isReload)`: master log `Gun Game loaded`; convars now and again
  3 s later (after other plugins' startup convars), then
  `AdmitConnected` and `Session.Check`; a 30 s reminder while waiting
  (chat, never a banner).
- `OnStartupServer()`: convars now and 3 s later.
- `OnClientFullConnect`: `Admit` (humans).
- `OnClientDisconnect`: `Remove` (humans).
- `OnClientConCommand`: `Stop` when `BlocksCommand` (menu hero picks,
  team changes).
- `OnModifyCurrency`: `Stop` when `BlocksCurrency` (earned souls, ranks
  outside the build).

## Deadworks constraints

- Timers do not run while the server hibernates (nobody on); the 3 s step
  runs once someone joins.
