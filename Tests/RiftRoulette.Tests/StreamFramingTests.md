# StreamFramingTests

Unit tests for `RiftRoulette/Lobby/StreamFraming`.

- The default framing is 264 units straight above the anchor, pitch 89,
  facing the anchor's yaw.
- `FromWorld` then `ToWorld` gives back the same position and angle.
- A framing saved against one anchor lands mirrored on an anchor turned
  half a turn (the two rift sides).
- `Pick` takes the side's framing, else the other side's, else the default.
- `Serialize` / `Parse` round-trip; `Parse` skips unknown sides and offsets
  without three numbers.
