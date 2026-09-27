# patch-baseline.sh

Saves a known-good snapshot before a Deadlock patch:
`python3 patch-check.py --save-baseline` (see `patch-check.py.md`).

```bash
Bublock/scripts/patch-baseline.sh
```

Writes `RiftRoulette/reference/baseline/<UTC date>/snapshot.json` and a copy
of `hero-builds.json`; a second run the same day overwrites that folder.
Needs network. Run `dw_selftest_run` on the server at the same time and keep
its log as the known-good self-test output.
