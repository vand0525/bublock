# AccessRuleTests

Unit tests for `RiftRoulette/Lobby/AccessRule`.

- A ban wins over the whitelist and admin status.
- Open mode lets in anyone who is not banned; private mode lets in only
  whitelisted players or admins.
- `TryParseSteamId` accepts a real Steam64 ID (with surrounding spaces) and
  rejects slot numbers, the base value, text, and empty input.
