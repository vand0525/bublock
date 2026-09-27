# DraftBoardText

Text for the draft boards and pool listings. Pure; unit tested in
`Tests/RiftRoulette.Tests`.

## Operations

- `TeamBoard(teamName, heroes, isSelected)`: the team board text, same
  layout as the archive `CreateDraftMessage`:

  ```text
  SAPPHIRE

  Shiv (SELECTED)
  Yamato
  ...

  /pick <hero>
  /unpick
  ```

  The footer reads `/pick <hero>` / `/unpick` (archive: `/select <hero>` /
  `/unselect`). That is the only visual change to the boards (inventory
  §1.4).
- `PoolLine(teamName, heroes, isSelected)`: one line for chat / console,
  e.g. `Sapphire: Shiv (taken), Yamato, ...` (used by `/heroes` and
  `/draft_status`).

## Inputs / outputs

- `isSelected`: normally `DraftState.IsSelected`; injected so tests need no
  static state.
