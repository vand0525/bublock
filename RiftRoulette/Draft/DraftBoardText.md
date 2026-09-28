# DraftBoardText

Text for the draft boards and pool listings. Pure; unit tested in
`Tests/RiftRoulette.Tests`.

## Operations

- `TeamBoard(teamName, heroes, isSelected)`: the team board text:

  ```text
  SAPPHIRE

  Shiv (SELECTED)
  Yamato
  ...

  /pick <hero>
  /unpick
  ```

  The footer lists the pick commands, `/pick <hero>` and `/unpick`.
- `PoolLine(teamName, heroes, isSelected)`: one line for chat / console,
  e.g. `Sapphire: Shiv (taken), Yamato, ...` (used by `/heroes` and
  `/draft_status`).

## Inputs / outputs

- `isSelected`: normally `DraftState.IsSelected`; injected so tests need no
  static state.
