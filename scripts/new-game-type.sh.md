# new-game-type.sh

Scaffolds a new game type: its own plugin DLL on the engine (`Shared/` +
`Modules/*`), never inside another game type.

## Usage

```bash
./scripts/new-game-type.sh <Name> <prefix> ["Title"]
./scripts/new-game-type.sh Grifball grif "Grifball"
```

- `Name`: PascalCase; the folder, project, DLL and namespace.
- `prefix`: 2-8 lowercase letters for the admin commands (`grif_status`,
  `grif_start`, `grif_end`, `grif_time`); refused if another game type
  already uses it.
- `Title`: what players see (defaults to `Name`).

## What it makes

From `templates/game-type/` and `templates/game-type-tests/` (tokens
`__NAME__`, `__NAMELOWER__`, `__PREFIX__`, `__TITLE__` replaced in names
and contents):

- `<Name>/`: `<Name>.csproj` (Shared + Loadout, Hud, Movement, Teams,
  Arena, Session, Economy), `<Name>Rules.cs`, `<Name>Service.cs`,
  `LobbyPlugin.cs`, `ArenaPlugin.cs`, `MatchPlugin.cs`, `Data/arena.json`
  (the mid lane brawl), `FEATURE.md` and a `.md` per file.
- `Tests/<Name>.Tests/`: rule and arena tests.
- Both projects added to `Bublock.sln` (when `dotnet` is on `PATH`).

The result is a working game type: timed team deathmatch in the arena
(2-minute matches, a point per enemy kill, most points wins, players pick
heroes, team changes refused). Verified 2026-09-28: a scaffolded game type
builds with 0 warnings and its 5 tests pass.

## Then

1. Describe the game in `<Name>/FEATURE.md`; change `<Name>Rules.cs`.
2. Add engine modules in the `.csproj` and wire them in `<Name>Service.cs`.
3. A new arena: edit `Data/arena.json`, run `scripts/check-arena.py`.
4. `scripts/update.sh`, `scripts/test.sh`.
5. Deploy with `DW_PLUGINS` / `DW_PARKED` (`deploy.sh.md`), then restart.
6. `scripts/knowledge.sh`.

## Refuses

An invalid name or prefix, an existing folder, or a taken prefix: exits 1
without writing anything.
