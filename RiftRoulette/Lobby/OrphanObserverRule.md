# OrphanObserverRule

Pure rule for `LobbyService.SweepOrphanObservers`: which `observer` pawns a
disconnect may remove. Tested by `OrphanObserverRuleTests`.

## Operations

| Op | Behavior | Returns |
|---|---|---|
| `SlotOf(controller)` | Slot of a controller handle: `(handle & IndexMask) - 1` (`IndexMask` 0x3FFF, the mask `CBaseEntity.EntityIndex` uses; a controller's entity index is slot + 1). `NoHandle` gives -1 | slot |
| `ShouldRemove(owner, leaverSlot, leaverController, slotController, slotConnected)` | `owner` is the pawn's `m_hController`. False when the owner is `NoHandle`, the leaver's slot is connected again, or the owner is in another slot. Otherwise: with the leaver's controller handle known, true only when the owner is that handle; without it (a disconnect with no controller), true unless the owner is the controller now in the slot (`slotController`) | remove? |

## Invariants

- Never removes an observer pawn owned by another slot. Every connected
  player owns one: a seated admin's live spectator pawn, and a spare one
  per hero player (index next to the hero pawn). Removing those crashed a
  spectating admin's client (2026-10-02, 16:43 and 20:45).
- An observer with no owner is kept (unknown; logged by the sweep).
- A slot reused by a new connection keeps its new pawns (connected, or a
  different controller handle).
