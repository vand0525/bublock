# RiftWatchTests

Unit tests for `RiftRoulette/Rift/RiftWatch`.

- A new trooper means `Finished`, even when a cash-in exists.
- The first cash-in sighting returns `CashinAppeared`; later ticks with the
  cash-in still present return `Waiting`.
- A cash-in that disappears after being seen returns `Tied`.
- With no cash-in ever seen and no trooper, every tick returns `Waiting`.
