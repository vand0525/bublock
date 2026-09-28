# fetch-deadworks.sh

Downloads the Deadworks API the plugins compile against, from a pinned
Deadworks GitHub release, into `lib/` inside the repo (git-ignored). Used by
CI and by any checkout without the `deadworks/` workspace (`../lib`,
`../Directory.Build.props`).

## Usage

```bash
./scripts/fetch-deadworks.sh            # pinned default (v0.4.18)
./scripts/fetch-deadworks.sh v0.4.19    # or DEADWORKS_VERSION=v0.4.19
```

## Behavior

- Downloads `deadworks-<version>.zip` from
  `github.com/Deadworks-net/deadworks/releases` and extracts
  `DeadworksManaged.Api.dll`, `Google.Protobuf.dll` and
  `DeadworksManaged.Api.xml` (API doc comments) from
  `game/bin/win64/managed/`.
- Destination: `$DeadworksLibDir` if set, else `<repo>/lib/`.
- Writes `<lib>/.version`; a rerun with the same version does nothing.

## Pinning

The version must match the Deadworks the server runs: enum values
(`Heroes`, `EModifierState`, ...) are compiled into our DLLs
(`reference/patch-day.md` step 3). After a Deadworks update, bump the default
here and `DEADWORKS_VERSION` in `.github/workflows/*.yml` together. The
server's own copy (`/server/game/bin/win64/managed/` over SFTP) is the
authority when in doubt.

## Requirements

`curl`, `python3`. Network. No server access.
