# OrphanObserverRuleTests

Tests `RiftRoulette/Lobby/OrphanObserverRule` (linked source).

- `SlotOf` reads the slot from a controller handle's index; no handle is -1.
- The leaver's observer pawn is removed.
- Observer pawns owned by other slots (a seated admin, hero players' spare
  pawns) are kept, with or without the leaver's handle.
- An observer with no owner is kept.
- Nothing is removed once the leaver's slot is connected again.
- A new controller in the same slot keeps its observer.
- Without the leaver's handle: removed unless the slot's current controller
  owns it.
