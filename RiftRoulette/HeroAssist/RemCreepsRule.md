# RemCreepsRule

Pure decision for Rem's 1v1 creeps (`RemCreepsService`). No Deadworks
calls; linked into the test project.

## Constants

| Name | Value | Meaning |
|---|---|---|
| `DefaultCount` | 3 | Creep count after every DLL load |
| `MaxCount` | 8 | Upper bound for `/rem_creeps` |

## Operations

| Op | Returns |
|---|---|
| `ShouldSpawn(isRandom, fighterCount, remIsFighter, count)` | true only in Random mode with exactly 2 fighters, Rem (`Heroes.Familiar`) fighting, and `count > 0` |
| `ClampCount(count)` | 0–`MaxCount` |
