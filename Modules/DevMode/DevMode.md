# DevMode.projitems

MSBuild shared-items file that compiles `DevRules`, `PositionMemory` and
`DebugSnapshot` into a consuming project.

```xml
<Import Project="..\Modules\DevMode\DevMode.projitems" />
```

- `DevRules` is pure; the other two need `Shared.projitems` and
  `Movement.projitems`.
- No commands; the game type adds `/play`, `/stop`, `/pause`.
