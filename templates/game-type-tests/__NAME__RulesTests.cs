using Bublock.Modules.Arena;
using Bublock.Modules.Session;

namespace Bublock.Tests.__NAME__;

public class __NAME__RulesTests
{
  [Theory]
  [InlineData(10UL, 20UL, 2, 3, true)]
  [InlineData(10UL, 10UL, 2, 2, false)]
  [InlineData(10UL, 20UL, 2, 2, false)]
  public void Enemy_kills_score(ulong attacker, ulong victim, int attackerTeam, int victimTeam, bool credited)
  {
    Assert.Equal(credited, global::__NAME__.__NAME__Rules.Credits(attacker, victim, attackerTeam, victimTeam));
  }

  [Fact]
  public void Result_banner_names_the_winner()
  {
    var (title, _) = global::__NAME__.__NAME__Rules.ResultBanner(new MatchResult(1, [(10, 3)], [10]), _ => "Ann", 5);
    Assert.Equal("Ann wins", title);
  }

  [Fact]
  public void Embedded_arena_loads_with_a_spot_per_slot_inside_its_bounds()
  {
    var arena = ArenaSpots.Load(typeof(__NAME__RulesTests).Assembly, global::__NAME__.__NAME__Rules.ArenaResource);

    for (var slot = 0; slot < arena.Offsets.Count; slot++)
    {
      Assert.True(arena.Contains(arena.For(2, slot)!.Position));
      Assert.True(arena.Contains(arena.For(3, slot)!.Position));
    }
  }
}
