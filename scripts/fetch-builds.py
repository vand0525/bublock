#!/usr/bin/env python3
"""Fetch the top hero builds from the Deadlock API into Modules/Loadout/Data/hero-builds.json.

Offline data step: the plugin never queries the API. Rerun for fresher builds,
then rebuild and deploy. See fetch-builds.py.md.
"""

import json
import os
import sys
import time
import urllib.parse
import urllib.request
from datetime import datetime, timezone

API = "https://api.deadlock-api.com"
WINDOW_DAYS = 14
BUILDS_PER_HERO = 3
REQUEST_GAP_SECONDS = 0.35

# currency_type values in a build's ability_order
ABILITY_UNLOCK = 2
ABILITY_POINTS = 1
MAX_UPGRADES = 3

# Laning / farming items that do nothing in rift rounds, and Healing Rite:
# never stored, so the next item in the build takes the slot.
BANNED_ITEMS = {
    "upgrade_non_player_bonus",
    "upgrade_non_player_bonus_sacrifice",
    "upgrade_goose_egg",
    "upgrade_trophy_collector",
    "upgrade_health_stimpak",
}

dropped_items = 0

HERE = os.path.dirname(os.path.abspath(__file__))
OUTPUT = os.path.join(HERE, "..", "Modules", "Loadout", "Data", "hero-builds.json")


def get(path, **params):
    query = urllib.parse.urlencode({k: v for k, v in params.items() if v is not None})
    url = f"{API}{path}" + (f"?{query}" if query else "")
    request = urllib.request.Request(url, headers={"User-Agent": "Bublock-fetch-builds"})

    for attempt in range(4):
        time.sleep(REQUEST_GAP_SECONDS)
        try:
            with urllib.request.urlopen(request, timeout=60) as response:
                return json.load(response)
        except urllib.error.HTTPError as error:
            if error.code == 429 and attempt < 3:
                time.sleep(15)
                continue
            raise


def playable_heroes():
    heroes = get("/v1/assets/heroes")
    return sorted(
        (h for h in heroes if h.get("player_selectable") and not h.get("in_development") and not h.get("disabled")),
        key=lambda h: h["id"],
    )


def item_tables():
    items = get("/v1/assets/items")
    upgrades = {i["id"]: i for i in items if i.get("type") == "upgrade" and i.get("shopable")}
    abilities = {i["id"]: i["class_name"] for i in items if i.get("type") == "ability"}
    return upgrades, abilities


def ranked_build_ids(hero_id, since):
    stats = get(f"/v1/analytics/hero-build-stats/{hero_id}", min_unix_timestamp=since)
    stats.sort(key=lambda s: (-s.get("matches", 0), -s.get("wins", 0)))
    return [(s["hero_build_id"], s.get("matches", 0), s.get("wins", 0)) for s in stats]


def fetch_build(build_id):
    found = get("/v1/builds", build_id=build_id, only_latest="true")
    return found[0] if found else None


def fallback_builds(hero_id, sort_by):
    return get("/v1/builds", hero_id=hero_id, sort_by=sort_by, sort_direction="desc", only_latest="true", limit=10)


