# ShopAccess

Owns `citadel_allow_purchasing_anywhere`. The only code that sets it.
Buying is always off: items reach players only through loadouts
(`AddItem`), and CleanSlate disables every shop.

## Operations

| Op | Effect |
|---|---|
| `ConVarValue` | Current convar value (`ConVar.GetInt`), null if the convar is missing |
| `Disable(mode)` | Sets the convar to 0 (`ServerConVars.TrySet`, one Warning if the convar is missing); logs `Buying anywhere off ConVar=` (Debug, `match` log) |

## Called from

- `LobbyService.ApplyServerConvars` (startup, hot reload, `/lobby_setup`).

## Invariants

- The convar is replicated (`sv, cl, rep, cheat`) and can be switched at
  runtime; `SetInt` bypasses the cheat flag.
- Mode never changes the outcome, only the log detail.
