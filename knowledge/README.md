---
id: knowledge-index
type: index
tags: [start-here, context]
related:
  - glossary.md
  - mental-models/how-deadworks-mods-work.md
  - mental-models/talking-to-the-game.md
  - mental-models/rift-roulette-architecture.md
  - mental-models/ship-and-operate.md
  - effects-catalog.md
  - game-mode-recipes.md
  - changelog.md
---

# Bublock knowledge base

What we know about modding Deadlock through Deadworks, written so a person or
an agent can load the right context in one pass. Deadlock has no official
modding API. Everything here comes from the Deadworks framework plus what the
game did when we tried it on a server.

Theo's working docs stay where they are and remain the source of truth for
their topics: `RiftRoulette/reference/.rules` (rules), `master-plan.md`
(stages and progress log), `resources.md` (verified discoveries),
`patch-day.md` (dependency tables), `FEATURE.md` in every feature folder, and
a `Foo.md` beside every `Foo.cs`. This folder links to them instead of
copying them.

## Load order (the context window)

Read top to bottom until you have enough for the task.

| # | Note | Answers |
|---|---|---|
| 1 | [glossary.md](glossary.md) | What does "up top", "cash-in", "bench", "clean mode" mean? |
| 2 | [how-deadworks-mods-work.md](mental-models/how-deadworks-mods-work.md) | How does our code get into the game at all? |
| 3 | [talking-to-the-game.md](mental-models/talking-to-the-game.md) | With no API, which channels can we use to read and change the game? |
| 4 | [rift-roulette-architecture.md](mental-models/rift-roulette-architecture.md) | How is the game mode put together, and where does new code go? |
| 5 | [effects-catalog.md](effects-catalog.md) | Which effects are proven, which are untested, and which to avoid? |
| 6 | [game-mode-recipes.md](game-mode-recipes.md) | How do those effects combine into new modes? |
| 7 | [ship-and-operate.md](mental-models/ship-and-operate.md) | Build, CI, deploy, logs, patch day, server layout |
| 8 | [changelog.md](changelog.md) | What changed in this fork, and when |

## Generated references (rebuild, never hand-edit)

| File | Built by | Contents |
|---|---|---|
| [generated/deadworks-api.md](generated/deadworks-api.md) | `scripts/api-index.cs` | Every Deadworks hook, game event, enum value (304 modifier states, 60 heroes), type and net message |
| [generated/indexes.md](generated/indexes.md) | `scripts/knowledge-graph.py` | Commands, game dependencies, hooks and events: each with the file that uses it |
| [generated/tree.md](generated/tree.md) | `scripts/knowledge-graph.py` | Plugin → feature → file tree with commands and dependencies |
| `generated/graph.json` | `scripts/knowledge-graph.py` | The knowledge graph (node-link JSON): plugins, modules, features, files, commands, game dependencies, docs |
| `site/index.html` (git-ignored) | `scripts/docs-site.py` | The docs webpage: all of the above, searchable, with the interactive graph |

Rebuild everything with `scripts/knowledge.sh`.

## Conventions for notes here

- One topic per file, with frontmatter `id`, `type`, `tags` and `related`.
  The graph builder turns `related` entries and markdown links into edges,
  so link generously and with relative paths.
- Mark every claim about game behavior as **verified** (seen on a server;
  cite the file or doc), **untested** (exists in the API or cvar list, not
  tried), or **avoid** (tried and it broke something).
- Server addresses, users and passwords never go here (the repo is public).
  They live in git-ignored `scripts/server.env`.
