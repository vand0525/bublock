# CI / CD

| Workflow | Runs on | Does | Server access |
|---|---|---|---|
| `ci.yml` | every push to `main`, every PR, manual | `fetch-deadworks.sh`, `update.sh` (build), `test.sh`; uploads the plugin DLLs as an artifact; on `main`, a failure opens or updates a `CI failing on main` issue and the next green run closes it | none |
| `release.yml` | a pushed tag `vX.Y.Z` | build, test, then a GitHub Release with `GunGame.dll`, `RiftRoulette.dll`, `DevTools.dll`, `CleanSlate.dll` and generated notes | none |
| `deploy.yml` | manual only (Actions → Deploy → Run workflow), `main` only | tests, then `scripts/deploy.sh --confirm` with the chosen plugin set (inputs `plugins`, default `GunGame DevTools CleanSlate`, and `parked`, default `RiftRoulette`); keeps `server-backups/<stamp>/` as an artifact for 90 days | SFTP |

Deploy is manual on purpose: `.rules` §0.2 says nothing reaches the live
server without an explicit approval per upload. Pressing "Run workflow" is
that approval. To also require a second person, add required reviewers to
the `production` environment (Settings → Environments).

## Secrets (environment `production`)

Never in committed files: the repo is public.

| Secret | Value |
|---|---|
| `DW_HOST` | SFTP host (same as `scripts/server.env`) |
| `DW_PORT` | SFTP port (not the game port) |
| `DW_USER` | SFTP user |
| `DW_PASSWORD` | SFTP password |
| `DW_KNOWN_HOSTS` | pinned host key line(s): `ssh-keyscan -p <port> <host>`, checked against the fingerprint in `scripts/server.env` |

With the GitHub CLI, from the repo root:

```bash
source scripts/server.env
gh api -X PUT "repos/{owner}/{repo}/environments/production" >/dev/null
gh secret set DW_HOST  --env production --body "$DW_HOST"
gh secret set DW_PORT  --env production --body "$DW_PORT"
gh secret set DW_USER  --env production --body "$DW_USER"
gh secret set DW_PASSWORD --env production        # prompts; never pass it with --body
ssh-keyscan -p "$DW_PORT" "$DW_HOST" | gh secret set DW_KNOWN_HOSTS --env production
```

## Rollback

Download the run's `server-backups-<run id>` artifact into
`../server-backups/` (so it holds `<stamp>/*.dll`), then locally
`scripts/rollback.sh <stamp>`.

## Deadworks version

`DEADWORKS_VERSION` in both workflows and the default in
`scripts/fetch-deadworks.sh` must match the Deadworks the server runs
(`reference/patch-day.md`). Bump all three together.

## Releases (patch style)

On `main`, after merging: `git tag v0.2.0 && git push origin v0.2.0`. The
Release workflow publishes the DLLs; copy the matching
`knowledge/changelog.md` section into the release text for the community.

## Issues

Issue forms in `.github/ISSUE_TEMPLATE/` (bug, feature idea, playtest
feedback) apply labels on creation. Labels: `type:`, `area:`, `P0`-`P3`,
`status:` (`needs-playtest`, `needs-theo`, `blocked`, `ready`).

