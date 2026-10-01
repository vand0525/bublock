#!/usr/bin/env python3
"""Check every per-slot spot (RiftRoulette/Round/Data/spots.json) against the
dl_midtown world mesh.

For each spot: a floor within FLOOR_DROP units below, no geometry inside a
hero-sized cylinder above the floor, and a clear line from the anchor to the
spot (nobody placed behind a wall). Read-only; prints a table and exits 1 if
any spot fails.

Needs RiftRoulette/reference/maps/dl_midtown/world.bin.gz (not in git; see
resources.md "dl_midtown map dump" for the refresh steps).
"""

import gzip
import json
import math
import re
import struct
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
MAP_DIR = ROOT / "RiftRoulette/reference/maps/dl_midtown"
SPOTS = ROOT / "RiftRoulette/Round/Data/spots.json"
LOCATIONS = ROOT / "RiftRoulette/Locations/RiftRouletteLocations.cs"

FLOOR_UP = 24.0        # start the floor ray this far above the spot
FLOOR_DROP = 64.0      # a floor must be within this far below the spot
RADIUS = 40.0          # hero cylinder radius
BODY_LOW = 18.0        # cylinder starts this far above the floor (skips the floor itself)
BODY_HIGH = 80.0       # cylinder top above the floor
SIGHT_HEIGHT = 48.0    # anchor-to-spot line height above both points
CELL = 128.0
LOD = 0

# The watch spots stand on the skybox floor, an invisible collision surface
# that is not in the render mesh (verified in game), so no floor is required
# there; body room and the line from the anchor are still checked.
GROUPS = [
    ("watch", "watch_green", False),
    ("watch", "watch_yellow", False),
    ("watch", "watch_center", False),
    ("fight", "green_sapphire", True),
    ("fight", "green_amber", True),
    ("fight", "yellow_sapphire", True),
    ("fight", "yellow_amber", True),
    ("fight", "center_sapphire", True),
    ("fight", "center_amber", True),
]


def parse_anchors():
    text = LOCATIONS.read_text()
    pattern = re.compile(
        r'new\(\s*"(\w+)",\s*new Vector3\(([^)]*)\),\s*new Vector3\(([^)]*)\)\s*\)',
        re.S,
    )
    anchors = {}
    for name, pos, ang in pattern.findall(text):
        nums = lambda s: [float(v.strip().rstrip("f")) for v in s.split(",")]
        anchors[name] = (nums(pos), nums(ang))
    return anchors


def offset(anchor, local):
    (px, py, pz), (_, yaw, _) = anchor
    r = math.radians(yaw)
    fx, fy = math.cos(r), math.sin(r)
    rx, ry = math.sin(r), -math.cos(r)
    f, s, u = local
    return (px + fx * f + rx * s, py + fy * f + ry * s, pz + u)


class Mesh:
    def __init__(self):
        world = json.loads((MAP_DIR / "world.json").read_text())
        bin_path = MAP_DIR / "world.bin.gz"
        if not bin_path.exists():
            sys.exit(f"missing {bin_path}; download it first (see resources.md)")
        self.data = gzip.decompress(bin_path.read_bytes())
        self.chunks = [c for c in world["chunks"] if len(c["levels"]) > LOD and c["levels"][LOD]]
        self.loaded = {}
        self.grid = {}

    def _load(self, chunk):
        key = chunk["key"]
        if key in self.loaded:
            return
        self.loaded[key] = True
        level = chunk["levels"][LOD]
        (cx, cy, cz), (hx, hy, hz) = level["center"], level["half"]
        off, count = level["positions"]
        raw = struct.unpack_from(f"<{count * 3}h", self.data, off)
        verts = [
            (cx + raw[i] / 32767 * hx, cy + raw[i + 1] / 32767 * hy, cz + raw[i + 2] / 32767 * hz)
            for i in range(0, len(raw), 3)
        ]
        ioff, icount, size = level["indices"]
        fmt = "H" if size == 2 else "I"
        idx = struct.unpack_from(f"<{icount}{fmt}", self.data, ioff)
        for t in range(0, icount - 2, 3):
            tri = (verts[idx[t]], verts[idx[t + 1]], verts[idx[t + 2]])
            xs = [p[0] for p in tri]
            ys = [p[1] for p in tri]
            for gx in range(int(math.floor(min(xs) / CELL)), int(math.floor(max(xs) / CELL)) + 1):
                for gy in range(int(math.floor(min(ys) / CELL)), int(math.floor(max(ys) / CELL)) + 1):
                    self.grid.setdefault((gx, gy), []).append(tri)

    def near(self, x0, y0, x1, y1):
        for chunk in self.chunks:
            (cx, cy, _), (hx, hy, _) = chunk["levels"][LOD]["center"], chunk["levels"][LOD]["half"]
            if cx + hx >= x0 and cx - hx <= x1 and cy + hy >= y0 and cy - hy <= y1:
                self._load(chunk)
        seen = set()
        for gx in range(int(math.floor(x0 / CELL)), int(math.floor(x1 / CELL)) + 1):
            for gy in range(int(math.floor(y0 / CELL)), int(math.floor(y1 / CELL)) + 1):
                for tri in self.grid.get((gx, gy), ()):
                    if id(tri) not in seen:
                        seen.add(id(tri))
                        yield tri


def sub(a, b): return (a[0] - b[0], a[1] - b[1], a[2] - b[2])
def dot(a, b): return a[0] * b[0] + a[1] * b[1] + a[2] * b[2]
def cross(a, b): return (a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0])


