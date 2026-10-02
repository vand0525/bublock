# StreamCamRule

Pure decision for the stream camera's park step (`StreamCam`, when nobody
is fighting). No game calls; unit tested
(`Tests/RiftRoulette.Tests/StreamCamRuleTests`).

## Types

- `ParkAction`: `Stay` (send nothing), `Park` (first park for this side),
  `Repark` (send the same park again).

## Operations

| Operation | Result |
|---|---|
| `ParkStep(flyCam, parkedForSide, placed, handled, moved, sinceLastPark, reparkEvery)` | Not in fly cam: `Park` if not parked for this side yet, else `Repark` once `reparkEvery` has passed since the last park (a park only moves the client in fly cam, so it is resent for when the viewer presses C), else `Stay`. In fly cam: `Stay` while `moved`; else `Park` if not parked for this side; else `Stay` if the park landed (`placed`) or the viewer moved the camera after it landed (`handled`); else `Repark` once `reparkEvery` has passed (the park did not land), else `Stay`. No `sinceLastPark` counts as due. |

## Invariants

- Never `Park` or `Repark` while the viewer is moving the camera in fly cam.
- Once the viewer has moved a landed camera, it stays where they left it
  until the side changes or a follow resets `parkedForSide`.
