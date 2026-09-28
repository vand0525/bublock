# LoadoutPlugin

Thin admin command host for the Loadout module. Every command checks
`AdminAuth` (a null caller, the server console, is trusted) and runs in Debug
mode.

## Commands

| Command | Behavior |
|---|---|
| `/loadout_give <slot> <hero> [build]` | Parses the hero (enum or game name) and picks build 1–3 (0 or omitted = random). Runs `LoadoutService.Swap` on the player in that slot. Errors: no player in slot, unknown hero, no builds, bad build number, no live pawn. |
| `/loadout_copy <from> <to>` | Captures the live pawn in slot `from` (`LoadoutService.Capture`) and applies it to the player in slot `to` with `SwapSnapshot` (0 gold). Replies with the snapshot summary. Errors: empty slot, source has no live hero, target has no live pawn. Test tool for 1v1 mode |
| `/loadout_list <hero>` | Lists the stored builds (name, build ID, rank, matches, wins, planned value, number of optional groups) and the 9 items each would grant with the first optional pick (`ItemOrder` + `FirstSlots`, before the cap) |
| `/loadout_info` | Data date, source, window, and hero count; then the baseline value, the current cap (`LoadoutService.MaxValue`), and the banned items |
| `/loadout_cap [souls\|default]` | No argument: the current cap and the default (20,000). With a value: `LoadoutPlanner.TryParseCap` (1,000 to 200,000, or `default`), then `LoadoutService.SetMaxValue` (Debug), then replies `Cap <old> -> <new> (applies to the next builds handed out)`. Bad input: `CommandException` naming the range. Builds already on players are not changed |

## Logs

The admin gate writes to the `Loadout` feature log.

## Constraints

- Command names must stay unique across Bublock DLLs. Import
  `Loadout.projitems` alone when another DLL needs the service without these
  commands.