def ray_hit(origin, direction, tri, eps=1e-7):
    """Moller-Trumbore; returns t or None."""
    v0, v1, v2 = tri
    e1, e2 = sub(v1, v0), sub(v2, v0)
    p = cross(direction, e2)
    det = dot(e1, p)
    if abs(det) < eps:
        return None
    inv = 1.0 / det
    s = sub(origin, v0)
    u = dot(s, p) * inv
    if u < 0 or u > 1:
        return None
    q = cross(s, e1)
    v = dot(direction, q) * inv
    if v < 0 or u + v > 1:
        return None
    t = dot(e2, q) * inv
    return t if t >= 0 else None


def closest_on_triangle(p, tri):
    """Closest point on a triangle to p (Ericson, Real-Time Collision Detection)."""
    a, b, c = tri
    ab, ac, ap = sub(b, a), sub(c, a), sub(p, a)
    d1, d2 = dot(ab, ap), dot(ac, ap)
    if d1 <= 0 and d2 <= 0:
        return a
    bp = sub(p, b)
    d3, d4 = dot(ab, bp), dot(ac, bp)
    if d3 >= 0 and d4 <= d3:
        return b
    vc = d1 * d4 - d3 * d2
    if vc <= 0 and d1 >= 0 and d3 <= 0:
        v = d1 / (d1 - d3)
        return (a[0] + ab[0] * v, a[1] + ab[1] * v, a[2] + ab[2] * v)
    cp = sub(p, c)
    d5, d6 = dot(ab, cp), dot(ac, cp)
    if d6 >= 0 and d5 <= d6:
        return c
    vb = d5 * d2 - d1 * d6
    if vb <= 0 and d2 >= 0 and d6 <= 0:
        w = d2 / (d2 - d6)
        return (a[0] + ac[0] * w, a[1] + ac[1] * w, a[2] + ac[2] * w)
    va = d3 * d6 - d5 * d4
    if va <= 0 and (d4 - d3) >= 0 and (d5 - d6) >= 0:
        bc = sub(c, b)
        w = (d4 - d3) / ((d4 - d3) + (d5 - d6))
        return (b[0] + bc[0] * w, b[1] + bc[1] * w, b[2] + bc[2] * w)
    denom = 1.0 / (va + vb + vc)
    v, w = vb * denom, vc * denom
    return (a[0] + ab[0] * v + ac[0] * w, a[1] + ab[1] * v + ac[1] * w, a[2] + ab[2] * v + ac[2] * w)


def check_spot(mesh, anchor_pos, spot):
    x, y, z = spot
    pad = RADIUS + 8
    tris = list(mesh.near(x - pad, y - pad, x + pad, y + pad))

    floor = None
    origin = (x, y, z + FLOOR_UP)
    for tri in tris:
        t = ray_hit(origin, (0.0, 0.0, -1.0), tri)
        if t is not None and t <= FLOOR_UP + FLOOR_DROP and (floor is None or t < floor):
            floor = t
    floor_z = z + FLOOR_UP - floor if floor is not None else None

    base = floor_z if floor_z is not None else z
    blocked = None
    samples = [base + BODY_LOW + (BODY_HIGH - BODY_LOW) * i / 7 for i in range(8)]
    for tri in tris:
        if max(p[2] for p in tri) < base + BODY_LOW or min(p[2] for p in tri) > base + BODY_HIGH:
            continue
        for sz in samples:
            q = closest_on_triangle((x, y, sz), tri)
            if math.dist(q, (x, y, sz)) < RADIUS:
                blocked = q
                break
        if blocked:
            break

    wall = None
    a = (anchor_pos[0], anchor_pos[1], anchor_pos[2] + SIGHT_HEIGHT)
    b = (x, y, base + SIGHT_HEIGHT)
    d = sub(b, a)
    length = math.sqrt(dot(d, d))
    if length > 1:
        direction = (d[0] / length, d[1] / length, d[2] / length)
        for tri in mesh.near(min(a[0], b[0]), min(a[1], b[1]), max(a[0], b[0]), max(a[1], b[1])):
            t = ray_hit(a, direction, tri)
            if t is not None and t < length - 1:
                wall = t
                break

    return floor_z, blocked, wall


def main():
    anchors = parse_anchors()
    offsets = json.loads(SPOTS.read_text())
    mesh = Mesh()
    failures = 0

    for table, anchor_name, needs_floor in GROUPS:
        anchor = anchors[anchor_name]
        floor_note = "" if needs_floor else " (skybox floor, not in the mesh)"
        print(f"\n{anchor_name} ({table}) anchor={tuple(round(v) for v in anchor[0])} yaw={anchor[1][1]}{floor_note}")
        for slot, local in enumerate(offsets[table]):
            spot = offset(anchor, local)
            floor_z, blocked, wall = check_spot(mesh, anchor[0], spot)
            problems = []
            if floor_z is None:
                if needs_floor:
                    problems.append(f"no floor within {FLOOR_DROP:.0f} below")
            elif abs(floor_z - spot[2]) > 16:
                problems.append(f"floor at z={floor_z:.0f} ({floor_z - spot[2]:+.0f})")
            if blocked:
                problems.append(f"geometry inside the body at {tuple(round(v) for v in blocked)}")
            if wall is not None:
                problems.append(f"wall between anchor and spot at {wall:.0f} units")
            status = "ok  " if not problems else "FAIL"
            failures += bool(problems)
            where = f"({spot[0]:.0f},{spot[1]:.0f},{spot[2]:.0f})"
            print(f"  {status} slot {slot:2d} {where:24s} {'; '.join(problems)}")

    print(f"\n{failures} spot(s) failed")
    return 1 if failures else 0


if __name__ == "__main__":
    sys.exit(main())
