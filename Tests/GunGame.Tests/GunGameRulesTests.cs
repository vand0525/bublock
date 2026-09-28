using Bublock.Modules.Arena;
using Bublock.Modules.Session;
using GunGame;

namespace Bublock.Tests.GunGame;

public class GunGameRulesTests
{
  [Theory]
  [InlineData(10UL, 20UL, 2, 3, true)]
  [InlineData(10UL, 10UL, 2, 2, false)]
  [InlineData(10UL, 20UL, 2, 2, false)]
  [InlineData(0UL, 20UL, 2, 3, false)]
  [InlineData(10UL, 20UL, 1, 3, false)]
  public void Only_enemy_kills_by_a_player_on_a_team_score(ulong attacker, ulong victim, int attackerTeam, int victimTeam, bool credited)
  {
    Assert.Equal(credited, GunGameRules.Credits(attacker, victim, attackerTeam, victimTeam));
  }

  [Fact]
  public void Session_is_two_minute_matches_for_two_or_more_players()
  {
    Assert.Equal(120, GunGameRules.Session.MatchSeconds);
    Assert.Equal(2, GunGameRules.Session.MinPlayers);
    Assert.Equal(("Gun Game", "2:00 - most kills wins"), GunGameRules.StartBanner(120));
  }

  [Fact]
  public void Kill_and_hero_banners_show_the_build_and_souls()
  {
    Assert.Equal(("1 kill: Haze", "Bullet - 12,345 souls"), GunGameRules.KillBanner(1, "Haze", "Bullet", 12345));
    Assert.Equal(("3 kills: Haze", "Bullet - 900 souls"), GunGameRules.KillBanner(3, "Haze", "Bullet", 900));
    Assert.Equal(("Haze", "Bullet - 900 souls"), GunGameRules.HeroBanner("Haze", "Bullet", 900));
  }

  [Fact]
  public void Result_banner_names_the_winner_ties_or_nobody()
  {
    string Name(ulong id) => id == 10 ? "Ann" : "Bo";

    Assert.Equal(("Ann wins", "7 kills - next match in 5s"),
      GunGameRules.ResultBanner(new MatchResult(1, [(10, 7), (20, 3)], [10]), Name, 5));
    Assert.Equal(("Tie: Ann, Bo", "5 kills each - next match in 5s"),
      GunGameRules.ResultBanner(new MatchResult(1, [(10, 5), (20, 5)], [10, 20]), Name, 5));
    Assert.Equal(("Match over", "No kills - next match in 5s"),
      GunGameRules.ResultBanner(new MatchResult(1, [], []), Name, 5));
  }

  [Fact]
  public void Embedded_arena_is_the_mid_lane_brawl_with_bounds_and_a_spot_per_slot()
  {
    var arena = ArenaSpots.Load(typeof(GunGameRulesTests).Assembly, GunGameRules.ArenaResource);

    Assert.Equal("mid lane brawl", arena.Name);
    Assert.Equal(4, arena.ActiveLane);
    Assert.Equal(13, arena.Offsets.Count);
    Assert.NotNull(arena.Bounds);

    for (var slot = 0; slot < 13; slot++)
    {
      Assert.True(arena.Contains(arena.For(2, slot)!.Position));
      Assert.True(arena.Contains(arena.For(3, slot)!.Position));
    }
  }
}
