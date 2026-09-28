using DeadworksManaged.Api;
using RiftRoulette.RandomMode;

namespace Bublock.Tests.RiftRoulette;

public class HeroBansTests
{
  private const int Sapphire = 3;
  private const int Amber = 2;
  private const ulong A = 1;
  private const ulong B = 2;
  private const ulong C = 3;

  [Fact]
  public void One_ban_per_team_and_a_teammate_is_told_the_team_ban()
  {
    var bans = new HeroBans();

    Assert.Equal(new HeroBanOutcome(HeroBanResult.Banned, Heroes.Haze, A), bans.TryBan(Sapphire, A, Heroes.Haze));
    Assert.Equal(new HeroBanOutcome(HeroBanResult.TeamAlreadyBanned, Heroes.Haze, A), bans.TryBan(Sapphire, B, Heroes.Shiv));
    Assert.Equal(1, bans.PendingCount);
  }

  [Fact]
  public void Both_teams_may_ban_the_same_hero()
  {
    var bans = new HeroBans();

    Assert.Equal(HeroBanResult.Banned, bans.TryBan(Sapphire, A, Heroes.Haze).Result);
    Assert.Equal(HeroBanResult.Banned, bans.TryBan(Amber, C, Heroes.Haze).Result);
    Assert.Equal(new HashSet<Heroes> { Heroes.Haze }, bans.Take(1));
  }

  [Fact]
  public void Take_moves_pending_into_current_once_per_round()
  {
    var bans = new HeroBans();
    bans.TryBan(Sapphire, A, Heroes.Haze);
    bans.TryBan(Amber, C, Heroes.Lash);

    Assert.Equal(new HashSet<Heroes> { Heroes.Haze, Heroes.Lash }, bans.Take(1));
    Assert.Equal(0, bans.PendingCount);

    bans.TryBan(Sapphire, B, Heroes.Shiv);

    Assert.Equal(new HashSet<Heroes> { Heroes.Haze, Heroes.Lash }, bans.Take(1));
    Assert.True(bans.TryGetPending(Sapphire, out var pending, out var by));
    Assert.Equal((Heroes.Shiv, B), (pending, by));

    Assert.Equal(new HashSet<Heroes> { Heroes.Shiv }, bans.Take(2));
    Assert.Empty(bans.Take(3));
    Assert.Empty(bans.Current);
  }

  [Fact]
  public void Pending_shows_each_team_ban()
  {
    var bans = new HeroBans();
    bans.TryBan(Amber, C, Heroes.Lash);

    Assert.Equal(new Dictionary<int, Heroes> { [Amber] = Heroes.Lash }, bans.Pending());
    Assert.False(bans.TryGetPending(Sapphire, out _, out _));
  }

  [Fact]
  public void Reset_clears_pending_current_and_the_round_cache()
  {
    var bans = new HeroBans();
    bans.TryBan(Sapphire, A, Heroes.Haze);
    bans.Take(1);
    bans.TryBan(Amber, C, Heroes.Lash);

    bans.Reset();

    Assert.Equal(0, bans.PendingCount);
    Assert.Empty(bans.Current);
    Assert.Empty(bans.Take(1));
  }

  [Theory]
  [InlineData(new[] { "Haze" }, "Banned this round: Haze")]
  [InlineData(new[] { "Haze", "Lash" }, "Banned this round: Haze, Lash")]
  public void RevealLine_lists_the_heroes_only(string[] heroes, string expected)
  {
    Assert.Equal(expected, HeroBans.RevealLine(heroes));
  }
}
