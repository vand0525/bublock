using RiftRoulette.Balance;
using RiftRoulette.Lobby;

namespace Bublock.Tests.RiftRoulette;

public class BalancePickerTests
{
  private const int S = RiftRouletteTeams.Sapphire;
  private const int A = RiftRouletteTeams.Amber;

  [Fact]
  public void Even_teams_swap_winners_best_with_losers_weakest()
  {
    var players = new[]
    {
      new BalanceCandidate(1, S, 5),
      new BalanceCandidate(2, S, 9),
      new BalanceCandidate(3, A, -2),
      new BalanceCandidate(4, A, 1)
    };

    Assert.Equal(new BalanceMove(2, 3), BalancePicker.Pick(players, S));
  }

  [Fact]
  public void Bigger_winning_team_moves_only_its_best()
  {
    var players = new[]
    {
      new BalanceCandidate(1, A, 5),
      new BalanceCandidate(2, A, 2),
      new BalanceCandidate(3, S, 0)
    };

    Assert.Equal(new BalanceMove(1, null), BalancePicker.Pick(players, A));
  }

  [Fact]
  public void Smaller_winning_team_still_swaps()
  {
    var players = new[]
    {
      new BalanceCandidate(1, S, 4),
      new BalanceCandidate(2, A, 0),
      new BalanceCandidate(3, A, -1)
    };

    Assert.Equal(new BalanceMove(1, 3), BalancePicker.Pick(players, S));
  }

  [Fact]
  public void Too_few_players_or_empty_winning_team_gives_no_move()
  {
    Assert.Null(BalancePicker.Pick([new BalanceCandidate(1, S, 9), new BalanceCandidate(2, A, 0)], S));
    Assert.Null(BalancePicker.Pick([new BalanceCandidate(1, A, 1), new BalanceCandidate(2, A, 0), new BalanceCandidate(3, A, 0)], S));
  }

  [Fact]
  public void Ties_break_by_steam_id()
  {
    var players = new[]
    {
      new BalanceCandidate(7, S, 3),
      new BalanceCandidate(5, S, 3),
      new BalanceCandidate(9, A, 0),
      new BalanceCandidate(8, A, 0)
    };

    Assert.Equal(new BalanceMove(5, 8), BalancePicker.Pick(players, S));
  }
}
