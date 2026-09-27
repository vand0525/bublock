# LogPaths

Resolves the server log root.

## Behavior

- `ResolveRoot()` returns `<managed>/../bublock/logs`, where `<managed>` is
  the folder of the shared `DeadworksManaged.Api` assembly
  (`game/bin/win64/managed/`). On this server:
  `/server/game/bin/win64/bublock/logs/`.
- Falls back to `AppContext.BaseDirectory/bublock/logs` if the API assembly
  has no location.

## Dangerous constraints

- Plugin DLLs are loaded from a byte stream, so a plugin's own
  `Assembly.Location` is empty. Always resolve through the API assembly.
- Logs live beside `managed/` (like the host's `configs/`) because
  `managed/` may be deleted on Deadworks updates.
- Verified on the live server at the Stage 12 push: resolves to
  `Z:\gameserver\server\game\bin\win64\bublock\logs` (SFTP
  `/server/game/bin/win64/bublock/logs/`). `dw_dev_logpath` (DevTools)
  prints the resolved path.
