using RiftRoulette.Betting;

namespace Bublock.Tests.RiftRoulette;

public class MarkBookTests
{
  [Fact]
  public void A_marker_holds_one_mark()
  {
    var book = new MarkBook();

    Assert.True(book.Place(1, 2, "Two", 3));
    Assert.False(book.Place(1, 4, "Four", 3));
    Assert.True(book.TryGet(1, out var mark));
    Assert.Equal(new Mark(2, "Two", 3), mark);
  }

  [Fact]
  public void Place_refuses_self_and_empty_ids()
  {
    var book = new MarkBook();

    Assert.False(book.Place(1, 1, "One", 3));
    Assert.False(book.Place(0, 2, "Two", 3));
    Assert.False(book.Place(1, 0, "None", 3));
    Assert.Equal(0, book.Count);
  }

  [Fact]
  public void Several_markers_may_mark_one_target()
  {
    var book = new MarkBook();

    Assert.True(book.Place(1, 9, "Nine", 3));
    Assert.True(book.Place(2, 9, "Nine", 3));
    Assert.Equal(2, book.Count);
  }

  [Fact]
  public void Consume_needs_the_marked_victim_and_round()
  {
    var book = new MarkBook();
    book.Place(1, 2, "Two", 3);

    Assert.False(book.TryConsume(1, 4, 3));
    Assert.False(book.TryConsume(1, 2, 4));
    Assert.False(book.TryConsume(5, 2, 3));
    Assert.True(book.TryConsume(1, 2, 3));
    Assert.False(book.TryGet(1, out _));
  }

  [Fact]
  public void A_mark_pays_once()
  {
    var book = new MarkBook();
    book.Place(1, 2, "Two", 3);

    Assert.True(book.TryConsume(1, 2, 3));
    Assert.False(book.TryConsume(1, 2, 3));
  }

  [Fact]
  public void Consuming_one_mark_keeps_the_others_on_that_target()
  {
    var book = new MarkBook();
    book.Place(1, 9, "Nine", 3);
    book.Place(2, 9, "Nine", 3);

    Assert.True(book.TryConsume(1, 9, 3));
    Assert.True(book.TryGet(2, out _));
  }

  [Fact]
  public void TakeAll_returns_every_mark_and_clears()
  {
    var book = new MarkBook();
    book.Place(2, 9, "Nine", 3);
    book.Place(1, 8, "Eight", 3);

    var taken = book.TakeAll();

    Assert.Equal([1UL, 2UL], taken.Select(entry => entry.Marker));
    Assert.Equal(new Mark(8, "Eight", 3), taken[0].Mark);
    Assert.Equal(0, book.Count);
    Assert.True(book.Place(1, 9, "Nine", 4));
  }

  [Fact]
  public void Reset_clears_every_mark()
  {
    var book = new MarkBook();
    book.Place(1, 2, "Two", 3);

    book.Reset();

    Assert.Equal(0, book.Count);
    Assert.False(book.TryGet(1, out _));
  }
}
