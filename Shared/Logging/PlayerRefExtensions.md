# PlayerRefExtensions

`controller.ToPlayerRef()` builds a `PlayerRef` from any
`CBasePlayerController` (including `CCitadelPlayerController`).

## Inputs / outputs

- Reads `Slot`, `PlayerSteamId`, `PlayerName` from the controller.
- Returns a `PlayerRef` value; no side effects.

## Dangerous constraints

- The controller must be a live, non-null entity. Kept in its own file so
  tests that compile Shared never call into Deadworks types.
