# docs-site.py

Builds the docs webpage, `site/index.html` (git-ignored): every markdown doc
in the repo plus `.rules`, the knowledge-graph view and search, in one
self-contained page.

## Usage

```bash
python3 scripts/knowledge-graph.py && python3 scripts/docs-site.py   # or scripts/knowledge.sh
```

Open `site/index.html` in a browser (needs network for fonts, marked and d3
from cdnjs).

## Behavior

- Bundles every `.md` except `cvarlist.md` (500 KB mirror) and folders
  `bin`, `obj`, `lib`, `logs`, `site`, `baseline`, `maps`, plus
  `RiftRoulette/reference/.rules`, with `knowledge/generated/graph.json`, as
  one JSON blob in the page.
- The sidebar comes from `NAV` in the script, plus every `FEATURE.md`.
- Links between docs, and backticked repo paths, become in-page links; a
  `.cs` path opens its sibling `.md`.
- Effects-catalog status cells render as chips (verified, untested, avoid).
- Graph view: d3 force layout, filters by category (structure, code,
  commands, game dependencies, stages and game modes, docs, scripts), node search, a detail
  panel with each node's links and an "Open doc" button. Filter choices are
  remembered per browser (`localStorage`, optional).

## Guard

Refuses to build (exit 1, value not printed) if any bundled doc contains a
non-empty `DW_*` value from `scripts/server.env` or any IPv4 address other
than the README's public join address. The page can be shared further than
the repo.
