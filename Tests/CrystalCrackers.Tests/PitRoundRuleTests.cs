using System.Numerics;
using CrystalCrackers.Pit;

namespace Bublock.Tests.CrystalCrackers;

public class PitRoundRuleTests
{
  private const int Amber = PitRoundRule.AmberTeam;
  private const int Sapphire = PitRoundRule.SapphireTeam;

  [Theory]
  [InlineData(Amber, Sapphire)]
  [InlineData(Sapphire, Amber)]
  [InlineData(1, 0)]
  [InlineData(0, 0)]
  public void Other_swaps_the_two_teams(int team, int expected) =>
    Assert.Equal(expected, PitRoundRule.Other(team));

  [Theory]
  [InlineData("amber", Amber)]
  [InlineData(" Sapphire ", Sapphire)]
  [InlineData("AMBER", Amber)]
  public void TryParseTeam_reads_team_names(string text, int expected)
  {
    Assert.True(PitRoundRule.TryParseTeam(text, out var team));
    Assert.Equal(expected, team);
  }

  [Theory]
  [InlineData("")]
  [InlineData("2")]
  [InlineData("spectator")]
  public void TryParseTeam_refuses_anything_else(string text) =>
    Assert.False(PitRoundRule.TryParseTeam(text, out _));

  [Theory]
  [InlineData(0, 0, Amber)]
  [InlineData(0, Amber, Sapphire)]
  [InlineData(0, Sapphire, Amber)]
  [InlineData(Amber, Amber, Amber)]
  [InlineData(Sapphire, 0, Sapphire)]
  public void PitTeam_alternates_unless_chosen(int chosen, int last, int expected) =>
    Assert.Equal(expected, PitRoundRule.PitTeam(chosen, last));

  [Theory]
  [InlineData(PitPhase.Idle, false)]
  [InlineData(PitPhase.Countdown, true)]
  [InlineData(PitPhase.Live, true)]
  [InlineData(PitPhase.BossDown, true)]
  public void BlocksSouls_outside_idle(PitPhase phase, bool expected) =>
    Assert.Equal(expected, PitRoundRule.BlocksSouls(phase));

  [Theory]
  [InlineData(PitEndReason.WindowOver, Sapphire, Amber, Sapphire)]
  [InlineData(PitEndReason.WindowOver, 0, Amber, Amber)]
  [InlineData(PitEndReason.WindowOver, 0, 0, 0)]
  [InlineData(PitEndReason.WindowOver, 0, 4, 0)]
  [InlineData(PitEndReason.TimeLimit, Sapphire, Amber, 0)]
  [InlineData(PitEndReason.Admin, Sapphire, Amber, 0)]
  public void Winner_is_the_crystal_team_then_the_killer(PitEndReason reason, int crystal, int killer, int expected) =>
    Assert.Equal(expected, PitRoundRule.Winner(reason, crystal, killer));

  [Theory]
  [InlineData(PitPhase.Live, true, true, false, RespawnAction.SendUp)]
  [InlineData(PitPhase.BossDown, true, true, false, RespawnAction.SendUp)]
  [InlineData(PitPhase.Live, true, true, true, RespawnAction.KeepIn)]
  [InlineData(PitPhase.BossDown, true, true, true, RespawnAction.KeepIn)]
  [InlineData(PitPhase.Live, true, false, false, RespawnAction.None)]
  [InlineData(PitPhase.Live, false, false, false, RespawnAction.SendUp)]
  [InlineData(PitPhase.BossDown, false, true, true, RespawnAction.SendUp)]
  [InlineData(PitPhase.Countdown, true, false, false, RespawnAction.SendUp)]
  [InlineData(PitPhase.Countdown, false, true, true, RespawnAction.SendUp)]
  public void OnRespawn_sends_the_dead_up_unless_reborn(PitPhase phase, bool fighter, bool died, bool reborn, RespawnAction expected) =>
    Assert.Equal(expected, PitRoundRule.OnRespawn(phase, fighter, died, reborn, roundsPlayed: true));

  [Theory]
  [InlineData(false, RespawnAction.None)]
  [InlineData(true, RespawnAction.SendUp)]
  public void OnRespawn_in_idle_sends_up_once_rounds_were_played(bool played, RespawnAction expected) =>
    Assert.Equal(expected, PitRoundRule.OnRespawn(PitPhase.Idle, true, true, true, played));

  [Theory]
  [InlineData(Amber, Amber, true)]
  [InlineData(Sapphire, Amber, false)]
  [InlineData(1, 1, false)]
  public void StartsInPit_only_for_the_pit_team(int team, int pitTeam, bool expected) =>
    Assert.Equal(expected, PitRoundRule.StartsInPit(team, pitTeam));

  [Fact]
  public void Score_shows_both_teams()
  {
    Assert.Equal("Sapphire 0 - Amber 0", PitRoundRule.Score(new Dictionary<int, int>()));
    Assert.Equal("Sapphire 2 - Amber 1", PitRoundRule.Score(new Dictionary<int, int> { [Sapphire] = 2, [Amber] = 1 }));
  }

  [Fact]
  public void IsFarFromPit_ignores_height_and_spares_the_lane_starts()
  {
    Assert.False(PitRoundRule.IsFarFromPit(new Vector3(0f, 0f, -768f)));
    Assert.False(PitRoundRule.IsFarFromPit(new Vector3(0f, -2000f, 384f)));
    Assert.True(PitRoundRule.IsFarFromPit(new Vector3(0f, 8000f, 600f)));
    Assert.True(PitRoundRule.IsFarFromPit(new Vector3(2500f, 2500f, 0f)));
  }

  [Theory]
  [InlineData("20", 5, 120, true, 20)]
  [InlineData(" 5 ", 5, 120, true, 5)]
  [InlineData("4", 5, 120, false, 4)]
  [InlineData("121", 5, 120, false, 121)]
  [InlineData("ten", 5, 120, false, 0)]
  [InlineData("2.5", 5, 120, false, 0)]
  public void TryParseSeconds_checks_the_range(string text, int min, int max, bool ok, int value)
  {
    Assert.Equal(ok, PitRoundRule.TryParseSeconds(text, min, max, out var seconds));
    Assert.Equal(value, seconds);
  }
}
