#!/usr/bin/env python3
"""Fetch the top hero builds from the Deadlock API into Modules/Loadout/Data/hero-builds.json.

Offline data step: the plugin never queries the API. Rerun for fresher builds,
then rebuild and deploy. See fetch-builds.py.md.
"""

import argparse
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

# The API's hero assets can lag a patch and still mark a new hero unplayable.
FORCE_HEROES = {"hero_ratking", "hero_baba"}

# Signature abilities from the game's heroes.vdata (GameTracking-Deadlock) that
# the API's item assets do not list yet; their IDs are computed with ability_id().
EXTRA_ABILITIES = [
    "ability_ratking_scrap_grenade",
    "ability_ratking_ratnibble",
    "ability_ratking_ratarmor",
    "ability_ratking_standard_bearer",
]

ABILITY_KINDS = {"unlock", "upgrade"}

dropped_items = 0

HERE = os.path.dirname(os.path.abspath(__file__))
DATA = os.path.join(HERE, "..", "Modules", "Loadout", "Data")
OUTPUT = os.path.join(DATA, "hero-builds.json")
CUSTOM = os.path.join(DATA, "custom-builds.json")


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
        (h for h in heroes
         if not h.get("disabled")
         and (h["class_name"] in FORCE_HEROES or (h.get("player_selectable") and not h.get("in_development")))),
        key=lambda h: h["id"],
    )


def ability_id(class_name, seed=0x31415926):
    """MurmurHash2 of the class name: matches every item and ability ID the API lists."""
    data = class_name.encode()
    m, length = 0x5BD1E995, len(data)
    h = (seed ^ length) & 0xFFFFFFFF
    i = 0
    while length >= 4:
        k = int.from_bytes(data[i:i + 4], "little")
        k = (k * m) & 0xFFFFFFFF
        k ^= k >> 24
        k = (k * m) & 0xFFFFFFFF
        h = ((h * m) & 0xFFFFFFFF) ^ k
        i += 4
        length -= 4
    if length == 3:
        h ^= data[i + 2] << 16
    if length >= 2:
        h ^= data[i + 1] << 8
    if length >= 1:
        h ^= data[i]
        h = (h * m) & 0xFFFFFFFF
    h ^= h >> 13
    h = (h * m) & 0xFFFFFFFF
    return h ^ (h >> 15)


def item_tables():
    items = get("/v1/assets/items")
    upgrades = {i["id"]: i for i in items if i.get("type") == "upgrade" and i.get("shopable")}
    abilities = {i["id"]: i["class_name"] for i in items if i.get("type") == "ability"}
    for name in EXTRA_ABILITIES:
        abilities.setdefault(ability_id(name), name)
    return upgrades, abilities


def ranked_build_ids(hero_id, since):
    stats = get(f"/v1/analytics/hero-build-stats/{hero_id}", min_unix_timestamp=since)
    stats.sort(key=lambda s: (-s.get("matches", 0), -s.get("wins", 0)))
    return [(s["hero_build_id"], s.get("matches", 0), s.get("wins", 0)) for s in stats]


def fetch_build(build_id):
    found = get("/v1/builds", build_id=build_id, only_latest="true")
    return found[0] if found else None


def fetch_live(hero_id, build_id):
    """Straight from Steam; the search index lags new builds by an hour or more."""
    try:
        found = get(f"/v1/builds/{hero_id}/{build_id}")
    except urllib.error.HTTPError:
        return None
    if isinstance(found, list):
        found = found[0] if found else None
    return found if found and "hero_build" in found else None


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


