#!/usr/bin/env python3
"""Detect what a Deadlock / Deadworks update broke for Bublock, offline.

  patch-check.py --save-baseline   save a known-good snapshot (before a patch)
  patch-check.py [--baseline DIR]  diff fresh data against the newest snapshot

Only reports what our code uses: convars and entity names found in the C#
source, heroes / items / abilities in hero-builds.json, and API members and
enum values our source mentions. Exits 1 on any HIT. See patch-check.py.md.
"""

import argparse
import hashlib
import json
import os
import re
import shutil
import subprocess
import sys
import urllib.request
from datetime import datetime, timezone

HERE = os.path.dirname(os.path.abspath(__file__))
BUBLOCK = os.path.normpath(os.path.join(HERE, ".."))
WORKSPACE = os.path.normpath(os.path.join(BUBLOCK, ".."))
REFERENCE = os.path.join(BUBLOCK, "RiftRoulette", "reference")
BASELINES = os.path.join(REFERENCE, "baseline")
LIB = os.path.join(WORKSPACE, "lib", "DeadworksManaged.Api.dll")
BUILDS = os.path.join(BUBLOCK, "Modules", "Loadout", "Data", "hero-builds.json")
MAP_ENTITIES = os.path.join(REFERENCE, "maps", "dl_midtown", "entities.json")

API = "https://api.deadlock-api.com"
CVARLIST = "https://raw.githubusercontent.com/Mikooboy/deadlock-cvar-list/main/cvarlist.md"
ILSPY = os.path.expanduser("~/.dotnet/tools/ilspycmd")

SOURCE_DIRS = ["Shared", "Modules", "RiftRoulette", "DevTools", "CleanSlate"]

# Placed in the map (not spawned at runtime), so the map dump can confirm them.
MAP_PLACED = [
    "info_koth_spawn_location",
    "info_super_trooper_spawn",
    "item_crate_spawn",
    "trigger_item_shop",
    "trigger_item_shop_safe_zone",
    "citadel_shop_prop_dynamic",
    "npc_boss_tier2",
    "npc_barrack_boss",
    "npc_trooper_boss",
    "citadel_item_powerup_spawner",
    "citadel_herotest_orbspawner",
]

hits = []
notes = []


def hit(area, message):
    hits.append(f"HIT   [{area}] {message}")


def note(area, message):
    notes.append(f"NOTE  [{area}] {message}")


# ---------------------------------------------------------------- source scan


def source_files():
    for folder in SOURCE_DIRS:
        for root, dirs, files in os.walk(os.path.join(BUBLOCK, folder)):
            dirs[:] = [d for d in dirs if d not in ("bin", "obj", "Tests", "reference", "logs")]
            for name in files:
                if name.endswith(".cs"):
                    yield os.path.join(root, name)


def scan_source():
    identifiers, convars, entity_names = set(), set(), set()
    for path in source_files():
        text = open(path, encoding="utf-8").read()
        identifiers.update(re.findall(r"[A-Za-z_]\w*", text))
        convars.update(re.findall(r'ConVar\.Find\("([^"]+)"\)', text))
        convars.update(m.split()[0] for m in re.findall(r'ExecuteCommand\(\$?"([a-z_]+)[ "]', text))
        entity_names.update(re.findall(r'ByDesignerName\("([^"]+)"\)', text))
        entity_names.update(re.findall(r'"((?:npc|citadel|info|item|trigger|point)_[a-z0-9_]+)"', text))
    convars.discard("kickid")
    return identifiers, convars, entity_names


def lobby_heroes():
    names = set()
    for rel in ["RiftRoulette/Lobby/LobbyService.cs"]:
        names.update(re.findall(r"\bHeroes\.(\w+)\b(?!\s*[(<])", open(os.path.join(BUBLOCK, rel), encoding="utf-8").read()))
    return names


def banned_items():
    text = open(os.path.join(BUBLOCK, "Modules/Loadout/LoadoutPlanner.cs"), encoding="utf-8").read()
    return set(re.findall(r'"(upgrade_[a-z0-9_]+)"', text))


def build_names():
    data = json.load(open(BUILDS, encoding="utf-8"))
    heroes = {h["id"]: h for h in data["heroes"]}
    items, abilities = set(), set()
    for hero in data["heroes"]:
        for build in hero.get("builds") or []:
            items.update(build.get("items") or [])
            for category in build.get("categories") or []:
                items.update(category.get("items") or [])
            for item, ability in (build.get("imbues") or {}).items():
                items.add(item)
                abilities.add(ability)
            abilities.update(step["ability"] for step in build.get("abilities") or [])
    for item, parts in (data.get("components") or {}).items():
        items.add(item)
        items.update(parts)
    return data.get("fetchedAt"), heroes, items, abilities


# ---------------------------------------------------------------- fresh data


def get_json(url):
    request = urllib.request.Request(url, headers={"User-Agent": "Bublock-patch-check"})
    with urllib.request.urlopen(request, timeout=60) as response:
        return json.load(response)


