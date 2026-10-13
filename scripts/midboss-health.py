#!/usr/bin/env python3
"""Recompute the mid-boss souls-to-game-minute table (Modules/MidBoss/MidBossRule) from real matches.

Read-only: prints the game minute at which the lobby's average net worth per
player reaches each soul value, the mid-boss health and resistance the game
gives a boss spawned at that minute, and the souls and minutes of real
mid-boss kills. Writes nothing; copy the printed table into MidBossRule.cs by
hand. See midboss-health.py.md.
"""

import argparse
import json
import statistics
import urllib.error
import urllib.parse
import urllib.request

API = "https://api.deadlock-api.com"

# npc_units.vdata, npc_super_neutral (check on patch day; MidBossRule holds the same values).
STARTING_HEALTH = 13000
HEALTH_PER_MINUTE = 195
RESISTANCE = 35
RESISTANCE_PER_MINUTE = 5


def get(path, **params):
    query = urllib.parse.urlencode({k: v for k, v in params.items() if v is not None})
    request = urllib.request.Request(f"{API}{path}?{query}", headers={"User-Agent": "Bublock-midboss-health"})

    try:
        with urllib.request.urlopen(request, timeout=300) as response:
            return json.load(response)
    except urllib.error.HTTPError as error:
        raise SystemExit(f"HTTP {error.code} for {path}: {error.read()[:300]!r}")


def net_worth_at(stats, time_s):
    points = [(0, 0)] + [(s["time_stamp_s"], s["net_worth"]) for s in stats]

    for (t0, n0), (t1, n1) in zip(points, points[1:]):
        if t0 <= time_s <= t1:
            return n0 + (n1 - n0) * (time_s - t0) / max(1, t1 - t0)

    return points[-1][1]


def lobby_curve(players):
    """(time, average net worth per player) at every snapshot time of the match."""
    stats = [p["stats"] for p in players if p.get("stats")]
    times = sorted({s["time_stamp_s"] for player in stats for s in player})
    return [(0, 0.0)] + [(t, sum(net_worth_at(player, t) for player in stats) / len(stats)) for t in times if t > 0]


def time_reaching(curve, souls):
    for (t0, n0), (t1, n1) in zip(curve, curve[1:]):
        if n0 < souls <= n1:
            return t0 + (t1 - t0) * (souls - n0) / max(1e-9, n1 - n0)

    return None


def percentile(values, fraction):
    ordered = sorted(values)
    return ordered[min(len(ordered) - 1, int(fraction * len(ordered)))]


def boss_health(minute):
    return STARTING_HEALTH + HEALTH_PER_MINUTE * round(minute)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--matches", type=int, default=500, help="recent ranked matches to read (default 500)")
    parser.add_argument("--min-badge", type=int, default=None, help="minimum average badge (default any)")
    parser.add_argument("--step", type=int, default=2500, help="soul step of the table (default 2500)")
    parser.add_argument("--min-share", type=float, default=0.25,
                        help="stop the table when fewer than this share of matches reach the souls (default 0.25)")
    args = parser.parse_args()

    matches = get(
        "/v1/matches/metadata",
        include_player_stats="true",
        include_mid_boss="true",
        game_mode="normal",
        match_mode="ranked",
        min_duration_s=1200,
        min_average_badge=args.min_badge,
        order_by="match_id",
        order_direction="desc",
        limit=args.matches,
    )

    badges = [m.get("average_badge") or 0 for m in matches]
    durations = [m["duration_s"] / 60 for m in matches]
    print(f"{len(matches)} ranked matches, median badge {statistics.median(badges) if badges else 0}, "
          f"median length {statistics.median(durations):.1f} min")

    curves = [lobby_curve(m["players"]) for m in matches if any(p.get("stats") for p in m["players"])]

    print("\nReal mid-boss kills: game minute, lobby and killing team average net worth per player (p25 / median / p75):")
    kills = {}

    for match in matches:
        for number, kill in enumerate(sorted(match.get("mid_boss") or [], key=lambda k: k["destroyed_time_s"]), 1):
            time_s = kill["destroyed_time_s"]
            killers = [p for p in match["players"] if p.get("stats") and p["team"] == kill["team_killed"]]
            everyone = [p for p in match["players"] if p.get("stats")]

            if time_s > 0 and killers:
                lobby = sum(net_worth_at(p["stats"], time_s) for p in everyone) / len(everyone)
                team = sum(net_worth_at(p["stats"], time_s) for p in killers) / len(killers)
                kills.setdefault(number, []).append((time_s / 60, lobby, team))

    for number in sorted(kills):
        values = kills[number]
        if len(values) < 20:
            continue
        columns = [[v[i] for v in values] for i in range(3)]
        print(f"  kill {number}: n={len(values):>4}  minute " +
              " / ".join(f"{percentile(columns[0], f):4.1f}" for f in (0.25, 0.5, 0.75)) + "  lobby " +
              " / ".join(f"{percentile(columns[1], f):6.0f}" for f in (0.25, 0.5, 0.75)) + "  killers " +
              " / ".join(f"{percentile(columns[2], f):6.0f}" for f in (0.25, 0.5, 0.75)))

    print("\nGame minute when the lobby average reaches each soul value (median), and the boss the game spawns then:")
    print("   souls  matches  minute  p25-p75        health  resistance")
    table = []
    souls = 0

    while True:
        souls += args.step
        times = [t / 60 for t in (time_reaching(curve, souls) for curve in curves) if t is not None]

        if len(times) < args.min_share * len(curves):
            break

        minute = statistics.median(times)
        table.append((souls, minute))
        print(f"  {souls:>6}  {len(times):>7}  {minute:6.1f}  {percentile(times, 0.25):5.1f}-{percentile(times, 0.75):5.1f}"
              f"  {boss_health(minute):>12,}  {RESISTANCE + RESISTANCE_PER_MINUTE * minute:10.0f}")

    print("\nMidBossRule.MinuteBySouls (copy into MidBossRule.cs):")
    print("  [(0, 0f), " + ", ".join(f"({s}, {m:.1f}f)" for s, m in table) + "]")


if __name__ == "__main__":
    main()
