using Bublock.Modules.Queue;
using RiftRoulette.Lobby;
using RiftRoulette.RandomMode;

namespace Bublock.Tests.RiftRoulette;

public class BenchRuleTests
{
  private const int S = RiftRouletteTeams.Sapphire;
  private const int A = RiftRouletteTeams.Amber;

  private static ulong[] Ids(int count) => Enumerable.Range(1, count).Select(i => (ulong)i).ToArray();

  [Theory]
  [InlineData(2)]
  [InlineData(4)]
  [InlineData(6)]
  public void Next_sits_nobody_out_with_an_even_count(int count)
  {
    Assert.Null(BenchRule.Next(new PlayerQueue(), Ids(count)));
  }

  [Theory]
  [InlineData(0)]
  [InlineData(1)]
  public void Next_sits_nobody_out_below_three_players(int count)
  {
    Assert.Null(BenchRule.Next(new PlayerQueue(), Ids(count)));
  }

  [Fact]
  public void Next_rotates_through_everyone_before_repeating()
  {
    var rotation = new PlayerQueue();
    var players = Ids(5);

    var benched = Enumerable.Range(0, 5).Select(_ => BenchRule.Next(rotation, players)!.Value).ToList();

    Assert.Equal(players.OrderBy(id => id), benched.OrderBy(id => id));
    Assert.Equal(benched[0], BenchRule.Next(rotation, players));
  }

  [Fact]
  public void Next_drops_players_who_left_and_puts_new_players_at_the_back()
  {
    var rotation = new PlayerQueue();

    Assert.Equal(1UL, BenchRule.Next(rotation, Ids(3)));

    var benched = BenchRule.Next(rotation, new ulong[] { 2, 3, 4, 5, 9 });

    Assert.Equal(2UL, benched);
    Assert.DoesNotContain(1UL, rotation.Items);
    Assert.Equal(new ulong[] { 3, 4, 5, 9, 2 }, rotation.Items);
  }

  [Fact]
  public void FightingTeams_leaves_out_the_bench_and_splits_evenly()
  {
    var teams = new Dictionary<ulong, int> { [1] = S, [2] = S, [3] = S, [4] = A, [5] = A };

    for (var seed = 0; seed < 10; seed++)
    {
      var fighters = BenchRule.FightingTeams(teams, 1, null, new Random(seed));

      Assert.Equal(4, fighters.Count);
      Assert.DoesNotContain(1UL, fighters.Keys);
      Assert.Equal(2, fighters.Values.Count(team => team == S));
      Assert.Equal(2, fighters.Values.Count(team => team == A));
      Assert.Equal(S, fighters[2]);
      Assert.Equal(A, fighters[4]);
    }
  }

  [Fact]
  public void FightingTeams_puts_the_returner_in_the_gap_the_bench_left()
  {
    // Player 1 sat out last round (still listed on Sapphire); player 4 sits out now, leaving Amber one short.
    var teams = new Dictionary<ulong, int> { [1] = S, [2] = S, [3] = S, [4] = A, [5] = A };

    for (var seed = 0; seed < 10; seed++)
    {
      var fighters = BenchRule.FightingTeams(teams, 4, 1, new Random(seed));

      Assert.Equal(A, fighters[1]);
      Assert.Equal(S, fighters[2]);
      Assert.Equal(S, fighters[3]);
      Assert.Equal(A, fighters[5]);
    }
  }
}
