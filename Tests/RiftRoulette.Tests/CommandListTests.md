# CommandListTests

Unit tests for `RiftRoulette/Lobby/CommandList`.

- `FormatPlayerCommands` drops hidden names and names with `_`, sorts by
  name, formats `/name - Description`, and prints `/name` alone when there is
  no description.
- The same name twice (any case) is listed once.
- `Scan` reads `[Command]` names, descriptions, and `Hidden` from a sample
  `DeadworksPluginBase` class declared in the test assembly.
