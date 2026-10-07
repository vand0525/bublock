# fetch-builds.py

Downloads the top hero builds from the Deadlock API into
`Modules/Loadout/Data/hero-builds.json`, which is embedded in every DLL that
imports `Loadout.projitems`. Offline data step: plugins never query the API.

## Usage

```bash
python3 Bublock/scripts/fetch-builds.py                     # every hero
python3 Bublock/scripts/fetch-builds.py --only hero_ratking # one hero; the rest stay as they are
```

Then rebuild (`update.sh`) and deploy (`deploy.sh --confirm`, with approval).
A full run takes about 1–2 minutes (about 160 requests, 0.35 s apart;
retries on HTTP 429).

## Behavior

1. `GET /v1/assets/heroes`: keeps heroes not `disabled` that are
   `player_selectable` and not `in_development`, or are in `FORCE_HEROES`
   (`hero_ratking`, `hero_baba`: the API's assets still mark them
   unplayable or in development after their patch).
2. `GET /v1/assets/items`: shopable `upgrade` items (ID to `class_name`,
   `component_items`) and `ability` class names (for ability order and imbue
   targets). `EXTRA_ABILITIES` (Rat King's four signatures, from the game's
   `heroes.vdata` in GameTracking-Deadlock) are added with IDs from
   `ability_id` (MurmurHash2 of the class name, seed `0x31415926`; matches
   every ID the API lists), so a published Rat King build keeps its ability
   order.
3. `Modules/Loadout/Data/custom-builds.json` (`load_custom`): per hero class
   name, a list of builds that go first (rank `custom`), before the API's.
   Keys starting with `_` are ignored. Each entry is either
   `{"buildId": N}` (a published build, fetched live from Steam with
   `GET /v1/builds/{hero}/{build}` because the search index lags new builds
   by an hour or more, else from the search; refused when it is for another
   hero) or a hand-made build: `name`, `items` (build order;
   or `categories` like the output), optional `imbues`, `sellPriority`, and
   `abilities` (steps as in the output). Hand-made builds get `buildId` -1,
   -2, ... Any unknown or banned item, bad ability step, missing ability
   order or unknown build stops the run with the list of problems.
4. `--only`: loads the current `hero-builds.json`, refreshes only the named
   heroes (keeping `fetchedAt`), and stops if a kept build uses an item no
   longer in the shop.
5. Per hero, after its custom builds (at most `BUILDS_PER_HERO` in all),
   `GET /v1/analytics/hero-build-stats/{hero}` over the last
   `WINDOW_DAYS` (14) days, sorted by matches, then wins. The top builds are
   fetched with `GET /v1/builds?build_id=&only_latest=true` (rank `matches`).
6. If fewer than `BUILDS_PER_HERO` (3), fills from `/v1/builds?hero_id=`
   sorted by `weekly_favorites`, then `favorites` (rank = that sort).
7. Per build:
   - `items`: mod categories in order (left to right as in the game's build
     editor), unknown or unshopable IDs dropped, duplicates removed.
   - `categories`: the same items grouped by mod category, each with its
     `name` and `optional` flag (the API's `optional: true` option groups;
     `null` means required). The plugin takes every required item and one
     random item per optional group.
   - `BANNED_ITEMS` (Monster Rounds `upgrade_non_player_bonus`, Cultist
     Sacrifice `upgrade_non_player_bonus_sacrifice`, Golden Goose Egg
     `upgrade_goose_egg`, Trophy Collector `upgrade_trophy_collector`,
     Healing Rite `upgrade_health_stimpak`) are dropped from `items` and
     `categories`; the build is kept and later items fill the slot. The run
     prints how many entries were dropped.
   - `imbues`: item to target ability class name, when the build names one.
   - `sellPriority`: item to the mod's `sell_priority`, only values above 0
     (most builds leave every item at 0; higher sells first). Empty `{}`
     when the build marks none.
   - `abilities`: `ability_order` steps as `unlock` / `upgrade`; each
     ability keeps its first unlock and first 3 upgrades (some builds
     repeat the whole order).
   - Builds with no usable items are skipped.
8. `components`: `component_items` for every item used by any build.
9. `itemCosts`: the item's `cost` in souls (tier 1 800, 2 1600, 3 3200,
   4 6400, 5 9999) for every item used; drives the budget planner and baseline.

## Output

```json
{
  "fetchedAt": "2026-09-27T02:08:19Z", "source": "...", "windowDays": 14,
  "heroes": [{ "id": 13, "className": "hero_haze", "name": "Haze",
    "builds": [{ "buildId": 0, "version": 0, "name": "", "rank": "matches",
      "matches": 0, "wins": 0, "favorites": 0,
      "items": ["upgrade_..."],
      "categories": [{"name": "Early Options", "optional": true, "items": ["upgrade_..."]}],
      "imbues": {"upgrade_...": "ability_..."},
      "sellPriority": {"upgrade_...": 100},
      "abilities": [{"ability": "ability_...", "kind": "unlock"}] }] }],
  "components": {"upgrade_...": ["upgrade_..."]},
  "itemCosts": {"upgrade_...": 800}
}
```

## Side effects

- Overwrites `Modules/Loadout/Data/hero-builds.json`. Reads (never writes)
  `custom-builds.json`, which is not embedded in the DLL. No server access.

## Notes

- The old assets host `assets.deadlock-api.com` no longer resolves; assets
  are served from `api.deadlock-api.com/v1/assets/...`.
- Analytics endpoints share a 200 requests/minute per-IP limit.
- Hero IDs equal the Deadworks `Heroes` enum values; heroes missing from the
  enum are skipped at load time by `HeroBuildCatalog`.
