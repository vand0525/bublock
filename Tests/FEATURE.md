# Tests — Feature

## Purpose

Local automated tests for code that does not need a running Deadworks
server. Never deployed to the server.

## Projects

- `Shared.Tests/` — xUnit tests for `Bublock/Shared` logging (imports
  `Shared.projitems`). Tests run against temp folders with a fake clock.
- `Modules.Tests/` — xUnit tests for the pure helpers in `Bublock/Modules`
  (imports `Shared.projitems`, `WorldText.projitems`,
  `Movement.projitems`, `Hud.projitems`, `Queue.projitems`, `Spectate.projitems`, and `Loadout.projitems`, which
  also embeds `hero-builds.json` in the test assembly; references
  `Google.Protobuf` because `MovementService` sends a camera message):
  WorldText placement math and text formatting, the Movement
  `LocationRegistry`, the Hud banner text parser, the Loadout planner
  (budgeted shopping: 12 slots, components, selling, optional groups,
  banned items, ability bits), the build catalog (inline sample plus the
  committed data, sell priorities, baseline value), the `PlayerQueue` join queue, and the spectate choice
  (keep, killer, any, park) and straight-down angle. Service ops that touch entities or players are
  not tested here.
- `RiftRoulette.Tests/` — xUnit tests for pure Rift Roulette pieces, included
  directly as linked files (`RiftRoulette/Round/RoundHeroes.cs`,
  `RiftRoulette/Lobby/RiftRouletteTeams.cs`,
  `CommandList.cs`, `RiftRoulette/Locations/RiftRouletteLocations.cs`,
  `RiftRoulette/Rift/RiftSide.cs`, `RiftWatch.cs`, `RiftRoundResult.cs`,
  `RiftRoulette/Round/RoundLocations.cs`, `WatchSpotRule.cs`, `WatchLayout.cs`,
  `WatchGuardRule.cs`,
  `RiftRoulette/GameLoop/MatchState.cs`,
  `MatchConfig.cs`, `AutoStartRule.cs`, `SoulRule.cs`, `RiftRoulette/RandomMode/HeroDraw.cs`, `BenchRule.cs`,
  `RiftRoulette/Lobby/TeamBalance.cs`, `AdminSeatRule.cs`, `StreamFraming.cs`, `RiftRoulette/Stats/StatsLedger.cs`, `DamageLedger.cs`,
  `StatsBoardText.cs`, `RiftRoulette/Balance/BalanceTracker.cs`,
  `BalancePicker.cs`, `RiftRoulette/Betting/BetBook.cs`, `BetBoardText.cs`, `MarkBook.cs`) rather than referencing the plugin DLL: round
  hero bookkeeping, team name parsing, the `/commands` list filter, the `/about` text, rift sides, the rift outcome
  decision, each side's team starts (each team on its own half) and watch
  spot, which side the watch spot is above, the yellow board layout and
  watch view angles, the below-the-watch-spot line, match scoring, match
  config parsing, which currency gains are blocked, the match auto-start decision, the admin
  seat rules, the stream camera's fixed spots and park rule,
  the random hero draw, who sits out (bench rotation) and the fighting teams,
  team placement, kill / death / assist counting and
  board text, the auto-balance trigger and pick, and betting souls, payouts,
  steals, marks and board text. Imports `Shared.projitems`,
  `Movement.projitems` because the locations are Movement types, and
  `Queue.projitems` for the bench rotation's `PlayerQueue`.

All three test projects reference `Google.Protobuf` because Shared
(`PlayerChat`) and Movement send net messages.

## How to run

`./scripts/test.sh` (runs `dotnet test` on every test project).

## Relation to plugins

- Tests must not call into Deadworks game types (`ConVar`, controllers,
  entities); those only work inside the server process.
- Behavior that needs the server is verified manually in game.
