using Bublock.Modules.Queue;

namespace Bublock.Tests.Modules;

public class PlayerQueueTests
{
  [Fact]
  public void Join_appends_and_returns_the_position()
  {
    var queue = new PlayerQueue();

    Assert.Equal(1, queue.Join(10));
    Assert.Equal(2, queue.Join(20));
    Assert.Equal(3, queue.Join(30));
    Assert.Equal([10UL, 20UL, 30UL], queue.Items);
  }

  [Fact]
  public void Join_twice_keeps_the_original_place()
  {
    var queue = new PlayerQueue();
    queue.Join(10);
    queue.Join(20);

    Assert.Equal(1, queue.Join(10));
    Assert.Equal(2, queue.Count);
  }

  [Fact]
  public void Leave_closes_the_gap()
  {
    var queue = new PlayerQueue();
    queue.Join(10);
    queue.Join(20);
    queue.Join(30);

    Assert.True(queue.Leave(20));
    Assert.False(queue.Leave(20));
    Assert.Equal(2, queue.PositionOf(30));
    Assert.Null(queue.PositionOf(20));
    Assert.False(queue.Contains(20));
  }

  [Fact]
  public void Front_returns_at_most_n()
  {
    var queue = new PlayerQueue();
    queue.Join(10);
    queue.Join(20);
    queue.Join(30);

    Assert.Equal([10UL, 20UL], queue.Front(2));
    Assert.Equal([10UL, 20UL, 30UL], queue.Front(5));
    Assert.Empty(queue.Front(0));
  }

  [Fact]
  public void MoveToBack_sends_a_player_to_the_end()
  {
    var queue = new PlayerQueue();
    queue.Join(10);
    queue.Join(20);
    queue.Join(30);

    Assert.True(queue.MoveToBack(10));
    Assert.Equal([20UL, 30UL, 10UL], queue.Items);
    Assert.False(queue.MoveToBack(99));
  }

  [Fact]
  public void RemoveWhere_and_Clear()
  {
    var queue = new PlayerQueue();
    queue.Join(10);
    queue.Join(20);
    queue.Join(30);

    Assert.Equal(2, queue.RemoveWhere(id => id != 20));
    Assert.Equal([20UL], queue.Items);

    queue.Clear();
    Assert.Equal(0, queue.Count);
  }
}
