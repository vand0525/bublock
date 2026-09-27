using RiftRoulette.Lobby;

namespace Bublock.Tests.RiftRoulette;

public class RiftRouletteTeamsTests
{
  [Theory]
  [InlineData("sapphire", 3)]
  [InlineData("Sapphire", 3)]
  [InlineData("AMBER", 2)]
  public void TryParse_ignores_case(string name, int expected)
  {
    Assert.True(RiftRouletteTeams.TryParse(name, out var team));
    Assert.Equal(expected, team);
  }

  [Theory]
  [InlineData("")]
  [InlineData("green")]
  [InlineData("3")]
  public void TryParse_rejects_unknown_names(string name)
  {
    Assert.False(RiftRouletteTeams.TryParse(name, out var team));
    Assert.Equal(0, team);
  }

  [Theory]
  [InlineData(3, "Sapphire")]
  [InlineData(2, "Amber")]
  [InlineData(1, "Team1")]
  public void Name_maps_team_numbers(int team, string expected)
  {
    Assert.Equal(expected, RiftRouletteTeams.Name(team));
  }
}
