# Teams.projitems

MSBuild shared-items file that compiles `DeadlockTeams` and `ChoiceGuard`
into a consuming project.

```xml
<Import Project="..\Modules\Teams\Teams.projitems" />
```

- No dependencies.
- No commands; produces no DLL of its own.
