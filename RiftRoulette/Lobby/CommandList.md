# CommandList

Builds the player command list shown by `/commands`. Deadworks' own
`dw_help` is console-only and its registration index is internal, so the
list is read from the `[Command]` attributes in this assembly.

## Types

- `CommandInfo(Name, Description, Hidden)` — one registered name
  (each alias of an attribute becomes its own entry).

## Operations

| Operation | Input | Output |
|---|---|---|
| `Scan(assembly)` | an assembly | every `[Command]` name on non-abstract `IDeadworksPlugin` types (public and non-public instance methods, as the Deadworks loader scans) |
| `FormatPlayerCommands(commands)` | `CommandInfo`s | lines `"/name - Description"` (or `"/name"` without a description) for non-hidden names **without** `_`, one per name (case-insensitive), sorted by name |
| `PlayerCommands(assembly)` | an assembly | `FormatPlayerCommands(Scan(assembly))` |

`Footer` is `"Full list: dw_help in console"`.

## Invariants

- The player/admin split is the naming rule from `.rules` §6: player commands
  are short verbs with no prefix; every admin command starts with a feature
  word and `_`. A new player command with `_` would be hidden from the list.
- Pure except for reflection; no Deadworks calls, no logging. Tested in
  `Tests/RiftRoulette.Tests/CommandListTests`.
