# update.sh

Build-all entrypoint for Bublock plugins.

## Usage

```bash
./Bublock/scripts/update.sh
```

Runs `dotnet build Bublock.sln -c Release`.

## Deploy policy

This script is **build-only**.

- It does **not** SFTP/push DLLs to the Deadworks server.
- `--deploy` and `DeployPlugins=true` are **rejected** with a pointer to
  `deploy.sh --confirm`, which backs up the server DLLs before uploading.
- The MSBuild `DeployToDeadworks` target in `Directory.Build.Targets` is not
  used (it uploads without a backup).

## Requirements

- .NET 10 SDK
- Workspace `lib/DeadworksManaged.Api.dll` and `lib/Google.Protobuf.dll`

## What it builds

Every project in `Bublock/Bublock.sln` (`RiftRoulette`, `DevTools`,
`CleanSlate`, and the test projects).
