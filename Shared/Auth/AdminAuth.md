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

- Static, read-only authorized set: `76561198192980843`.

## Invariants

- Never add an ID that is not taken from verified source.
- Each consuming DLL has its own copy (separate load contexts); editing the
  set requires rebuilding every consumer.
