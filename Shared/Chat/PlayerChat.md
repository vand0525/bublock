# PlayerChat

Sends a chat line to one player.

## Behavior

- `Send(player, message)`: sends `CCitadelUserMsg_ChatMsg` with
  `PlayerSlot = player.Slot`, `Text = message`, `AllChat = true`, filtered to
  that player only (`RecipientFilter.Single(player.Slot)`). Body identical
  to the archive Rift Roulette `SendChat`.

## Inputs / outputs

- `player`: a connected controller. No return value.

## Side effects

- One net message to one client. No logging (callers log if the line
  matters).

## Constraints

- Game-thread only.
- Compiling this file needs a `Google.Protobuf` reference (see
  `reference/resources.md` Discoveries).
