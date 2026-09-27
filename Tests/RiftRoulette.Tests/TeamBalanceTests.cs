using RiftRoulette.Lobby;

namespace Bublock.Tests.RiftRoulette;

public class TeamBalanceTests
{
  private const int S = RiftRouletteTeams.Sapphire;
  private const int A = RiftRouletteTeams.Amber;

  [Theory]
  [InlineData(0)]
  [InlineData(1)]
  [InlineData(6)]
  [InlineData(7)]
  [InlineData(12)]
  public void Even_places_unassigned_players_and_differs_by_at_most_one(int count)
  {
    var current = Enumerable.Range(1, count).ToDictionary(i => (ulong)i, _ => 0);

    for (var seed = 0; seed < 20; seed++)
    {
      var teams = TeamBalance.Even(current, new Random(seed));
      var sapphire = teams.Values.Count(team => team == S);
      var amber = teams.Values.Count(team => team == A);

      Assert.Equal(count, teams.Count);
      Assert.Equal(count, sapphire + amber);
      Assert.InRange(Math.Abs(sapphire - amber), 0, 1);
    }
  }

  [Fact]
  public void Even_keeps_balanced_teams_unchanged()
  {
    var current = new Dictionary<ulong, int> { [1] = S, [2] = A, [3] = S, [4] = A, [5] = A };

    for (var seed = 0; seed < 10; seed++)
      Assert.Equal(current, TeamBalance.Even(current, new Random(seed)));
  }

  [Fact]
  public void Even_moves_only_as_many_players_as_needed()
  {
    var current = new Dictionary<ulong, int> { [1] = A, [2] = A, [3] = A, [4] = A, [5] = A, [6] = S };

    for (var seed = 0; seed < 10; seed++)
    {
      var teams = TeamBalance.Even(current, new Random(seed));

      Assert.Equal(3, teams.Values.Count(team => team == S));
      Assert.Equal(S, teams[6]);
      Assert.Equal(2, current.Count(pair => teams[pair.Key] != pair.Value));
    }
  }

  [Fact]
  public void Even_splits_two_left_on_one_team()
  {
    var current = new Dictionary<ulong, int> { [1] = S, [2] = S };

    for (var seed = 0; seed < 10; seed++)
    {
      var teams = TeamBalance.Even(current, new Random(seed));

      Assert.Equal(1, teams.Values.Count(team => team == S));
      Assert.Equal(1, teams.Values.Count(team => team == A));
    }
  }

  [Fact]
  public void SmallerTeam_picks_the_team_with_fewer_players()
  {
    var teams = new[] { S, S, A };

    Assert.Equal(A, TeamBalance.SmallerTeam(teams, new Random(0)));
    Assert.Equal(S, TeamBalance.SmallerTeam([A], new Random(0)));
  }

  [Fact]
  public void SmallerTeam_on_a_tie_returns_a_real_team()
  {
    for (var seed = 0; seed < 10; seed++)
    {
      var team = TeamBalance.SmallerTeam([], new Random(seed));

      Assert.True(team is S or A);
    }
  }
}
