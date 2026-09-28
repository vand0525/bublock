# AutoStartService

Starts the match when enough human players are connected and ends it when
too few remain, so no admin has to be online. Called 2 s after
every join (`LobbyService.AdmitPlayer` → `CheckSoon`), on every disconnect
(`LobbyService.RemovePlayer`), 3 s after the DLL loads
(`GameLoopPlugin.OnLoad`), when an admin turns it on, and after every 1v1
queue join / leave (`DuelPlugin`).

## State

- `Enabled` (default on after every DLL load); `MinPlayers` = 2.

## Operations

| Op | Behavior | Returns |
|---|---|---|
| `SetEnabled(enabled, mode)` | Sets the flag, logs `Auto-start set` | — |
| `Check(timer, mode, leavingSteamId = null)` | Counts participants (`Participants.Humans()`: no bots, no seated admin) — in 1v1 mode the connected players in the 1v1 queue instead (`DuelService.QueuedCount`) — leaving out `leavingSteamId` (the leaving controller may still be listed during the disconnect callback). `AutoStartRule.Decide(..., leaving: leavingSteamId != null)` then (a disconnect can only end a match, never start one): `Start` in 1v1 mode without a copied build (`DuelService.HasSnapshot`) → Debug line `Auto-start waiting for a 1v1 build`, returns `None`; otherwise `Start` → `MatchService.Start(timer, mode)`; if the match did not start (for example an admin rift is running) logs `Auto-start refused` with the reply and returns `None`. `End` → `MatchService.End(timer, mode)`, then 3 s later the waiting message to every remaining human if no match started meanwhile. No action on a join or load check while waiting (auto-start on, no match, 1 human; 1v1: 1 queued) → the waiting message to every human | `AutoStartAction` taken |
| `RemindWaiting(mode)` | Every `WaitingReminderSeconds` (30 s, `LobbyPlugin` timer): the waiting message again while waiting (same condition) | — |
| `CheckSoon(timer, mode)` | `Check(timer, mode)` after `JoinCheckDelaySeconds` (2 s). Used on join: a match started inside `OnClientFullConnect` lost every player's `SelectHero` (no builds) | — |
| `Describe()` | `Auto-start=on \| MinPlayers=2 \| Humans=N`, plus `\| Queued=N (1v1 counts the queue)` in 1v1 mode | line |

## Waiting message

A chat line, never a banner (banners go by too fast and long text does not
fit): `Waiting for players: Match starts when 1 more player joins. You wait
up top until then.` (the count is `MinPlayers` minus the humans present;
1v1 mode: `1v1 starts when 2 are queued - /queue`). Players joining an
empty server used to get no message while restrained up top and left
within 10-20 s (logs 2026-09-27).

## Side effects

- Starting or ending a match has all the effects of `MatchService.Start` /
  `End` (banners, Random mode teams and heroes, 1v1 build and lock, draft
  reset on end).
- Logs to `match-*.log` (check and waiting announcements at Debug; start / end / refusal at
  Information) and the master log (`Match auto-started Humans=` /
  `Match auto-ended Humans=`).

## Invariants

- Only reacts to events; an admin `/match_end` while it is on holds until
  the next join (or disconnect below the minimum), reload, or
  `/match_auto on`.
- Never starts a match from a disconnect: the 2026-09-27 crash was a start
  (with the leaving player still listed) during `RemovePlayer`.
- With `Enabled` off, `Check` never starts or ends a match.
- The waiting message after a match ends is delayed 3 s so it comes after
  the `Match over` banner `End` sends.
