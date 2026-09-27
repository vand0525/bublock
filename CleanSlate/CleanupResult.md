# CleanupResult

Immutable result of one `CleanSlateService.RemoveMapEntities` run.

## Shape

| Member | Meaning |
|--------|---------|
| `Removed` | designer name -> number of entities removed |
| `Disabled` | designer name -> number of entities sent the `Disable` input |
| `RemovedCount` / `DisabledCount` | totals |
| `Describe()` | `Removed=[a=1 b=2] Disabled=[c=9]`, keys sorted, `none` when empty |

## Invariants

- Pure data, no Deadworks calls. Used for the `cleanup` summary line, the
  master line and the `/cleanup_run` reply.
