# ShopAccess

Owns `citadel_allow_purchasing_anywhere`. The only code that sets it.

## Operations

| Op | Effect |
|---|---|
| `BuyAnywhere` | `ShopRule.BuyAnywhere(MatchConfig.IsDuel, MatchService.State.IsRunning)` |
| `ConVarValue` | Current convar value (`ConVar.GetInt`), null if the convar is missing |
| `Sync(mode)` | Sets the convar to 1 / 0 from `BuyAnywhere` every call (`ServerConVars.TrySet`, one Warning if the convar is missing); when the value differs from the last one applied, logs `Buying anywhere State=On/Off Mode= MatchRunning=` (Information, `match` log) and returns true |

## Called from

- `LobbyService.ApplyServerConvars` (startup, hot reload, `/lobby_setup`).
- `MatchService.Start` (after the state starts running), `MatchService.End`
  (after the state resets), `MatchService.SetHeroMode` (after the mode changes).

## Invariants

- The convar is replicated (`sv, cl, rep, cheat`) and can be switched at
  runtime; `SetInt` bypasses the cheat flag.
- Mode never changes the outcome, only the log detail.
