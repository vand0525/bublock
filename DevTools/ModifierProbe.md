# ModifierProbe

Logs the name of every game modifier the first time it is added in this
load, so modifier names that no data file lists (for example Vyper's
Petrify, used for the banned-player statue) can be read from the logs after
someone uses the ability in game.

## Operations

| Op | Behavior |
|---|---|
| `Observe(args)` | Called by `DevToolsPlugin.OnAddModifier` for every added modifier. Reads `args.ModifierVData.Name`; the first time a name is seen, Information `Modifier seen for the first time Name= Caster= Owner=` (caster / owner: the player's name for a hero pawn, else the entity's designer name, `-` when none) |
| `Count` | Distinct names seen since load |

## State

`Seen`: case-insensitive set of names, static, reset by every load.

## Logs

`modifiers-YYYYMMDD.log` in `bublock/logs/DevTools/`, prefix
`[DevTools.Modifiers]`. One line per distinct name, so it stays small.

## Constraints

- The hook runs for every modifier on every entity (troopers too); only a
  set lookup happens after the first sighting. Never block or change the
  modifier here (`DevToolsPlugin` always returns `HookResult.Continue`).
