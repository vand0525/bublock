#!/usr/bin/env python3
"""Extract ability charge data from the game's abilities.vdata into RiftRoulette/Round/Data/ability-charges.json.

Offline data step: the plugin never downloads anything. Rerun after a game patch,
then rebuild and deploy. See fetch-charges.py.md.
"""

import argparse
import json
import os
import re
import sys
import urllib.request
from datetime import datetime, timezone

REPO = "SteamTracking/GameTracking-Deadlock"
VDATA_PATH = "game/citadel/pak01_dir/scripts/abilities.vdata"
RAW = f"https://raw.githubusercontent.com/{REPO}/master/{VDATA_PATH}"
COMMITS = f"https://api.github.com/repos/{REPO}/commits?path={VDATA_PATH}&per_page=1"

HERE = os.path.dirname(os.path.abspath(__file__))
OUTPUT = os.path.join(HERE, "..", "RiftRoulette", "Round", "Data", "ability-charges.json")

# Banned from every build (fetch-builds.py BANNED_ITEMS); its BonusAbilityCharges is not a charge item.
IGNORED_ITEMS = {"upgrade_non_player_bonus_sacrifice"}

TOP = re.compile(r"^\t([a-z0-9_]+) = $")
VALUE = re.compile(r'm_strValue = "([^"]*)"')
PROPERTY = re.compile(r'm_strPropertyName = "([^"]*)"')
BONUS = re.compile(r'm_strBonus = "([^"]*)"')


def fetch(url):
    request = urllib.request.Request(url, headers={"User-Agent": "bublock-fetch-charges"})
    with urllib.request.urlopen(request, timeout=60) as response:
        return response.read().decode("utf-8", errors="replace")


def number(text):
    try:
        return int(float(text))
    except (TypeError, ValueError):
        return None


def blocks(lines):
    starts = [(m.group(1), i) for i, line in enumerate(lines) if (m := TOP.match(line))]
    starts.append(("", len(lines)))
    for (name, start), (_, end) in zip(starts, starts[1:]):
        yield name, lines[start:end]


def prop(body, name):
    header = f"\t\t\t{name} = "
    for j, line in enumerate(body):
        if line == header:
            for k in range(j + 1, min(j + 8, len(body))):
                if m := VALUE.search(body[k]):
                    return m.group(1)
    return None


def tier_charges(body):
    """Charge bonus of each m_vecAbilityUpgrades tier, in order."""
    try:
        start = body.index("\t\tm_vecAbilityUpgrades = ")
    except ValueError:
        return []
    tiers = []
    current = None
    pending = None
    for line in body[start + 2:]:
        if line == "\t\t]":
            break
        if line == "\t\t\t{":
            current = 0
            tiers.append(0)
            continue
        if m := PROPERTY.search(line):
            pending = m.group(1)
        elif (m := BONUS.search(line)) and pending == "AbilityCharges" and current is not None:
            tiers[-1] += number(m.group(1)) or 0
            pending = None
    return tiers


def parse(text):
    abilities = {}
    items = {}
    for name, body in blocks(text.split("\n")):
        if name.startswith("upgrade_"):
            if name in IGNORED_ITEMS:
                continue
            bonus = number(prop(body, "BonusAbilityCharges"))
            joined = "\n".join(body)
            if bonus and bonus > 0 and "IMBUE" not in joined and "m_bDisabled = true" not in joined:
                items[name] = bonus
            continue
        base = number(prop(body, "AbilityCharges")) or 0
        tiers = tier_charges(body)
        if base <= 0 and not any(tiers):
            continue
        tiers = (tiers + [0, 0, 0])[:3]
        abilities[name] = {
            "base": max(base, 0),
            "tiers": tiers,
            "conditional": number(prop(body, "AbilityChargesConditionally")) == 1,
        }
    return abilities, items


def latest_commit():
    try:
        data = json.loads(fetch(COMMITS))
        return data[0]["sha"][:12] if data else None
    except Exception as error:
        print(f"warning: could not read the GameTracking commit: {error}", file=sys.stderr)
        return None


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--vdata", help="read a local abilities.vdata instead of downloading it")
    args = parser.parse_args()

    if args.vdata:
        with open(args.vdata, encoding="utf-8", errors="replace") as handle:
            text = handle.read()
        commit = None
    else:
        text = fetch(RAW)
        commit = latest_commit()

    abilities, items = parse(text)
    if not abilities or not items:
        sys.exit(f"parse failed: {len(abilities)} abilities, {len(items)} items")

    output = {
        "fetchedAt": datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"),
        "source": f"https://github.com/{REPO}/blob/master/{VDATA_PATH}",
        "commit": commit,
        "abilities": dict(sorted(abilities.items())),
        "items": dict(sorted(items.items())),
    }
    with open(OUTPUT, "w", encoding="utf-8") as handle:
        json.dump(output, handle, indent=2)
        handle.write("\n")

    conditional = sorted(name for name, entry in abilities.items() if entry["conditional"])
    print(f"Wrote {len(abilities)} abilities, {len(items)} items to {os.path.relpath(OUTPUT)}")
    print(f"Items: {', '.join(f'{k} +{v}' for k, v in sorted(items.items()))}")
    print(f"Conditional (resource pool): {', '.join(conditional)}")


if __name__ == "__main__":
    main()