def convert(entry, rank, matches, wins, upgrades, abilities):
    global dropped_items
    hero_build = entry["hero_build"]
    details = hero_build.get("details") or {}

    items, imbues, categories, sell_priority = [], {}, [], {}
    for category in details.get("mod_categories") or []:
        category_items = []
        for mod in category.get("mods") or []:
            item = upgrades.get(mod.get("ability_id"))
            if item is None:
                continue
            name = item["class_name"]
            if name in BANNED_ITEMS:
                dropped_items += 1
                continue
            if name not in category_items:
                category_items.append(name)
            if name not in items:
                items.append(name)
            target = abilities.get(mod.get("imbue_target_ability_id"))
            if target:
                imbues[name] = target
            # Only non-zero values mean anything; higher sells first.
            priority = mod.get("sell_priority") or 0
            if priority > 0 and name not in sell_priority:
                sell_priority[name] = priority
        if category_items:
            categories.append({
                "name": (category.get("name") or "").strip(),
                "optional": bool(category.get("optional")),
                "items": category_items,
            })

    # Some builds repeat the whole order (author edits appended); keep each
    # ability's first unlock and first MAX_UPGRADES upgrades.
    steps, unlocked, upgrades_done = [], set(), {}
    for change in (details.get("ability_order") or {}).get("currency_changes") or []:
        ability = abilities.get(change.get("ability_id"))
        kind = {ABILITY_UNLOCK: "unlock", ABILITY_POINTS: "upgrade"}.get(change.get("currency_type"))
        if not ability or not kind:
            continue
        if kind == "unlock":
            if ability in unlocked:
                continue
            unlocked.add(ability)
        else:
            if upgrades_done.get(ability, 0) >= MAX_UPGRADES:
                continue
            upgrades_done[ability] = upgrades_done.get(ability, 0) + 1
        steps.append({"ability": ability, "kind": kind})

    return {
        "buildId": hero_build["hero_build_id"],
        "version": hero_build.get("version"),
        "name": (hero_build.get("name") or "").strip(),
        "rank": rank,
        "matches": matches,
        "wins": wins,
        "favorites": entry.get("num_favorites") or 0,
        "items": items,
        "categories": categories,
        "imbues": imbues,
        "sellPriority": sell_priority,
        "abilities": steps,
    }


def builds_for(hero, since, upgrades, abilities):
    builds, seen = [], set()

    def add(entry, rank, matches=0, wins=0):
        if entry is None or len(builds) >= BUILDS_PER_HERO:
            return
        hero_build = entry["hero_build"]
        if hero_build["hero_build_id"] in seen or hero_build.get("hero_id") != hero["id"]:
            return
        build = convert(entry, rank, matches, wins, upgrades, abilities)
        if not build["items"]:
            return
        seen.add(build["buildId"])
        builds.append(build)

    for build_id, matches, wins in ranked_build_ids(hero["id"], since):
        if len(builds) >= BUILDS_PER_HERO:
            break
        add(fetch_build(build_id), "matches", matches, wins)

    for sort_by in ("weekly_favorites", "favorites"):
        if len(builds) >= BUILDS_PER_HERO:
            break
        for entry in fallback_builds(hero["id"], sort_by):
            add(entry, sort_by)

    return builds


def main():
    now = datetime.now(timezone.utc)
    since = int(now.timestamp()) - WINDOW_DAYS * 86400

    upgrades, abilities = item_tables()
    by_class = {i["class_name"]: i for i in upgrades.values()}
    heroes_out, used = [], set()

    for hero in playable_heroes():
        builds = builds_for(hero, since, upgrades, abilities)
        print(f"{hero['id']:>3} {hero['name']:<16} {len(builds)} build(s) " +
              ", ".join(f"{b['rank']}:{b['matches']}" for b in builds))
        if not builds:
            continue
        for build in builds:
            used.update(build["items"])
        heroes_out.append({
            "id": hero["id"],
            "className": hero["class_name"],
            "name": hero["name"],
            "builds": builds,
        })

    components = {
        name: [c for c in by_class[name].get("component_items") or []]
        for name in sorted(used)
        if by_class[name].get("component_items")
    }

    item_costs = {name: by_class[name].get("cost") or 0 for name in sorted(used)}

    data = {
        "fetchedAt": now.strftime("%Y-%m-%dT%H:%M:%SZ"),
        "source": API,
        "windowDays": WINDOW_DAYS,
        "heroes": heroes_out,
        "components": components,
        "itemCosts": item_costs,
    }

    os.makedirs(os.path.dirname(OUTPUT), exist_ok=True)
    with open(OUTPUT, "w", encoding="utf-8") as out:
        json.dump(data, out, indent=1, ensure_ascii=False)
        out.write("\n")

    print(f"Dropped {dropped_items} banned item entries ({', '.join(sorted(BANNED_ITEMS))})")
    print(f"Wrote {len(heroes_out)} heroes to {os.path.normpath(OUTPUT)}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
