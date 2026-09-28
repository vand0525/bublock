#!/usr/bin/env python3
"""Recompute the loadout item-limit breakpoints (Modules/Loadout/ItemSlots) from real matches.

Read-only: prints player net worth when their team's 1st, 2nd and 3rd enemy
Walker fell, suggested breakpoints, and items held per net worth band. Writes
nothing; copy the suggested breakpoints into ItemSlots.cs by hand. See
walker-souls.py.md.
"""

import argparse
import collections
import json
import statistics
import urllib.error
import urllib.parse
import urllib.request

API = "https://api.deadlock-api.com"
BASE_SLOTS = 9
BAND = 2000


def get(path, **params):
    query = urllib.parse.urlencode({k: v for k, v in params.items() if v is not None})
    request = urllib.request.Request(f"{API}{path}?{query}", headers={"User-Agent": "Bublock-walker-souls"})

    try:
        with urllib.request.urlopen(request, timeout=180) as response:
            return json.load(response)
    except urllib.error.HTTPError as error:
        raise SystemExit(f"HTTP {error.code} for {path}: {error.read()[:300]!r}")


def net_worth_at(stats, time_s):
    points = [(0, 0)] + [(s["time_stamp_s"], s["net_worth"]) for s in stats]

    for (t0, n0), (t1, n1) in zip(points, points[1:]):
        if t0 <= time_s <= t1:
            return n0 + (n1 - n0) * (time_s - t0) / max(1, t1 - t0)

    return points[-1][1]


def percentile(values, fraction):
    ordered = sorted(values)
    return ordered[min(len(ordered) - 1, int(fraction * len(ordered)))]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--matches", type=int, default=500, help="recent ranked matches to read (default 500)")
    parser.add_argument("--min-badge", type=int, default=None, help="minimum average badge (default any)")
    args = parser.parse_args()

    upgrades = {item["id"] for item in get("/v1/assets/items") if item.get("type") == "upgrade"}
    matches = get(
        "/v1/matches/metadata",
        include_player_items="true",
        include_player_stats="true",
        include_objectives="true",
        game_mode="normal",
        match_mode="ranked",
        min_duration_s=1200,
        min_average_badge=args.min_badge,
        order_by="match_id",
        order_direction="desc",
        limit=args.matches,
    )

    badges = [m.get("average_badge") or 0 for m in matches]
    print(f"{len(matches)} ranked matches, median badge {statistics.median(badges) if badges else 0}")

    walker_worth = collections.defaultdict(list)
    held = collections.defaultdict(list)
    no_walker = collections.defaultdict(int)

    for match in matches:
        objectives = match.get("objectives") or []

        for player in match["players"]:
            stats = player.get("stats") or []
            if not stats:
                continue

            enemy_walkers = sorted(
                o["destroyed_time_s"]
                for o in objectives
                if o["team_objective"].startswith("Tier2") and o["team"] != player["team"] and o["destroyed_time_s"] > 0
            )

            for number, time_s in enumerate(enemy_walkers[:3], 1):
                walker_worth[number].append(net_worth_at(stats, time_s))

            items = [i for i in player.get("items", []) if i["item_id"] in upgrades]

            for sample in stats:
                time_s = sample["time_stamp_s"]
                band = int(sample["net_worth"] // BAND * BAND)
                held[band].append(sum(
                    1 for i in items
                    if i["game_time_s"] <= time_s and (i["sold_time_s"] == 0 or i["sold_time_s"] > time_s)))

                if not any(t <= time_s for t in enemy_walkers):
                    no_walker[band] += 1

    print("\nNet worth when the team's enemy Walker fell (p25 / median / p75):")
    suggested = []

    for number in (1, 2, 3):
        values = walker_worth[number]
        if not values:
            print(f"  Walker {number}: no data")
            continue

        median = percentile(values, 0.5)
        print(f"  Walker {number}: n={len(values)} {percentile(values, 0.25):.0f} / {median:.0f} / {percentile(values, 0.75):.0f}")
        suggested.append((int(round(median / 1000) * 1000), BASE_SLOTS + number))

    print("\nSuggested ItemSlots.Breakpoints (median rounded to the nearest 1,000):")
    print("  [" + ", ".join(f"({souls}, {slots})" for souls, slots in suggested) + "]")

    print(f"\nItems held per {BAND:,} net worth band (p25 / median / p75 / max, share with no Walker down):")
    for band in sorted(held):
        values = held[band]
        if len(values) < 30:
            continue
        print(
            f"  {band:>6}: n={len(values):>5} {percentile(values, 0.25):>2} / {percentile(values, 0.5):>2} / "
            f"{percentile(values, 0.75):>2} / {max(values):>2}  no walker {no_walker[band] / len(values):.2f}")


if __name__ == "__main__":
    main()