def get_text(url):
    request = urllib.request.Request(url, headers={"User-Agent": "Bublock-patch-check"})
    with urllib.request.urlopen(request, timeout=60) as response:
        return response.read().decode("utf-8")


def fetch_heroes():
    return {
        str(h["id"]): {
            "class_name": h.get("class_name"),
            "name": h.get("name"),
            "playable": bool(h.get("player_selectable") and not h.get("in_development") and not h.get("disabled")),
        }
        for h in get_json(f"{API}/v1/assets/heroes")
    }


def fetch_items():
    return {
        i["class_name"]: {"type": i.get("type"), "shopable": bool(i.get("shopable"))}
        for i in get_json(f"{API}/v1/assets/items")
        if i.get("class_name")
    }


def fetch_cvars():
    cvars = {}
    for line in get_text(CVARLIST).splitlines():
        parts = [p.strip() for p in line.split("|")]
        if len(parts) >= 2 and parts[0] and not parts[0].startswith(("Name", "----")):
            cvars[parts[0]] = parts[1]
    return cvars


def api_surface():
    """Public types, members and enum values of the Deadworks API (needs ilspycmd)."""
    if not os.path.exists(ILSPY):
        return None
    source = subprocess.run([ILSPY, LIB], capture_output=True, text=True, check=True).stdout
    types, enums = {}, {}
    current, enum_name, enum_next = None, None, 0
    type_decl = re.compile(r"^\s*public\s+(?:[\w]+\s+)*(class|struct|enum|interface|record)\s+(\w+)")
    enum_member = re.compile(r"^\s*(\w+)\s*(?:=\s*([^,]+?))?\s*,?\s*$")
    for raw in source.splitlines():
        line = raw.strip()
        declared = type_decl.match(raw)
        if declared:
            current = declared.group(2)
            types.setdefault(current, [])
            if declared.group(1) == "enum":
                enum_name, enum_next = current, 0
                enums[enum_name] = {}
            else:
                enum_name = None
            continue
        if enum_name:
            if line == "}":
                enum_name = None
                continue
            member = enum_member.match(line)
            if member and line not in ("{",) and not line.startswith("["):
                name, value = member.group(1), member.group(2)
                if value is None:
                    value = str(enum_next)
                try:
                    enum_next = int(value, 0) + 1
                except ValueError:
                    enum_next = 0
                enums[enum_name][name] = value
            continue
        if current and line.startswith(("public ", "protected ")) and not type_decl.match(raw):
            types[current].append(re.sub(r"\s+", " ", line.split("{")[0].rstrip(";").strip()))
    return {"types": types, "enums": enums}


def map_counts():
    if not os.path.exists(MAP_ENTITIES):
        return None
    counts = {name: 0 for name in MAP_PLACED}
    for entity in json.load(open(MAP_ENTITIES, encoding="utf-8"))["entities"]:
        name = entity.get("classname")
        if name in counts:
            counts[name] += 1
    return counts


def sha256(path):
    return hashlib.sha256(open(path, "rb").read()).hexdigest() if os.path.exists(path) else None


def snapshot():
    fetched_at, _, _, _ = build_names()
    snap = {
        "meta": {
            "savedAt": datetime.now(timezone.utc).isoformat(timespec="seconds"),
            "libSha256": sha256(LIB),
            "heroBuildsFetchedAt": fetched_at,
        },
        "heroes": fetch_heroes(),
        "items": fetch_items(),
        "cvars": fetch_cvars(),
        "mapCounts": map_counts(),
    }
    api = api_surface()
    if api is None:
        note("API", f"ilspycmd not found at {ILSPY}; API surface skipped (dotnet tool install -g ilspycmd)")
    snap["api"] = api
    return snap


# ---------------------------------------------------------------- compare


def to_int(value):
    try:
        return int(value, 0)
    except (TypeError, ValueError):
        return None


def member_name(signature):
    before = signature.split("(")[0].split("=")[0].strip()
    return before.split()[-1] if before.split() else ""


