# fetch-builds.py

Downloads the top hero builds from the Deadlock API into
`Modules/Loadout/Data/hero-builds.json`, which is embedded in every DLL that
imports `Loadout.projitems`. Offline data step: plugins never query the API.

## Usage

```bash
python3 Bublock/scripts/fetch-builds.py
```

Then rebuild (`update.sh`) and deploy (`deploy.sh --confirm`, with approval).
Takes about 1–2 minutes (about 160 requests, 0.35 s apart; retries on HTTP 429).

## Behavior

1. `GET /v1/assets/heroes`: keeps heroes with `player_selectable`, not
   `in_development`, not `disabled`.
2. `GET /v1/assets/items`: shopable `upgrade` items (ID to `class_name`,
   `component_items`) and `ability` class names (for ability order and imbue
   targets).
3. Per hero, `GET /v1/analytics/hero-build-stats/{hero}` over the last
   `WINDOW_DAYS` (14) days, sorted by matches, then wins. The top builds are
   fetched with `GET /v1/builds?build_id=&only_latest=true` (rank `matches`).
4. If fewer than `BUILDS_PER_HERO` (3), fills from `/v1/builds?hero_id=`
   sorted by `weekly_favorites`, then `favorites` (rank = that sort).
5. Per build:
   - `items`: mod categories in order (left to right as in the game's build
     editor), unknown or unshopable IDs dropped, duplicates removed.
   - `categories`: the same items grouped by mod category, each with its
     `name` and `optional` flag (the API's `optional: true` option groups;
     `null` means required). The plugin takes every required item and one
     random item per optional group.
   - `BANNED_ITEMS` (Monster Rounds `upgrade_non_player_bonus`, Cultist
     Sacrifice `upgrade_non_player_bonus_sacrifice`, Golden Goose Egg
     `upgrade_goose_egg`, Trophy Collector `upgrade_trophy_collector`) are
     dropped from `items` and
     `categories`; the build is kept and later items fill the slot. The run
     prints how many entries were dropped.
   - `imbues`: item to target ability class name, when the build names one.
   - `abilities`: `ability_order` steps as `unlock` / `upgrade`; each
     ability keeps its first unlock and first 3 upgrades (some builds
     repeat the whole order).
   - Builds with no usable items are skipped.
6. `components`: `component_items` for every item used by any build.
7. `itemCosts`: the item's `cost` in souls (tier 1 800, 2 1600, 3 3200,
   4 6400, 5 9999) for every item used; drives the value baseline and cap.

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
      "abilities": [{"ability": "ability_...", "kind": "unlock"}] }] }],
  "components": {"upgrade_...": ["upgrade_..."]},
  "itemCosts": {"upgrade_...": 800}
}
```

## Side effects

- Overwrites `Modules/Loadout/Data/hero-builds.json`. No server access.

## Notes

- The old assets host `assets.deadlock-api.com` no longer resolves; assets
  are served from `api.deadlock-api.com/v1/assets/...`.
- Analytics endpoints share a 200 requests/minute per-IP limit.
- Hero IDs equal the Deadworks `Heroes` enum values; heroes missing from the
  enum are skipped at load time by `HeroBuildCatalog`.
