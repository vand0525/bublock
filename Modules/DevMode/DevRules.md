# DevRules

Dev / prod for game types. Pure; unit tested in `Tests/Modules.Tests`.

## Types

`RunMode`: `Dev` (sandbox: no live session, environment-changing commands
allowed) and `Prod` (the live session).

## Operations

| Op | Behavior | Returns |
|---|---|---|
| `Refusal(mode, command)` | In prod: `<command> is dev-only; a live session is running (/stop first).`; in dev: null (allowed) | message or null |
| `Name(mode)` | `dev` / `prod` | string |
| `TryParse(text, out mode)` | `dev` / `prod`, trimmed, any case | bool |
