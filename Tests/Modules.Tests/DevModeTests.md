# DevModeTests

Unit tests for `Modules/DevMode/DevRules`: a dev-only command is allowed in
dev and refused in prod with the `/stop first` message; mode names parse
(`dev`, `prod`, any case, trimmed; anything else is refused).
