# BanJoinRule

Pure anti-spam rule for banned players who reconnect. Tested by
`BanJoinRuleTests`.

## Types

- `BanJoinDecision`: `Statue` (let them in as a statue, then kick) or
  `Refuse` (refuse at connect; they never load in).
- `BanRecord(Strikes, LockedUntil)`: one per banned Steam ID. `None` is a
  clean record. `LockedForever` once `Strikes` reaches
  `ForeverAfterStrikes` (3).

## Operations

| Op | Behavior | Returns |
|---|---|---|
| `Decide(record, now)` | `Refuse` when locked forever or `now` is before `LockedUntil`; otherwise `Statue` | decision |
| `Strike(record, now)` | One more strike. Lockout after strike 1: 10 min; after strike 2: 30 min (`Lockouts`); strike 3: locked until restart (`LockedUntil` null) | new record |
| `Describe(record, now)` | `Strikes=N`, plus `locked M min` or `locked until restart` | text |

## Invariants

- A strike is only recorded for an allowed statue visit; a refused connect
  never adds one (refusals do not escalate further).
- "Until restart" means until the record is lost: records live in memory in
  `BanStatueService`, so a hot reload also clears them.
