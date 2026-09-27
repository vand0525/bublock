# SelfTestResult

Result types and console formatting for the self-test. Pure (no Deadworks
calls); tested by `SelfTestTests`.

## Types

- `CheckStatus`: `Pass`, `Warn`, `Fail`.
- `CheckResult(Area, Name, Status, Detail = "")`: one check.
  `Describe()` renders `PASS [Area] Name - Detail` (no ` - ` when the detail
  is empty).

## SelfTestReport

- `Summarize(results)`: `PASS n | WARN n | FAIL n`.
- `Lines(results, all)`: with `all`, every check then the total. Without it,
  only the WARN / FAIL checks, then one `Area: PASS n | WARN n | FAIL n` line
  per area (in first-seen order), then `Total: ...`.