def compare(base, fresh):
    identifiers, convars, entity_names = scan_source()
    fetched_at, build_heroes, items, abilities = build_names()

    # Convars
    for name in sorted(convars):
        was, now = name in base["cvars"], name in fresh["cvars"]
        if was and not now:
            hit("Convar", f"{name} is gone from the cvar list (upstream list may lag a patch)")
        elif was and base["cvars"][name] != fresh["cvars"][name]:
            note("Convar", f"{name} flags changed: {base['cvars'][name]} -> {fresh['cvars'][name]}")
        elif not was:
            note("Convar", f"{name} was never in the cvar list; check it in game (dw_selftest_run)")

    # Heroes
    enums = (fresh.get("api") or base.get("api") or {}).get("enums", {})
    hero_enum = {name: to_int(value) for name, value in enums.get("Heroes", {}).items()}
    enum_ids = {value: name for name, value in hero_enum.items() if value is not None}
    for hero_id, hero in sorted(build_heroes.items()):
        now = fresh["heroes"].get(str(hero_id))
        if not now or not now["playable"]:
            hit("Hero", f"{hero['name']} (id {hero_id}) is no longer playable; random mode would still hand it out")
        elif now["class_name"] != hero.get("className"):
            hit("Hero", f"{hero['name']} (id {hero_id}) class {hero.get('className')} -> {now['class_name']}")
    for hero_id, hero in sorted(fresh["heroes"].items(), key=lambda kv: int(kv[0])):
        if hero["playable"] and int(hero_id) not in build_heroes:
            note("Hero", f"new playable hero {hero['name']} (id {hero_id}); rerun fetch-builds.py")
            if enum_ids and int(hero_id) not in enum_ids:
                hit("Hero", f"{hero['name']} (id {hero_id}) is not in the Heroes enum; update lib/ and rebuild")
    if hero_enum:
        for name in sorted(lobby_heroes()):
            if name not in hero_enum:
                hit("Hero", f"Heroes.{name} (used in Lobby) is not in the enum")
            else:
                hero = fresh["heroes"].get(str(hero_enum[name]))
                if hero is None:
                    hit("Hero", f"Heroes.{name} has no hero in the heroes asset")
                elif name == "Skyrunner":
                    note("Hero", f"lobby hero Skyrunner is {hero['class_name']} playable={hero['playable']}")

    # Items and abilities
    for name in sorted(items | banned_items()):
        if name not in fresh["items"]:
            hit("Item", f"{name} is gone from the items asset (hero-builds.json fetched {fetched_at})")
    for name in sorted(abilities):
        if name not in fresh["items"]:
            hit("Ability", f"{name} is gone from the items asset")

    # Map dump counts
    if base.get("mapCounts") and fresh.get("mapCounts"):
        for name in MAP_PLACED:
            if base["mapCounts"].get(name) != fresh["mapCounts"].get(name):
                (hit if name in entity_names else note)(
                    "Map", f"{name} count {base['mapCounts'].get(name)} -> {fresh['mapCounts'].get(name)}")

    # API surface
    if base.get("api") and fresh.get("api"):
        for type_name, members in base["api"]["types"].items():
            if type_name not in identifiers:
                continue
            if type_name not in fresh["api"]["types"]:
                hit("API", f"type {type_name} is gone")
                continue
            now = set(fresh["api"]["types"][type_name])
            for signature in members:
                if signature not in now and member_name(signature) in identifiers:
                    hit("API", f"{type_name}: {signature} changed or removed")
        for enum_name, values in base["api"]["enums"].items():
            if enum_name not in identifiers:
                continue
            now = fresh["api"]["enums"].get(enum_name, {})
            for name, value in values.items():
                if name in identifiers and now.get(name) != value:
                    hit("Enum", f"{enum_name}.{name} {value} -> {now.get(name, 'removed')}")
    elif base["meta"].get("libSha256") != fresh["meta"].get("libSha256"):
        note("API", "lib/DeadworksManaged.Api.dll changed but no API surface to compare (install ilspycmd)")


# ---------------------------------------------------------------- main


def newest_baseline():
    if not os.path.isdir(BASELINES):
        return None
    dirs = sorted(d for d in os.listdir(BASELINES) if os.path.isfile(os.path.join(BASELINES, d, "snapshot.json")))
    return os.path.join(BASELINES, dirs[-1]) if dirs else None


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--save-baseline", action="store_true", help="save a snapshot to reference/baseline/<UTC date>/")
    parser.add_argument("--baseline", help="baseline folder to compare against (default: newest)")
    args = parser.parse_args()

    fresh = snapshot()

    if args.save_baseline:
        folder = os.path.join(BASELINES, datetime.now(timezone.utc).strftime("%Y%m%d"))
        os.makedirs(folder, exist_ok=True)
        with open(os.path.join(folder, "snapshot.json"), "w", encoding="utf-8") as out:
            json.dump(fresh, out, indent=1, sort_keys=True)
        shutil.copyfile(BUILDS, os.path.join(folder, "hero-builds.json"))
        api = fresh.get("api") or {"types": {}, "enums": {}}
        print(f"Baseline saved to {os.path.relpath(folder, BUBLOCK)}: "
              f"{len(fresh['heroes'])} heroes, {len(fresh['items'])} items, {len(fresh['cvars'])} convars, "
              f"{len(api['types'])} API types, {len(api['enums'])} enums")
        for line in notes:
            print(line)
        return 0

    folder = args.baseline or newest_baseline()
    if not folder:
        print("No baseline found. Run scripts/patch-baseline.sh before the patch; comparing against nothing.")
        base = {"meta": {}, "heroes": {}, "items": {}, "cvars": {}, "mapCounts": None, "api": None}
    else:
        base = json.load(open(os.path.join(folder, "snapshot.json"), encoding="utf-8"))
        print(f"Baseline: {os.path.relpath(folder, BUBLOCK)} (saved {base['meta'].get('savedAt')})")

    compare(base, fresh)

    for line in notes + hits:
        print(line)
    print(f"{len(hits)} hit(s), {len(notes)} note(s)")
    return 1 if hits else 0


if __name__ == "__main__":
    sys.exit(main())
