using RiftRoulette.Betting;
using RiftRoulette.Lobby;

namespace Bublock.Tests.RiftRoulette;

public class BetBookTests
{
  private const int S = RiftRouletteTeams.Sapphire;
  private const int A = RiftRouletteTeams.Amber;
  private static readonly int[] Both = [S, A];

  [Fact]
  public void New_players_start_with_the_starting_chips()
  {
    var book = new BetBook();

    Assert.Equal(BetBook.StartingChips, book.Chips(1));
    Assert.Equal(BetBook.StartingChips, book.Total(1));
  }

  [Fact]
  public void TrySpend_takes_only_unstaked_chips()
  {
    var book = new BetBook();

    for (var kill = 0; kill < 10; kill++)
      book.AwardKill(1);

    Assert.True(book.TrySpend(1, 1000));
    Assert.Equal(100, book.Chips(1));
    Assert.False(book.TrySpend(1, 1000));
    Assert.False(book.TrySpend(1, 0));
    Assert.Equal(100, book.Chips(1));

    book.Place(1, S, Both);

    Assert.False(book.TrySpend(1, 100));
    Assert.Equal(100, book.Total(1));
  }

  [Fact]
  public void A_kill_adds_chips()
  {
    var book = new BetBook();

    Assert.Equal(200, book.AwardKill(1));
    Assert.Equal(300, book.AwardKill(1));
    Assert.Equal(300, book.Chips(1));
  }

  [Fact]
  public void An_assist_adds_half_a_kill()
  {
    var book = new BetBook();

    Assert.Equal(150, book.AwardAssist(1));
    Assert.Equal(250, book.AwardKill(1));
    Assert.Equal(300, book.AwardAssist(1));
  }

  [Fact]
  public void An_assist_during_a_bet_adds_unstaked_chips()
  {
    var book = new BetBook();
    book.Place(1, S, Both);

    Assert.Equal(50, book.AwardAssist(1));
    Assert.Equal(150, book.Total(1));
  }

  [Fact]
  public void Place_stakes_every_chip()
  {
    var book = new BetBook();
    book.AwardKill(1);

    Assert.Equal(BetResult.Placed, book.Place(1, A, Both));
    Assert.Equal(0, book.Chips(1));
    Assert.Equal(200, book.Total(1));
    Assert.True(book.TryGetBet(1, out var bet));
    Assert.Equal(new Bet(A, 200), bet);
  }

  [Fact]
  public void Place_refuses_a_team_that_is_not_allowed()
  {
    var book = new BetBook();

    Assert.Equal(BetResult.NotAllowed, book.Place(1, A, [S]));
    Assert.Equal(0, book.OpenBets);
    Assert.Equal(BetBook.StartingChips, book.Chips(1));
  }

  [Fact]
  public void Place_refuses_with_no_chips()
  {
    var book = new BetBook();
    book.Place(1, A, Both);
    book.Settle(S);

    Assert.Equal(0, book.Chips(1));
    Assert.Equal(BetResult.NoChips, book.Place(1, A, Both));
  }

  [Fact]
  public void A_second_bet_only_changes_the_team()
  {
    var book = new BetBook();

    book.Place(1, A, Both);
    Assert.Equal(BetResult.Changed, book.Place(1, S, Both));
    Assert.True(book.TryGetBet(1, out var bet));
    Assert.Equal(new Bet(S, 100), bet);
  }

  [Fact]
  public void Settle_doubles_winners_takes_losers_and_clears_bets()
  {
    var book = new BetBook();
    book.AwardKill(2);

    book.Place(1, A, Both);
    book.Place(2, S, Both);

    var settled = book.Settle(A);

    Assert.Equal(BetOutcome.Won, settled.Single(s => s.SteamId == 1).Outcome);
    Assert.Equal(200, book.Chips(1));
    Assert.Equal(BetOutcome.Lost, settled.Single(s => s.SteamId == 2).Outcome);
    Assert.Equal(0, book.Chips(2));
    Assert.Equal(0, book.OpenBets);
  }

  [Fact]
  public void Settle_with_no_winner_refunds()
  {
    var book = new BetBook();
    book.Place(1, A, Both);

    var settled = book.Settle(null);

    Assert.Equal(BetOutcome.Refunded, settled.Single().Outcome);
    Assert.Equal(100, settled.Single().Balance);
    Assert.Equal(100, book.Chips(1));
  }

  [Fact]
  public void Reset_gives_everyone_the_starting_chips_again()
  {
    var book = new BetBook();
    book.AwardKill(1);
    book.Place(2, A, Both);

    book.Reset();

    Assert.Equal(BetBook.StartingChips, book.Chips(1));
    Assert.Equal(BetBook.StartingChips, book.Chips(2));
    Assert.Equal(0, book.OpenBets);
  }
}
