# AdminAuth

Single Steam-ID admin gate for every Bublock DLL.

## Behavior

- `IsAuthorized(ulong steamId)` returns `true` when the ID is in the
  authorized set, otherwise `false`.
- `SteamIds`: the authorized set, read-only (Rift Roulette's seat commands
  use it to find the admin without arguments).

## Inputs / outputs

- Input: a player's `PlayerSteamId`.
- Output: `bool`. No side effects, no logging. Callers decide how to report a
  rejection.

## State

- Static, read-only authorized set:
  - `76561198192980843` (Theo), copied from the archive DevTools oracle
    (`archive/DevTools/DevToolsPlugin.cs`).
  - `76561198001002148`, the owner of the redock fork's server, given by
    the owner (2026-09-27).
- Every admin connects into the admin seat (spectating, `AdminSeatRule.SeatOnJoin`)
  and may always connect (`AdminSeatRule.CanConnect`); `dw_seat_play` joins a team.

## Invariants

- Never add an ID that is not taken from verified source.
- Each consuming DLL compiles its own copy (separate load contexts); editing
  the set requires rebuilding every consumer.
