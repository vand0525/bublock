# MirrorPick

Pure pick for Mirror mode: one hero and one build index, chosen once and
shared by every fighter. No game calls; unit tested
(`Tests/RiftRoulette.Tests/MirrorPickTests`).

## Types

| Type | Meaning |
|---|---|
| `MirrorChoice(Hero, BuildIndex)` | The round's shared pair. `BuildIndex` is 0-based into `HeroBuildCatalog.BuildsFor(Hero)` |
| `MirrorPins(Hero, Slot)` | Admin pins. `Slot` is 1-based (what `/mirror_build` takes). `None` = nothing pinned |

## Operations

| Op | Behavior |
|---|---|
| `MirrorPins.PinHero(hero, buildCount)` | Pins the hero. Keeps the slot pin when `slot <= buildCount`, else drops it |
| `MirrorPins.TryPinSlot(slot, buildCount, out pinned)` | False (pins unchanged) with no hero pinned or a slot outside `1..buildCount`; else pins the slot |
| `MirrorPins.ClearSlot()` | Drops only the slot pin |
| `Resolve(pins, pool, buildCount, last, rng)` | Hero: the pinned hero when it has builds, else one random pool hero with builds, never `last` unless it is the only one. Build: the pinned slot when the hero is pinned and has that slot, else one random index. Null when no hero has builds |
| `Share(fighters, choice)` | Every fighter (deduplicated) mapped to the same `choice` |

## Invariants

- One `Resolve` call per assignment pass; every fighter gets that one
  result, so heroes and builds always match.
- A slot pin without a hero pin is never produced by `TryPinSlot`, and
  `Resolve` ignores one (the build is rolled).
- Deterministic for a given `Random` seed.
