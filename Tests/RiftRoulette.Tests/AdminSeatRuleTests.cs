using RiftRoulette.Lobby;

namespace Bublock.Tests.RiftRoulette;

public class AdminSeatRuleTests
{
  [Theory]
  [InlineData(false, 0, true)]
  [InlineData(false, 11, true)]
  [InlineData(false, 12, false)]
  [InlineData(true, 12, true)]
  [InlineData(true, 13, true)]
  public void CanConnect_refuses_non_admins_at_the_cap(bool isAdmin, int playing, bool expected)
  {
    Assert.Equal(expected, AdminSeatRule.CanConnect(isAdmin, playing));
  }

  [Theory]
  [InlineData(11, true)]
  [InlineData(12, false)]
  public void CanStand_needs_a_free_player_slot(int playing, bool expected)
  {
    Assert.Equal(expected, AdminSeatRule.CanStand(playing));
  }

  [Theory]
  [InlineData(AdminMode.Spectate)]
  [InlineData(AdminMode.Roam)]
  [InlineData(AdminMode.Play)]
  public void JoinMode_keeps_the_admins_last_mode(AdminMode last)
  {
    Assert.Equal(last, AdminSeatRule.JoinMode(true, 4, last));
  }

  [Fact]
  public void JoinMode_plays_with_no_last_mode()
  {
    Assert.Equal(AdminMode.Play, AdminSeatRule.JoinMode(true, 0, null));
    Assert.Equal(AdminMode.Play, AdminSeatRule.JoinMode(true, 11, null));
  }

  [Theory]
  [InlineData(null, AdminMode.Spectate)]
  [InlineData(AdminMode.Play, AdminMode.Spectate)]
  [InlineData(AdminMode.Spectate, AdminMode.Spectate)]
  [InlineData(AdminMode.Roam, AdminMode.Roam)]
  public void JoinMode_never_plays_as_the_13th_connection(AdminMode? last, AdminMode expected)
  {
    Assert.Equal(expected, AdminSeatRule.JoinMode(true, 12, last));
  }

  [Fact]
  public void JoinMode_always_plays_for_a_non_admin()
  {
    Assert.Equal(AdminMode.Play, AdminSeatRule.JoinMode(false, 12, AdminMode.Spectate));
  }

  [Theory]
  [InlineData(0, true)]
  [InlineData(1, false)]
  [InlineData(12, false)]
  public void ShouldRoam_only_on_an_empty_server(int participants, bool expected)
  {
    Assert.Equal(expected, AdminSeatRule.ShouldRoam(participants));
  }

  [Fact]
  public void Custom_cap_is_respected()
  {
    Assert.False(AdminSeatRule.CanConnect(false, 2, cap: 2));
    Assert.True(AdminSeatRule.CanStand(1, cap: 2));
    Assert.Equal(12, AdminSeatRule.PlayerCap);
  }
}
