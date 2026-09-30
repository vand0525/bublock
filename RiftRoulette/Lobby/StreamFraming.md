# StreamFraming

Pure framing math for the stream camera's park (`StreamCam`). No game
calls; unit tested (`Tests/RiftRoulette.Tests/StreamFramingTests`).

## Types

- `CameraPose(Offset, Pitch, Yaw)`: a camera view relative to a watch spot
  anchor. `Offset` is `(forward, right, up)` in the anchor's frame
  (`MovementLocation.Offset`), `Yaw` is added to the anchor's yaw, `Pitch`
  is absolute.

## Operations

| Operation | Result |
|---|---|
| `Default` | Offset `(0, 0, OverheadHeight)` (264: above the 1536 floor up top), pitch 89, yaw 0 (the anchor's yaw). |
| `Pick(saved, side)` | The side's pose; else for Green/Yellow the mirrored other side's; else `Default`. Center never mirrors Green/Yellow. |
| `ToWorld(anchor, pose)` | World position `anchor.Offset(pose.Offset)` and angle `(pitch, anchor yaw + yaw, 0)` (yaw wrapped). |
| `FromWorld(anchor, position, angle)` | The inverse: `anchor.LocalOf(position)`, the pitch, the yaw minus the anchor's yaw (wrapped). |
| `Parse(json)` / `Serialize(poses)` | The `streamcam.json` shape: `{ "green": { "offset": [f, r, u], "pitch": p, "yaw": y }, "yellow": ... }`. `Parse` skips unknown sides and offsets without three numbers. |

## Invariants

- The two watch spot anchors face each other (half a turn apart), so a
  pose saved on one side lands mirrored on the other.
