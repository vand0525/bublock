# ExecutionMode

Execution mode passed explicitly to operations (`.rules` §5).

## Values

- `Clean` — game lifecycle orchestration; low-noise logging.
- `Debug` — manual admin invocation; deeper diagnostic logging.

## Invariants

- Mode changes logging and diagnostics only, never gameplay outcomes.
- Passed as a parameter; there is no ambient/static current mode.
- Not yet consumed by any operation (logging arrives in Stage 4).
