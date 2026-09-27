# AccessListTests

Unit tests for `RiftRoulette/Lobby/AccessList`.

- `ToJson` then `Parse` keeps the mode and both lists, with IDs sorted.
- Hand edits parse: comments, trailing commas, a capitalized key, quoted IDs,
  and a missing `allowed` list.
- Bad JSON and `null` throw `JsonException`.
- `Check` applies bans, the whitelist, admin status, and private mode.
