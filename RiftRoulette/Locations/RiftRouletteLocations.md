# RiftRouletteLocations

Rift Roulette's teleport targets as typed `MovementLocation`s. Values are
copied unchanged from the archive plugin (`DraftPosition` / `DraftAngle` and
the four rift start spawns). The two watch spots (Stage 13i) sit above each
rift's spawn (`RiftSides.GreenPosition` / `YellowPosition` x, y) at the
draft height (the skybox floor). Their camera angles face the welcome
board (`Round/WatchLayout.LookAt` of the welcome offset (500, 500, 300),
turned half a turn on yellow): yaw 45 on green, -135 on yellow, pitch -23
(up). `WatchLayoutTests` keeps them in sync with the board.

| Field | Name | Position | Camera angle |
|---|---|---|---|
| `Draft` | `draft` | (0, 0, 1536.0625) | (0, 0, 0) |
| `WatchGreen` | `watch_green` | (7612, 0, 1536.0625) | (-23, 45, 0) |
| `WatchYellow` | `watch_yellow` | (-7560, 0, 1536.0625) | (-23, -135, 0) |
| `GreenSapphire` | `green_sapphire` | (8225, 1797.71875, 248.0625) | (0, -100.34375, 0) |
| `GreenAmber` | `green_amber` | (7025.65625, -2088.59375, 256.03125) | (0, 80.71875, 0) |
| `YellowSapphire` | `yellow_sapphire` | (-7072.65625, 2209.53125, 248.03125) | (0, -102.15625, 0) |
| `YellowAmber` | `yellow_amber` | (-8259.5625, -2134.3125, 248.03125) | (0, 80.875, 0) |

The archive had the two yellow starts swapped (Sapphire landed on Amber's
half). They were swapped back on 2026-09-27, each angle moving with its
position: Sapphire (team 3, base at +y) starts at +y on both lanes, Amber
(team 2, base at -y) at -y.

## Operations

- `All`: the seven locations above.
- `RegisterAll()`: registers them in `MovementService.Locations` as
  code (protected) locations. Idempotent (re-registering replaces with the
  same values). Called from `SessionPlugin.OnLoad`.

## Invariants

- Game code uses the typed fields directly, so it never depends on
  `RegisterAll()` having run (no plugin load-order coupling). Registration
  only makes the names reachable from `/mv_tp` and `/mv_list`.
- Rift **spawn** positions (where the rift itself appears) are not teleport
  targets and live in `Rift/RiftSide`.
- Since Stage 13i nothing teleports to `Draft` any more (players go to the
  watch spots through `Round/WatchSpot`); it stays registered for `/mv_tp`.
  The boards are anchored at the current watch spot (`BoardLayout.Origin`).
- The watch spots and the four starts are **anchors**: players are sent to
  their own slot spot offset from them (`Round/SlotSpots`,
  `Round/Data/spots.json`), not to the anchor itself. Moving an anchor here
  moves its whole group; rerun `scripts/check-spots.py` after any change.
