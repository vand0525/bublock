using System.Numerics;
using Bublock.Modules.Movement;
using RiftRoulette.Rift;
using RiftRoulette.Round;

namespace Bublock.Tests.RiftRoulette;

public class SlotSpotsTests
{
  private const int Slots = 13;
  private const float MinSpacing = 90f;

  public static TheoryData<RiftSide> Sides => new() { RiftSide.Green, RiftSide.Yellow };

  private static List<MovementLocation> All(MovementLocation anchor, Func<MovementLocation, int, MovementLocation> spot) =>
    Enumerable.Range(0, Slots).Select(slot => spot(anchor, slot)).ToList();

  private static void AssertSpread(IReadOnlyList<MovementLocation> spots)
  {
    for (var i = 0; i < spots.Count; i++)
    for (var j = i + 1; j < spots.Count; j++)
    {
      var distance = Vector3.Distance(spots[i].Position, spots[j].Position);
      Assert.True(distance >= MinSpacing, $"Slots {i} and {j} are {distance:0} apart");
    }
  }

  [Fact]
  public void Embedded_table_has_an_entry_for_every_slot()
  {
    Assert.Equal(Slots, SlotSpots.Offsets.Watch.Count);
    Assert.Equal(Slots, SlotSpots.Offsets.Fight.Count);
  }

  [Theory]
  [MemberData(nameof(Sides))]
  public void Watch_spots_are_spread_at_the_anchor_height_with_its_angle(RiftSide side)
  {
    var anchor = RoundLocations.WatchFor(side);
    var spots = All(anchor, SlotSpots.Watch);

    AssertSpread(spots);
    Assert.All(spots, spot => Assert.Equal(anchor.Position.Z, spot.Position.Z));
    Assert.All(spots, spot => Assert.Equal(anchor.Angle, spot.Angle));
    Assert.Equal($"{anchor.Name}#3", spots[3].Name);
  }

  [Fact]
  public void Yellow_watch_spots_are_green_turned_half_a_turn()
  {
    var green = RoundLocations.WatchFor(RiftSide.Green);
    var yellow = RoundLocations.WatchFor(RiftSide.Yellow);

    for (var slot = 0; slot < Slots; slot++)
    {
      var g = SlotSpots.Watch(green, slot).Position - green.Position;
      var y = SlotSpots.Watch(yellow, slot).Position - yellow.Position;

      Assert.True(Vector3.Distance(new Vector3(-g.X, -g.Y, g.Z), y) < 0.1f, $"Slot {slot}: green {g}, yellow {y}");
    }
  }

  [Theory]
  [MemberData(nameof(Sides))]
  public void Fight_spots_are_spread_and_stay_on_each_team_half(RiftSide side)
  {
    var (sapphire, amber) = RoundLocations.StartsFor(side);
    var sapphireSpots = All(sapphire, SlotSpots.Fight);
    var amberSpots = All(amber, SlotSpots.Fight);

    AssertSpread(sapphireSpots);
    AssertSpread(amberSpots);
    Assert.All(sapphireSpots, spot => Assert.True(spot.Position.Y > 0f, $"{spot.Name} at {spot.Position}"));
    Assert.All(amberSpots, spot => Assert.True(spot.Position.Y < 0f, $"{spot.Name} at {spot.Position}"));
  }

  [Theory]
  [InlineData(0, 0)]
  [InlineData(12, 12)]
  [InlineData(13, 0)]
  [InlineData(20, 7)]
  [InlineData(-1, 12)]
  public void Index_wraps_slots_outside_the_table(int slot, int expected)
  {
    Assert.Equal(expected, SlotSpots.Index(slot, Slots));
  }

  [Fact]
  public void Parse_rejects_entries_without_three_numbers()
  {
    Assert.Throws<InvalidOperationException>(() => SlotSpots.Parse("""{"watch":[[1,2]],"fight":[[0,0,0]]}"""));
    Assert.Throws<InvalidOperationException>(() => SlotSpots.Parse("""{"watch":[[0,0,0]]}"""));
  }
}
