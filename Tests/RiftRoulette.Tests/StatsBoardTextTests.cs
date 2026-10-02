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
  public void Trim_cuts_long_names()
  {
    Assert.Equal("Short", StatsBoardText.Trim("Short"));
    Assert.Equal(StatsBoardText.NameLength, StatsBoardText.Trim(new string('x', 40)).Length);
  }
}
