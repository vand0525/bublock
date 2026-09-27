# HudService

Static service that shows the game's on-screen announcement banner (title
plus smaller description) to one player or to everyone. Game-agnostic.

## Constants

- `AdminSayLabel = "Server admin"`: the description line under an admin
  message (`/hud_say`).

## Operations

| Op | Behavior | Returns |
|---|---|---|
| `Announce(player, title, description = "", mode)` | `player.HudAnnounce(title, description)`; Debug line with `PlayerRef` | — |
| `AnnounceAll(title, description = "", mode)` | `HudAnnounce` for every controller from `Players.GetAll()`; one Information line | players sent to |
| `ParseAnnouncement(text)` | Splits admin input on the first `\|` into title and description, both trimmed; no `\|` means no description (pure) | `(Title, Description)` |

## Logs

`Hud` feature log (`hud-YYYYMMDD.log`).

## Deadworks notes

- `CCitadelPlayerController.HudAnnounce(title, description)` sends
  `CCitadelUserMsg_HudGameAnnouncement` (`TitleLocstring`,
  `DescriptionLocstring`) to that player's slot only. Plain text works (the
  reference `MatchStart.dll` sends `"Match started"`); strings starting with
  `#` may be treated as localization tokens by the client.
- The client shows a banner for a few seconds
  (`citadel_hud_announcement_display_time_max`, client convar, default 3);
  banners sent close together queue up.
- No rate limit here; callers must not announce per tick.
