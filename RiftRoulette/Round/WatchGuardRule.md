# WatchGuardRule

Pure rule for "this waiting player has left the watch spot".

## Operations

| Op | Returns |
|---|---|
| `Margin` | 300 units of room below the watch spot |
| `Line(spotZ)` | `spotZ - Margin` (1236 for the watch spots at z = 1536) |
| `IsBelow(z, spotZ)` | true when `z < Line(spotZ)` |

## Invariants

- The line is well below where anyone stands or jumps up top and far above
  the lanes (z of about 250), so only a real fall or an unstuck teleport
  crosses it.
- No Deadworks calls; linked into `RiftRoulette.Tests` (`WatchGuardRuleTests`).
