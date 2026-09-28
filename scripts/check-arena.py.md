# check-arena.py

Checks a game type's arena asset against the `dl_midtown` world mesh, and
probes floor heights while designing a new arena. Offline, read-only.

## Usage

```bash
python3 scripts/check-arena.py GunGame/Data/arena.json [more.json ...]
python3 scripts/check-arena.py --probe X Y [X Y ...]
```

## Checks (per team anchor and slot offset)

The same test as `check-spots.py` (it imports that script): a floor within
64 units below the spot, no geometry inside a hero-sized cylinder, and a
clear line from the anchor to the spot. Prints `ok` / `FAIL` per slot and
the bounds; exits 1 when any spot fails.

## Probe

`--probe X Y` prints every surface a vertical ray at (X, Y) crosses,
highest first (for example the middle lane street at z 376, bridges above
the center, the mid-boss pit at -768). Use it to pick anchor heights.

## Requirements

`RiftRoulette/reference/maps/dl_midtown/world.bin.gz` (git-ignored; fetch
steps in `reference/resources.md`, "dl_midtown map explorer export") and
`world.json` (in git).
