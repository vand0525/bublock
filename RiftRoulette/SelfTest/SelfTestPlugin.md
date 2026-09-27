# SelfTestPlugin

Thin plugin class for the patch-day self-test (`Name` = "Rift Roulette
Self-Test"). No hooks.

## Commands (admin, Debug)

| Command | Behavior |
|---|---|
| `/selftest_run [all]` (`dw_selftest_run`) | `SelfTestService.Run(Debug)`. Replies the WARN / FAIL lines plus per-area and total counts; `all` replies every check. Full detail always goes to `selftest-*.log` |
| `/selftest_live <slot>` (`dw_selftest_live`) | `SelfTestService.Live(caller, player, Timer, Debug)` on the player in that slot: loadout read, banner, teleport + restraint check after 1 s. Error when the slot is empty; refused during a rift round |

Both check `AdminAuth` (server console trusted). The names contain `_`, so
they stay out of the player `/commands` list.
