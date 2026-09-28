# knowledge-graph.py

Builds the repo's knowledge graph and the two docs derived from it. Offline,
read-only on the source.

## Usage

```bash
python3 scripts/knowledge-graph.py
```

## Outputs (`knowledge/generated/`)

- `graph.json`: node-link JSON (`nodes`, `links`; loads in d3,
  `networkx.node_link_graph`, and most graph tools).
- `tree.md`: plugin / module → feature → file tree with each file's doc
  summary, commands and game dependencies.
- `indexes.md`: commands, game dependencies, stages, game modes, plugins (role, modules), types and docs, each with the
  files that use them.

## Nodes

| Kind | From |
|---|---|
| `plugin`, `test-project` | `*.csproj` (under `Tests/` = test project); plugins get `role` = `game type` when their code overrides `OnClientFullConnect` (they admit players), else `tool` |
| `module` | folders holding a `.projitems` (`Shared`, `Modules/*`) |
| `feature` | other folders with a `FEATURE.md` |
| `source`, `test` | `*.cs`; summary = first sentence of the sibling `.md` |
| `command` | `[Command("name", Description = ...)]`; `admin` when the method calls `AdminCommand.Authorize` |
| `hook`, `event` | `override ... On*(`, `[GameEventHandler("...")]` |
| `convar` | string literals found in `reference/cvarlist.md` (with an underscore, or `maxplayers`), minus the file's own command names |
| `entity` | literals in the `dl_midtown` entity dump, or with a known designer prefix (`npc_`, `citadel_item_`, ...), or `observer` |
| `modifier`, `ability`, `schema-field` | literals `modifier_*`, `citadel_ability_*`, `m_*` |
| `modifier-state`, `net-message` | `EModifierState.X`, `CCitadelUserMsg_*` / `CUserMessage*` |
| `script`, `workflow` | `scripts/*.sh|py|cs`, `.github/workflows/*.yml` |
| `doc`, `knowledge` | every other `.md` (colocated docs attach to their file node instead) |
| `stage` | each `###` / `####` entry under "Staged roadmap" and "Game types" in `reference/master-plan.md` (ids `stage:13j`, `stage:g1`); `status` = `done` (`- [x]`), `awaiting playtest` (`- [ ]` and uploaded), else `open`; summary = its **Goal** |
| `game-mode` | each `###` recipe in `knowledge/game-mode-recipes.md`; `status` from its `##` section: `shipped`, `in progress`, `designed` |

## Links

`contains` (folder → child), `imports` (csproj → module), `uses` (file →
file whose type it names), `tests` (`FooTests` → `Foo`), `registers`,
`hooks`, `listens`, `depends-on`, `sends`, `refers-to` (script → script it
names outside comments), `documents` (module → its `.projitems` doc), `links`
(markdown links, `related:` front matter, and backticked repo paths),
`feature-uses` (folder → folder, rolled up from `uses` between their files,
with a `weight`), `touches` (stage → every backticked name in its entry that
resolves to a node), `mode-uses` (game mode → the same, for its recipe) and
`planned-in` (game mode → a stage it names as `Stage 13k`).

Backtick resolution (`resolve_ref`): `/command`, a repo path or folder
(also under `RiftRoulette/`, with or without `.cs`), `EModifierState.X`, a
hook, event, convar, entity, modifier, schema field, ability or net message
name, then a C# type name (its declaring file).

`templates/` is skipped (placeholders, not code).

## Limits

Pattern-based, not a compiler: a type named only through `var` or a
dependency built from string concatenation is missed. The command catalogs
and `patch-day.md` §5 stay the authority.
