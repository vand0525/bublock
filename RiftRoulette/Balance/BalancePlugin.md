# BalancePlugin

Thin plugin class (`Name` = "Rift Roulette Balance"). Admin commands only;
each calls `AdminCommand.Authorize` with the `Balance` log (server console
trusted), runs in Debug mode, and replies with `[Balance]`.

| Command | Calls | Reply / errors |
|---|---|---|
| `/balance_status` | `BalanceService.Describe` | enabled + verdict, counters since last swap, rules |
| `/balance_auto <on\|off>` | `BalanceService.SetEnabled` | `Auto-balance on/off`; error for any other word |
| `/balance_now` | `RandomModeService.PrepareRound(Timer, Debug, forceBalance: true)` | `Balanced and rerolled: N swapped, M pending`; error unless Random mode and the match is in intermission. Everyone also gets new heroes (it is a reroll) |
