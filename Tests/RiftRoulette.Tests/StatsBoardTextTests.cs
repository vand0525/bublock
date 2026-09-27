using RiftRoulette.Stats;

namespace Bublock.Tests.RiftRoulette;

public class StatsBoardTextTests
{
  [Fact]
  public void TeamBoard_shows_header_total_and_rows_sorted_by_kills()
  {
    var rows = new[]
    {
      new StatsRow("Sam", new PlayerStats(1, 3, 2)),
      new StatsRow("Theo", new PlayerStats(5, 1, 4))
    };

    var text = StatsBoardText.TeamBoard("SAPPHIRE", 2, rows);

    Assert.Equal(
      "SAPPHIRE - 2 rounds\nK / D / A   6 / 4 / 6\n\nTheo   5 / 1 / 4\nSam   1 / 3 / 2",
      text);
  }

  [Fact]
  public void TeamBoard_single_round_and_no_players()
  {
    Assert.Equal("AMBER - 1 round\nK / D / A   0 / 0 / 0\n\nNo players", StatsBoardText.TeamBoard("AMBER", 1, []));
  }

  [Fact]
  public void StreakBoard_ranks_by_best_then_name_and_skips_zero()
  {
    var rows = new[]
    {
      new StreakRow("sam", 3),
      new StreakRow("Zed", 0),
      new StreakRow("Theo", 5),
      new StreakRow("Ann", 3)
    };

    Assert.Equal("STREAKS\n\n1  Theo   5\n2  Ann   3\n3  sam   3", StatsBoardText.StreakBoard(rows));
  }

  [Fact]
  public void StreakBoard_empty_says_no_streaks()
  {
    Assert.Equal("STREAKS\n\nNo streaks yet", StatsBoardText.StreakBoard([]));
    Assert.Equal(["No streaks yet"], StatsBoardText.StreakLines([new StreakRow("Zed", 0)]));
  }

  [Fact]
  public void Trim_cuts_long_names()
  {
    Assert.Equal("Short", StatsBoardText.Trim("Short"));
    Assert.Equal(StatsBoardText.NameLength, StatsBoardText.Trim(new string('x', 40)).Length);
  }
}