def load_custom(by_class, upgrades, abilities, hero_ids):
    """Hand-made or published (by buildId) builds per hero class name, checked against the item table."""
    if not os.path.exists(CUSTOM):
        return {}
    with open(CUSTOM, encoding="utf-8") as source:
        raw = json.load(source)

    custom, problems = {}, []
    for class_name, entries in raw.items():
        if class_name.startswith("_"):
            continue
        builds = []
        for index, entry in enumerate(entries):
            where = f"{class_name} build {index + 1} ({entry.get('name') or entry.get('buildId', '')})"
            if "buildId" in entry:
                found = (fetch_live(hero_ids[class_name], entry["buildId"]) if class_name in hero_ids else None) \
                    or fetch_build(entry["buildId"])
                if found is None:
                    problems.append(f"{where}: build {entry['buildId']} not found in the API")
                    continue
                if found["hero_build"].get("hero_id") != hero_ids.get(class_name):
                    problems.append(f"{where}: build {entry['buildId']} is for hero {found['hero_build'].get('hero_id')}, not {class_name}")
                    continue
                build = convert(found, "custom", 0, 0, upgrades, abilities)
                if not build["items"]:
                    problems.append(f"{where}: build {entry['buildId']} has no usable items")
                if not build["abilities"]:
                    problems.append(f"{where}: build {entry['buildId']} has no ability order (the hero would get no ability ranks)")
                builds.append(build)
                continue
            categories = entry.get("categories") or [{"name": "Build", "optional": False, "items": entry.get("items") or []}]
            items = []
            for category in categories:
                for name in category["items"]:
                    if name not in by_class:
                        problems.append(f"{where}: unknown or unshopable item {name}")
                    elif name in BANNED_ITEMS:
                        problems.append(f"{where}: banned item {name}")
                    elif name not in items:
                        items.append(name)
            for step in entry.get("abilities") or []:
                if step.get("kind") not in ABILITY_KINDS or not str(step.get("ability", "")).startswith("ability_"):
                    problems.append(f"{where}: bad ability step {step}")
            if not entry.get("abilities"):
                problems.append(f"{where}: no ability order (the hero would get no ability ranks)")
            builds.append({
                "buildId": -(index + 1),
                "version": 0,
                "name": (entry.get("name") or f"Custom {index + 1}").strip(),
                "rank": "custom",
                "matches": 0,
                "wins": 0,
                "favorites": 0,
                "items": items,
                "categories": [
                    {"name": c.get("name", ""), "optional": bool(c.get("optional")), "items": [n for n in c["items"] if n in items]}
                    for c in categories
                ],
                "imbues": entry.get("imbues") or {},
                "sellPriority": entry.get("sellPriority") or {},
                "abilities": entry.get("abilities") or [],
            })
        custom[class_name] = builds

    if problems:
        raise SystemExit("custom-builds.json problems:\n  " + "\n  ".join(problems))
    return custom


def builds_for(hero, since, upgrades, abilities, custom):
    builds = list(custom.get(hero["class_name"], []))[:BUILDS_PER_HERO]
    seen = {b["buildId"] for b in builds}

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
    parser = argparse.ArgumentParser(description="Fetch hero builds into hero-builds.json.")
    parser.add_argument("--only", nargs="+", metavar="CLASS_NAME",
                        help="refresh only these heroes (e.g. hero_ratking); every other hero's builds stay as they are")
    args = parser.parse_args()

    now = datetime.now(timezone.utc)
    since = int(now.timestamp()) - WINDOW_DAYS * 86400

    upgrades, abilities = item_tables()
    by_class = {i["class_name"]: i for i in upgrades.values()}
    heroes = playable_heroes()
    custom = load_custom(by_class, upgrades, abilities, {h["class_name"]: h["id"] for h in heroes})
    kept = []
    previous = None
    if args.only:
        known = {h["class_name"] for h in heroes}
        missing = [name for name in args.only if name not in known]
        if missing:
            raise SystemExit(f"not a playable hero (add it to FORCE_HEROES?): {', '.join(missing)}")
        with open(OUTPUT, encoding="utf-8") as source:
            previous = json.load(source)
        kept = [h for h in previous["heroes"] if h["className"] not in args.only]
        heroes = [h for h in heroes if h["class_name"] in args.only]

    fresh = []
    for hero in heroes:
        builds = builds_for(hero, since, upgrades, abilities, custom)
        print(f"{hero['id']:>3} {hero['name']:<16} {len(builds)} build(s) " +
              ", ".join(f"{b['rank']}:{b['matches']}" for b in builds))
        if not builds:
            continue
        fresh.append({
            "id": hero["id"],
            "className": hero["class_name"],
            "name": hero["name"],
            "builds": builds,
        })

    heroes_out = sorted(kept + fresh, key=lambda h: h["id"])
    used = {item for h in heroes_out for b in h["builds"] for item in b["items"]}
    unknown = sorted(name for name in used if name not in by_class)
    if unknown:
        raise SystemExit(f"kept builds use items no longer in the shop: {', '.join(unknown)}; run without --only")

    components = {
        name: [c for c in by_class[name].get("component_items") or []]
        for name in sorted(used)
        if by_class[name].get("component_items")
    }

    item_costs = {name: by_class[name].get("cost") or 0 for name in sorted(used)}

    data = {
        "fetchedAt": previous["fetchedAt"] if previous else now.strftime("%Y-%m-%dT%H:%M:%SZ"),
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
