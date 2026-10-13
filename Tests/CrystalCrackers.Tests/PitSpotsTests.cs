using System.Numerics;
using CrystalCrackers.Pit;

namespace Bublock.Tests.CrystalCrackers;

public class PitSpotsTests
{
  private const int Amber = PitRoundRule.AmberTeam;
  private const int Sapphire = PitRoundRule.SapphireTeam;

  [Fact]
  public void Every_table_has_a_spot_per_slot()
  {
    var offsets = PitSpots.Offsets;

    Assert.Equal(13, offsets.Watch.Count);
    Assert.Equal(13, offsets.Pit.Count);
    Assert.Equal(13, offsets.Lane.Count);
  }

  [Fact]
  public void Pit_spots_are_on_the_pit_floor_and_on_their_team_end()
  {
    for (var slot = 0; slot < 13; slot++)
    {
      var sapphire = PitSpots.Pit(Sapphire, slot)!;
      var amber = PitSpots.Pit(Amber, slot)!;

      Assert.Equal(-768f, sapphire.Position.Z, 1);
      Assert.True(sapphire.Position.Y > 300f, $"slot {slot} y {sapphire.Position.Y}");
      Assert.True(amber.Position.Y < -300f, $"slot {slot} y {amber.Position.Y}");
      Assert.True(new Vector2(sapphire.Position.X, sapphire.Position.Y).Length() < 700f);
    }
  }

  [Fact]
  public void Amber_spots_mirror_sapphire_spots()
  {
    for (var slot = 0; slot < 13; slot++)
    {
      var sapphire = PitSpots.Pit(Sapphire, slot)!.Position;
      var amber = PitSpots.Pit(Amber, slot)!.Position;

      Assert.Equal(-sapphire.X, amber.X, 1);
      Assert.Equal(-sapphire.Y, amber.Y, 1);
    }
  }

  [Fact]
  public void Pit_spots_are_at_least_65_apart()
  {
    var spots = Enumerable.Range(0, 13).Select(slot => PitSpots.Pit(Sapphire, slot)!.Position).ToList();

    for (var i = 0; i < spots.Count; i++)
    for (var j = i + 1; j < spots.Count; j++)
      Assert.True(Vector3.Distance(spots[i], spots[j]) >= 65f, $"slots {i} and {j}");
  }

  [Fact]
  public void Lane_spots_are_on_each_team_side_of_mid()
  {
    Assert.Equal(new Vector3(0f, 2000f, 384f), PitSpots.Lane(Sapphire, 0)!.Position);
    Assert.Equal(new Vector3(0f, -2000f, 384f), PitSpots.Lane(Amber, 0)!.Position);
  }

  [Fact]
  public void Other_teams_have_no_spot()
  {
    Assert.Null(PitSpots.Pit(1, 0));
    Assert.Null(PitSpots.Lane(4, 0));
  }

  [Theory]
  [InlineData(13, 0)]
  [InlineData(-1, 12)]
  [InlineData(5, 5)]
  public void Index_wraps(int slot, int expected) =>
    Assert.Equal(expected, PitSpots.Index(slot, 13));

  [Fact]
  public void Parse_needs_every_table()
  {
    Assert.Throws<InvalidOperationException>(() => PitSpots.Parse("""{ "watch": [[0,0,0]], "pit": [[0,0,0]] }"""));
    Assert.Throws<InvalidOperationException>(() => PitSpots.Parse("""{ "watch": [[0,0]], "pit": [[0,0,0]], "lane": [[0,0,0]] }"""));
  }
}
