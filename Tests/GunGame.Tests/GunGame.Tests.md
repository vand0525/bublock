# GunGame.Tests

xUnit project for the Gun Game game type (never deployed). It compiles the
engine modules Gun Game's pure code needs (`Shared`, `Movement`, `Teams`,
`Arena`, `Session`), links `GunGame/GunGameRules.cs`, and embeds
`GunGame/Data/arena.json` under the same logical name as the plugin, so the
real asset is tested. `scripts/test.sh` picks it up automatically.
