# LoadoutPlugin

Thin admin command host for the Loadout module. Every command checks
`AdminAuth` (a null caller, the server console, is trusted) and runs in Debug
mode.

## Commands

| Command | Behavior |
|---|---|
| `/loadout_give <slot> <hero> [build]` | Parses the hero (enum or game name) and picks build 1–3 (0 or omitted = random). Runs `LoadoutService.Swap` on the player in that slot. Errors: no player in slot, unknown hero, no builds, bad build number, no live pawn. |
| `/loadout_show <slot>` | `LoadoutService.Capture` on the slot's hero, then `LoadoutSnapshot.HeldLines(HeroBuildCatalog.Default.CostOf)`: replies the summary and one line per held item, and logs them as one Information line `Held items` (with `PlayerRef`). Error for an empty slot or no hero |
| `/loadout_copy <from> <to>` | Captures the live pawn in slot `from` (`LoadoutService.Capture`) and applies it to the player in slot `to` with `SwapSnapshot` (0 gold). Replies with the snapshot summary. Errors: empty slot, source has no live hero, target has no live pawn. Test tool for 1v1 mode |
| `/loadout_list <hero>` | At the current cap: lists the stored builds (name, build ID, rank, matches, wins, planned value, sold, skipped, filled and upgraded counts, number of optional groups) and the items each would end with (up to 12) with the first optional pick (`HeroBuildCatalog.Plan`) |
| `/loadout_info` | Data date, source, window, and hero count; then the baseline value (median planned value at 20,000, 12 slots), the current cap (`LoadoutService.MaxValue`), and the banned items |
| `/loadout_cap [souls\|default]` | No argument: the current cap and the default (20,000). With a value: `LoadoutPlanner.TryParseCap` (1,000 to 200,000, or `default`), then `LoadoutService.SetMaxValue` (Debug), then replies `Cap <old> -> <new> (applies to the next builds handed out)`. Bad input: `CommandException` naming the range. Builds already on players are not changed. The cap sets both the items' budget and the hero's level |

## Logs

The admin gate writes to the `Loadout` feature log.

## Constraints

- Command names must stay unique across Bublock DLLs. Import
  `Loadout.projitems` alone when another DLL needs the service without these
  commands.
