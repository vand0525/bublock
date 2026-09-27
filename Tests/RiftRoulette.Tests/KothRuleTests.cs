using RiftRoulette.Duel;

namespace Bublock.Tests.RiftRoulette;

public class KothRuleTests
{
  private const int Sapphire = 2;
  private const int Amber = 3;

  private static readonly Dictionary<ulong, int> Fighters = new() { [10] = Sapphire, [20] = Amber };

  [Fact]
  public void Capture_by_a_team_makes_the_other_fighter_the_loser()
  {
    Assert.Equal(20UL, KothRule.Loser(Fighters, Sapphire));
    Assert.Equal(10UL, KothRule.Winner(Fighters, Sapphire));
    Assert.Equal(10UL, KothRule.Loser(Fighters, Amber));
    Assert.Equal(20UL, KothRule.Winner(Fighters, Amber));
  }

  [Fact]
  public void Tie_or_unknown_winner_has_no_loser()
  {
    Assert.Null(KothRule.Loser(Fighters, null));
    Assert.Null(KothRule.Winner(Fighters, null));
  }

  [Fact]
  public void Winner_team_without_a_fighter_has_no_loser()
  {
    var alone = new Dictionary<ulong, int> { [10] = Sapphire };

    Assert.Null(KothRule.Loser(alone, Amber));
    Assert.Null(KothRule.Winner(alone, Amber));
    Assert.Null(KothRule.Loser(alone, Sapphire));
  }

  [Fact]
  public void Crown_counts_the_streak()
  {
    Assert.Equal((10UL, 1), KothRule.Crown(null, 0, 10));
    Assert.Equal((10UL, 3), KothRule.Crown(10, 2, 10));
    Assert.Equal((20UL, 1), KothRule.Crown(10, 5, 20));
  }
}
