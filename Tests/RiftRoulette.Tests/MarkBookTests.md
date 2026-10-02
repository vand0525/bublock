# MarkBookTests

Unit tests for `RiftRoulette/Betting/MarkBook`.

- A marker holds one mark; a second `Place` is refused and the first stays.
- Marking yourself or a zero ID is refused.
- Several markers may mark the same target.
- `TryConsume` needs the marked victim and the mark's round, from that
  marker; it removes the mark.
- A mark pays once.
- Consuming one marker's mark keeps the other marks on that target.
- `TakeAll` returns every mark in marker order and clears them (the marker
  can mark again).
- `Reset` clears every mark.
