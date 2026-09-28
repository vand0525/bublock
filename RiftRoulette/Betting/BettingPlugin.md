# BettingPlugin

Thin host for round betting (`BettingService`).

## Hooks

- `OnChatMessage(ChatMessage)`: while betting is `Active`, a chat line
  that is exactly `sapphire` or `amber` (trimmed, any case) is a bet. The
  sender is found again by Steam ID on the next tick, then
  `BettingService.TryBet` and the reply goes to them in chat. Always
  returns `Continue`, so the line still shows in chat. Bots and lines that
  are not a team name are ignored.

## Commands

| Command | Who | Calls | Reply |
|---|---|---|---|
| `/bet <sapphire\|amber>` | players | `BettingService.TryBet` | the bet reply in chat; `Bet on sapphire or amber.` for another word |
| `/souls` | players | `BettingService.DescribePlayer` | souls, open bet, whether betting is open, hero reservation |
| `/bet_status` | admin | `BettingService.RefreshBoard` + `Describe` (Debug) | every participant's souls and bet |

## Deadworks constraints

- `ChatMessage` has `SenderSlot`, `ChatText`, `AllChat`, `LaneColor`, and
  a `Controller` property (looked up from the slot). Replies are sent on
  the next tick, outside the chat hook.
