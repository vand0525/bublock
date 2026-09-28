using DeadworksManaged.Api;
using RiftRoulette.RandomMode;

namespace Bublock.Tests.RiftRoulette;

public class HeroReservationsTests
{
  private const ulong A = 1;
  private const ulong B = 2;
  private const ulong C = 3;

  [Fact]
  public void First_reservation_holds_and_a_player_gets_only_one()
  {
    var book = new HeroReservations();

    Assert.Equal(ReserveResult.Holding, book.TryReserve(A, Heroes.Haze).Result);

    var second = book.TryReserve(A, Heroes.Shiv);

    Assert.Equal(ReserveResult.AlreadyHasOne, second.Result);
    Assert.Equal(Heroes.Haze, second.Hero);
    Assert.Equal(1, book.Count);
  }

  [Fact]
  public void Later_reservations_on_the_same_hero_wait_with_place_and_rounds_ahead()
  {
    var book = new HeroReservations();
    book.TryReserve(A, Heroes.Haze);

    var b = book.TryReserve(B, Heroes.Haze);
    var c = book.TryReserve(C, Heroes.Haze);

    Assert.Equal(new ReserveOutcome(ReserveResult.Waiting, Heroes.Haze, 1, 3, A), b);
    Assert.Equal(new ReserveOutcome(ReserveResult.Waiting, Heroes.Haze, 2, 6, A), c);
    Assert.Equal(new ReservationPosition(Heroes.Haze, 2, 6, 3, A), book.Position(C));
  }

  [Fact]
  public void Take_counts_down_once_per_round_and_a_reroll_does_not_count_twice()
  {
    var book = new HeroReservations();
    book.TryReserve(A, Heroes.Haze);

    Assert.Equal([new ReservedTurn(A, Heroes.Haze, 1)], book.Take([A, B], 1));
    Assert.Equal([new ReservedTurn(A, Heroes.Haze, 1)], book.Take([A, B], 1));
    Assert.Equal(2, book.Position(A)!.RoundsLeft);

    Assert.Equal([new ReservedTurn(A, Heroes.Haze, 2)], book.Take([A, B], 2));
    Assert.Equal([new ReservedTurn(A, Heroes.Haze, 3)], book.Take([A, B], 3));
    Assert.Null(book.Position(A));
    Assert.Empty(book.Take([A, B], 4));
  }

  [Fact]
  public void A_benched_holder_keeps_their_rounds_and_the_next_fighter_in_line_plays_it()
  {
    var book = new HeroReservations();
    book.TryReserve(A, Heroes.Haze);
    book.TryReserve(B, Heroes.Haze);

    Assert.Equal([new ReservedTurn(B, Heroes.Haze, 1)], book.Take([B, C], 1));
    Assert.Equal(3, book.Position(A)!.RoundsLeft);
    Assert.Equal(2, book.Position(B)!.RoundsLeft);

    Assert.Equal([new ReservedTurn(A, Heroes.Haze, 1)], book.Take([A, B, C], 2));
  }

  [Fact]
  public void The_holder_finishing_hands_the_hero_to_the_next_in_line()
  {
    var book = new HeroReservations();
    book.TryReserve(A, Heroes.Haze);
    book.TryReserve(B, Heroes.Haze);

    for (var round = 1; round <= HeroReservations.Rounds; round++)
      Assert.Equal(A, Assert.Single(book.Take([A, B], round)).SteamId);

    Assert.Equal(new ReservationPosition(Heroes.Haze, 0, 0, 3, B), book.Position(B));
    Assert.Equal([new ReservedTurn(B, Heroes.Haze, 1)], book.Take([A, B], 4));
  }

  [Fact]
  public void Never_two_players_on_one_hero_in_a_round()
  {
    var book = new HeroReservations();
    book.TryReserve(A, Heroes.Haze);
    book.TryReserve(B, Heroes.Haze);
    book.TryReserve(C, Heroes.Shiv);

    var turns = book.Take([A, B, C], 1);

    Assert.Equal(2, turns.Count);
    Assert.Equal(turns.Count, turns.Select(turn => turn.Hero).Distinct().Count());
    Assert.DoesNotContain(turns, turn => turn.SteamId == B);
  }

