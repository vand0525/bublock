# AccessListTests

Unit tests for `Modules/Access/AccessList`.

- `ToJson` then `Parse` keeps the mode and both lists, with IDs sorted.
- Hand edits parse: comments, trailing commas, a capitalized key, quoted IDs,
  and a missing `allowed` list.
- `statueModifier` round-trips, is left out of the file when unset, and a
  blank value parses as unset.
- Bad JSON and `null` throw `JsonException`.
- `Check` applies bans, the whitelist, admin status, and private mode.
- `Check(..., forcePrivate: true)` treats an open file as private (bans
  still win, allowed IDs and admins pass) without changing `Private`.
