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
  [InlineData(true, true)]
  [InlineData(false, false)]
  public void SeatOnJoin_seats_every_admin(bool isAdmin, bool expected)
  {
    Assert.Equal(expected, AdminSeatRule.SeatOnJoin(isAdmin));
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
