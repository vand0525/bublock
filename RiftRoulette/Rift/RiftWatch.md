# RiftWatch

The per-tick outcome decision of the rift watcher, kept out of the
`Timer.Sequence` lambda so it can be unit tested. Pure; one instance per
rift.

## Operations

`Observe(newTrooper, cashinExists)` is called once per watch tick and
returns:

| Input | Result |
|---|---|
| a trooper created after the rift started exists | `Finished` (checked first, even if a cash-in exists) |
| no new trooper, cash-in exists, first time seen | `CashinAppeared` (sets `SawCashin`) |
| no new trooper, cash-in exists, seen before | `Waiting` |
| no new trooper, no cash-in, cash-in seen before | `Tied` |
| no new trooper, no cash-in, never seen | `Waiting` |

## State

`SawCashin`: whether a `citadel_koth_cashin` has been seen during this rift.

## Invariants

- The caller stops watching after `Finished` or `Tied`.
- Known-good order of checks; do not reorder.
