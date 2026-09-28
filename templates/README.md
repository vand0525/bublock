# templates

Source for `scripts/new-game-type.sh`. Not built or tested by themselves:
the tokens `__NAME__`, `__NAMELOWER__`, `__PREFIX__`, `__TITLE__` are
replaced when a game type is scaffolded.

- `game-type/`: a working base game type (timed team deathmatch in the mid
  lane brawl arena) on the engine modules.
- `game-type-tests/`: its test project.

When an engine module gains something every game type should use, update
the template too, and check it by scaffolding a throwaway game type,
building, testing, then removing it (`new-game-type.sh.md`).
