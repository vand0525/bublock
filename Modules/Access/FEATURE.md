# Access module

## Purpose

Join access shared by every mode DLL: the banned and allowed Steam64 ID
lists and the open / private mode, kept in `bublock/access.json` on the
server. Game agnostic: it decides a verdict and edits the file; what
happens to a banned or refused player (statue, kick, refusal) is up to the
mode DLL.

## Files

| File | Role |
|---|---|
| `AccessRule.cs` | Pure rule: ban wins, then private (allowed or admin), Steam64 ID check (tested) |
| `AccessList.cs` | The file model, JSON parse and write, `Check` with optional `forcePrivate` (tested) |
| `AccessStore.cs` | File load (write-time cache), list and mode changes, atomic save, describe lines |
| `Access.projitems` | Adds the three to a consumer |

## Public operations

`AccessStore.Load`, `Ban`, `Unban`, `Allow`, `Disallow`, `SetPrivate`,
`SetStatueModifier`, `Describe`, `DescribeIds`; `AccessList.Check`;
`AccessRule.TryParseSteamId` (see the file docs).

## State

`AccessStore` caches the last good list and its write time (per DLL load).
The file is the source of truth and survives reloads, deploys and mode
swaps.

## Commands and lifecycle

No commands and no plugin class. Consumers:

- Rift Roulette: `Lobby/AccessService` (statues, kicks, pause follows the
  mode) and `Lobby/AccessPlugin` (`/player_ban`, `/ban_*`, `/allow_*`,
  `/access_mode`, `/ban_modifier`).
- Crystal Crackers: `AccessGate` and `AccessPlugin` (always private,
  refused or kicked; `/ban_*`, `/allow_*`, `/access_mode`).

## Tests

`Tests/Modules.Tests/AccessRuleTests.cs`, `AccessListTests.cs`.
