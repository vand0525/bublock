using RiftRoulette.Lobby;

namespace Bublock.Tests.RiftRoulette;

public class OverviewRuleTests
{
  private static readonly DateTime Start = new(2026, 9, 27, 12, 0, 0, DateTimeKind.Utc);

  [Fact]
  public void Shows_for_10_seconds_then_ends()
  {
    var end = OverviewRule.EndFrom(Start);

    Assert.True(OverviewRule.Showing(end, Start));
    Assert.True(OverviewRule.Showing(end, Start.AddSeconds(9.9)));
    Assert.False(OverviewRule.Showing(end, Start.AddSeconds(10)));
    Assert.False(OverviewRule.Showing(null, Start));
  }

  [Fact]
  public void Starts_once_per_live_round()
  {
    Assert.True(OverviewRule.CanStart(roundLive: true, roundNumber: 3, lastShownRound: null));
    Assert.True(OverviewRule.CanStart(roundLive: true, roundNumber: 3, lastShownRound: 2));
    Assert.False(OverviewRule.CanStart(roundLive: true, roundNumber: 3, lastShownRound: 3));
    Assert.True(OverviewRule.CanStart(roundLive: true, roundNumber: 4, lastShownRound: 3));
  }

  [Fact]
  public void Never_starts_without_a_live_round()
  {
    Assert.False(OverviewRule.CanStart(roundLive: false, roundNumber: 3, lastShownRound: null));
    Assert.False(OverviewRule.CanStart(roundLive: true, roundNumber: 0, lastShownRound: null));
  }
}
