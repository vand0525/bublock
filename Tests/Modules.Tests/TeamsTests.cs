using Bublock.Modules.Teams;

namespace Bublock.Tests.Modules;

public class TeamsTests
{
  [Fact]
  public void SmallerTeam_fills_the_team_with_fewer_players_and_ignores_others()
  {
    var rng = new Random(1);

    Assert.Equal(DeadlockTeams.Sapphire, DeadlockTeams.SmallerTeam([2, 2, 3, 1, 1], rng));
    Assert.Equal(DeadlockTeams.Amber, DeadlockTeams.SmallerTeam([3, 3, 2], rng));
  }

  [Fact]
  public void SmallerTeam_on_a_tie_picks_a_playable_team()
  {
    var picks = Enumerable.Range(0, 50).Select(seed => DeadlockTeams.SmallerTeam([2, 3], new Random(seed))).ToHashSet();

    Assert.Equal(new HashSet<int> { DeadlockTeams.Amber, DeadlockTeams.Sapphire }, picks);
  }

  [Fact]
  public void Names_and_other_team()
  {
    Assert.Equal("Amber", DeadlockTeams.Name(2));
    Assert.Equal("Sapphire", DeadlockTeams.Name(3));
    Assert.Equal("Spectator", DeadlockTeams.Name(1));
    Assert.Equal("Team 7", DeadlockTeams.Name(7));
    Assert.Equal(DeadlockTeams.Amber, DeadlockTeams.Other(DeadlockTeams.Sapphire));
    Assert.False(DeadlockTeams.IsPlayable(1));
  }

  [Theory]
  [InlineData("selecthero", true, false, true)]
  [InlineData(" SelectHero ", true, false, true)]
  [InlineData("selecthero", false, true, false)]
  [InlineData("changeteam", false, true, true)]
  [InlineData("jointeam", false, true, true)]
  [InlineData("jointeam", true, false, false)]
  [InlineData("say", true, true, false)]
  public void ChoiceGuard_blocks_only_what_is_locked(string command, bool heroLocked, bool teamLocked, bool blocked)
  {
    Assert.Equal(blocked, ChoiceGuard.Blocks(command, heroLocked, teamLocked));
  }
}
