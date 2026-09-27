# HudPlugin

Thin Deadworks plugin class (`Name` = `Hud`) exposing the admin
`/hud_announce` and `/hud_say` commands. No hooks, no state.

## Commands

| Command | Op | Result |
|---|---|---|
| `/hud_announce <title> [\| description]` | `HudService.ParseAnnouncement`, `HudService.AnnounceAll` (Debug) | Banner on every player's screen; replies `[Hud] Announced to N player(s)`. Error when the title is empty |
| `/hud_say <message>` | `HudService.AnnounceAll(message, AdminSayLabel)` (Debug) | The whole message as the big banner title, `Server admin` underneath, on every player's screen; replies `[Hud] Said to N player(s): <message>`. Error when the message is empty |

Admin-only (`AdminCommand.Authorize` with the `Hud` log; server console
trusted, so both run from the Deadworks / server console while spectating,
where chat is unavailable). Text takes the rest of the line. For
`/hud_announce`, everything after the first `|` is the description, e.g.
`/hud_announce Round 3 | GREEN rift`; `/hud_say` keeps `|` as text.

Every message is logged at Information in `hud-*.log`
(`Announced to all Title= Description= Players=`).
