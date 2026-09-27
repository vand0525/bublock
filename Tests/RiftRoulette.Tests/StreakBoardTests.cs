using RiftRoulette.Duel;

namespace Bublock.Tests.RiftRoulette;

public class StreakBoardTests
{
  [Fact]
  public void Best_keeps_the_highest_streak()
  {
    var board = new StreakBoard();

    for (var streak = 1; streak <= 5; streak++)
      board.Record(10, streak);

    Assert.False(board.Record(10, 3));
    Assert.Equal(5, board.BestOf(10));
  }

  [Fact]
  public void New_king_starts_at_one_without_touching_the_old_best()
  {
    var board = new StreakBoard();
    board.Record(10, 4);

    var (king, streak) = KothRule.Crown(10, 4, 20);
    board.Record(king, streak);

    Assert.Equal(4, board.BestOf(10));
    Assert.Equal(1, board.BestOf(20));
  }

  [Fact]
  public void Unknown_player_has_no_best_and_forget_and_clear_drop_rows()
  {
    var board = new StreakBoard();

    Assert.Equal(0, board.BestOf(99));

    board.Record(10, 2);
    board.Record(20, 1);
    board.Forget(10);

    Assert.Equal(0, board.BestOf(10));
    Assert.Equal(1, board.Count);

    board.Clear();
    Assert.Equal(0, board.Count);
  }
}
