using System.Numerics;
using Bublock.Modules.Arena;

namespace Bublock.Tests.Modules;

public class ArenaSpotsTests
{
  private const string Json = """
    {
      "name": "test lane",
      "active_lane": 4,
      "amber": { "position": [0, -2000, 376], "angle": [0, 90, 0] },
      "sapphire": { "position": [0, 2000, 376], "angle": [0, -90, 0] },
      "bounds": { "min": [-1500, -2800, -900], "max": [1500, 2800, 1600] },
      "offsets": [[0, 0, 0], [0, 100, 0], [-100, 0, 0]]
    }
    """;

  [Fact]
  public void Parse_reads_anchors_offsets_bounds_and_lane()
  {
    var arena = ArenaSpots.Parse(Json);

    Assert.Equal("test lane", arena.Name);
    Assert.Equal(new Vector3(0, -2000, 376), arena.Amber.Position);
    Assert.Equal(new Vector3(0, 2000, 376), arena.Sapphire.Position);
    Assert.Equal(3, arena.Offsets.Count);
    Assert.Equal(4, arena.ActiveLane);
    Assert.NotNull(arena.Bounds);
  }

  [Fact]
  public void For_turns_each_slot_offset_by_the_team_anchor_and_wraps_slots()
  {
    var arena = ArenaSpots.Parse(Json);

    var amberRight = arena.For(2, 1)!;
    var sapphireRight = arena.For(3, 1)!;
    var amberBack = arena.For(2, 2)!;

    Assert.Equal(100f, amberRight.Position.X, 3);
    Assert.Equal(-100f, sapphireRight.Position.X, 3);
    Assert.Equal(-2100f, amberBack.Position.Y, 3);
    Assert.Equal(arena.For(2, 0)!.Position, arena.For(2, 3)!.Position);
    Assert.Equal(arena.For(2, 2)!.Position, arena.For(2, -1)!.Position);
    Assert.Null(arena.For(1, 0));
  }

  [Fact]
  public void Contains_uses_the_bounds_box_and_everything_is_inside_without_one()
  {
    var arena = ArenaSpots.Parse(Json);

    Assert.True(arena.Contains(new Vector3(0, 0, 0)));
    Assert.False(arena.Contains(new Vector3(0, 3000, 376)));
    Assert.False(arena.Contains(new Vector3(0, 0, -1000)));

    var open = ArenaSpots.Parse(Json.Replace("\"bounds\": { \"min\": [-1500, -2800, -900], \"max\": [1500, 2800, 1600] },", ""));
    Assert.Null(open.Bounds);
    Assert.True(open.Contains(new Vector3(99999, 0, 0)));
  }

  [Fact]
  public void Parse_rejects_a_row_that_is_not_three_numbers()
  {
    Assert.Throws<InvalidOperationException>(() => ArenaSpots.Parse(Json.Replace("[-100, 0, 0]", "[-100, 0]")));
  }
}
