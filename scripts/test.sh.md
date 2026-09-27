# test.sh

Runs the local test projects with `dotnet test` in Release.

## Usage

```bash
./Bublock/scripts/test.sh
```

## Behavior

- Runs every `Tests/*/*.Tests.csproj` in turn (today `Shared.Tests` and
  `Modules.Tests`); new test projects are picked up automatically.
- Exits non-zero on any failure.
- No server access, nothing uploaded.
