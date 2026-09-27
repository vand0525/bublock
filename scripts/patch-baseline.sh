#!/usr/bin/env bash
# Save a known-good snapshot for patch-check.py (run before a Deadlock patch).
set -euo pipefail
exec python3 "$(dirname "$0")/patch-check.py" --save-baseline "$@"