  [Fact]
  public void TakeLate_gives_the_hero_only_when_nobody_plays_it_this_round()
  {
    var book = new HeroReservations();
    book.TryReserve(A, Heroes.Haze);
    book.TryReserve(B, Heroes.Shiv);

    Assert.Null(book.TakeLate(A, 1, new HashSet<Heroes>()));

    book.Take([C], 1);

    Assert.Null(book.TakeLate(A, 1, new HashSet<Heroes> { Heroes.Haze }));
    Assert.Equal(new ReservedTurn(B, Heroes.Shiv, 1), book.TakeLate(B, 1, new HashSet<Heroes>()));
    Assert.Equal(new ReservedTurn(B, Heroes.Shiv, 1), book.TakeLate(B, 1, new HashSet<Heroes>()));
    Assert.Equal(2, book.Position(B)!.RoundsLeft);
  }

  [Fact]
  public void Reset_clears_every_line()
  {
    var book = new HeroReservations();
    book.TryReserve(A, Heroes.Haze);
    book.Take([A], 1);

    book.Reset();

    Assert.Equal(0, book.Count);
    Assert.Null(book.Position(A));
    Assert.Empty(book.Take([A], 1));
  }

  [Fact]
  public void A_banned_hero_uses_the_front_fighters_round_without_a_turn()
  {
    var book = new HeroReservations();
    book.TryReserve(A, Heroes.Haze);
    book.TryReserve(B, Heroes.Haze);
    book.TryReserve(C, Heroes.Shiv);

    var turns = book.Take([A, B, C], 1, new HashSet<Heroes> { Heroes.Haze });

    Assert.Equal([new ReservedTurn(C, Heroes.Shiv, 1)], turns);
    Assert.Equal([new ReservedTurn(A, Heroes.Haze, 1)], book.Burned);
    Assert.Equal(2, book.Position(A)!.RoundsLeft);
    Assert.Equal(3, book.Position(B)!.RoundsLeft);

    Assert.Equal([new ReservedTurn(A, Heroes.Haze, 2)], book.Take([A, B, C], 2).Where(turn => turn.Hero == Heroes.Haze));
    Assert.Empty(book.Burned);
  }

  [Fact]
  public void A_ban_on_the_last_round_ends_the_reservation()
  {
    var book = new HeroReservations();
    book.TryReserve(A, Heroes.Haze);
    book.Take([A], 1);
    book.Take([A], 2);

    book.Take([A], 3, new HashSet<Heroes> { Heroes.Haze });

    Assert.Equal([new ReservedTurn(A, Heroes.Haze, 3)], book.Burned);
    Assert.Null(book.Position(A));
  }

  [Fact]
  public void TakeLate_refuses_a_banned_hero()
  {
    var book = new HeroReservations();
    book.TryReserve(A, Heroes.Haze);
    book.Take([B], 1);

    Assert.Null(book.TakeLate(A, 1, new HashSet<Heroes>(), new HashSet<Heroes> { Heroes.Haze }));
    Assert.Equal(3, book.Position(A)!.RoundsLeft);
  }

  [Theory]
  [InlineData(1, 3, "Someone has reserved Haze. When their 3 rounds are done, it will be your turn.")]
  [InlineData(1, 1, "Someone has reserved Haze. When their 1 round is done, it will be your turn.")]
  [InlineData(2, 5, "2 players are ahead of you for Haze (5 rounds). Then it will be your turn.")]
  public void WaitingLine_never_names_the_holder(int ahead, int roundsAhead, string expected)
  {
    Assert.Equal(expected, HeroReservations.WaitingLine("Haze", ahead, roundsAhead));
  }

  [Fact]
  public void BurnedLine_says_which_round_was_used()
  {
    Assert.Equal(
      "Your reserved Haze was banned this round (round 2 of 3 used).",
      HeroReservations.BurnedLine("Haze", 2));
  }

  [Theory]
  [InlineData(1, "Your reserved hero is up: Haze (round 1 of 3).")]
  [InlineData(2, "Reserved hero: Haze (round 2 of 3).")]
  public void TurnLine_says_which_use_this_is(int use, string expected)
  {
    Assert.Equal(expected, HeroReservations.TurnLine("Haze", use));
  }
}
