# PositionMemory

Remembers where players stood in dev so ending a live session puts them
back there. Game-agnostic; one instance per game type.

## Operations

| Op | Behavior | Returns |
|---|---|---|
| `Save(players)` | Clears, then stores each player's position and eye angles (`MovementService.Where`); logs `Dev positions saved` | count |
| `Restore(players, mode)` | Teleports each saved player back (`MovementService.TeleportTo`, camera to the saved angles); players without a saved spot are left alone; logs `Dev positions restored` | count restored |
| `Clear()`, `Count` | — | — |

## Deadworks constraints

- Call `Restore` outside game events (next tick).
