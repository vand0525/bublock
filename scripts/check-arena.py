#!/usr/bin/env python3
"""Check a game type's arena asset (arena.json) against the dl_midtown world mesh,
or probe the floor at points while designing a new arena.

  python3 scripts/check-arena.py GunGame/Data/arena.json [...]
  python3 scripts/check-arena.py --probe X Y [X Y ...]

Reuses scripts/check-spots.py (mesh, floor / body / line-of-sight test, offsets).
Read-only; exits 1 if any spot fails. See check-arena.py.md.
"""

import importlib.util
import json
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
_spec = importlib.util.spec_from_file_location("check_spots", HERE / "check-spots.py")
spots = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(spots)

TOP = 4000.0
BOTTOM = -2000.0


def floors(mesh, x, y):
    """Every surface a vertical ray at (x, y) crosses, highest first."""
    hits = set()
    for tri in mesh.near(x - 1, y - 1, x + 1, y + 1):
        t = spots.ray_hit((x, y, TOP), (0.0, 0.0, -1.0), tri)
        if t is not None and TOP - t >= BOTTOM:
            hits.add(round(TOP - t, 1))
    return sorted(hits, reverse=True)


def check_arena(mesh, path):
    arena = json.loads(Path(path).read_text())
    failures = 0
    print(f"\n{path}: {arena.get('name', 'arena')}")

    for team in ("amber", "sapphire"):
        anchor = (tuple(arena[team]["position"]), tuple(arena[team]["angle"]))
        print(f"  {team} anchor={tuple(round(v) for v in anchor[0])} yaw={anchor[1][1]}")
        for slot, local in enumerate(arena["offsets"]):
            spot = spots.offset(anchor, local)
            floor_z, blocked, wall = spots.check_spot(mesh, anchor[0], spot)
            problems = []
            if floor_z is None:
                problems.append(f"no floor within {spots.FLOOR_DROP:.0f} below")
            elif abs(floor_z - spot[2]) > 16:
                problems.append(f"floor at z={floor_z:.0f} ({floor_z - spot[2]:+.0f})")
            if blocked:
                problems.append(f"geometry inside the body at {tuple(round(v) for v in blocked)}")
            if wall is not None:
                problems.append(f"wall between anchor and spot at {wall:.0f} units")
            failures += bool(problems)
            where = f"({spot[0]:.0f},{spot[1]:.0f},{spot[2]:.0f})"
            print(f"    {'ok  ' if not problems else 'FAIL'} slot {slot:2d} {where:24s} {'; '.join(problems)}")

    bounds = arena.get("bounds")
    if bounds:
        print(f"  bounds min={bounds['min']} max={bounds['max']}")
    return failures


def main(argv):
    if not argv:
        print(__doc__)
        return 2

    mesh = spots.Mesh()

    if argv[0] == "--probe":
        values = [float(v) for v in argv[1:]]
        if len(values) < 2 or len(values) % 2:
            sys.exit("--probe needs X Y pairs")
        for x, y in zip(values[0::2], values[1::2]):
            print(f"({x:.0f}, {y:.0f}): floors {floors(mesh, x, y)}")
        return 0

    failures = sum(check_arena(mesh, path) for path in argv)
    print(f"\n{failures} spot(s) failed")
    return 1 if failures else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
