using RiftRoulette.Betting;

namespace Bublock.Tests.RiftRoulette;

public class BetBoardTextTests
{
  [Fact]
  public void Board_ranks_by_chips_then_name()
  {
    var text = BetBoardText.Board([new BetRow("bo", 100), new BetRow("Al", 1200), new BetRow("ann", 100)]);

    Assert.Equal("BETTING\n\n1  Al   1,200\n2  ann   100\n3  bo   100", text);
  }

  [Fact]
  public void Board_shows_at_most_eight_rows()
  {
    var rows = Enumerable.Range(1, 12).Select(i => new BetRow($"p{i:00}", i * 100));

    var lines = BetBoardText.Board(rows).Split('\n');

    Assert.Equal(2 + BetBoardText.MaxRows, lines.Length);
    Assert.Equal("1  p12   1,200", lines[2]);
  }

  [Fact]
  public void Board_with_nobody_says_so()
  {
    Assert.Equal($"BETTING\n\n{BetBoardText.Empty}", BetBoardText.Board([]));
  }
}
