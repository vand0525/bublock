# Bublock (redock fork)

Theo (GitHub `vand0525`) built this repo. His development rules are the source
of truth and are loaded here; follow them over general defaults. Paths in them
start with `Bublock/`, which is this repo's root.

@RiftRoulette/reference/.rules

## Context

- Start with `knowledge/README.md`: glossary, mental models, effects catalog
  (verified / untested / avoid), game-mode recipes, generated indexes.
- Before using any Deadworks API member, grep `knowledge/generated/deadworks-api.md`.
- Before touching a feature, read its `FEATURE.md` and the file's sibling `.md`.

## This fork

- Game types: Rift Roulette is one game type, `Shared/` + `Modules/` are the
  engine. New game types are their own DLLs (`scripts/new-game-type.sh`),
  never modes inside another game type. The server runs one at a time
  (`DW_PLUGINS` / `DW_PARKED`); restart after parking one.
- The owner's server is an experimental debug and game-mode planning server:
  try things, use Debug-mode admin commands, and write findings back into
  `knowledge/`. `scripts/server-check.sh` is the read-only first look.
- Server details (SFTP host, port, user, password, game address) live only in
  git-ignored `scripts/server.env`. Never commit or publish them, and never
  echo the SFTP user or password; the repo is public. `scripts/docs-site.py`
  refuses to build if a doc holds one.
- Build here: `.NET 10` is in `~/.dotnet` (add it to `PATH`), the Deadworks
  API comes from `scripts/fetch-deadworks.sh` into git-ignored `lib/`, then
  `scripts/update.sh` and `scripts/test.sh`.
- Deploy only with the owner's explicit approval for that upload:
  `scripts/deploy.sh --confirm` locally, or the manual Deploy workflow.
- After a change: `scripts/knowledge.sh` (regenerates the API index, graph,
  tree, indexes and `site/index.html`) and a dated entry in
  `knowledge/changelog.md`.
