using System.Numerics;
using Bublock.Modules.Movement;

namespace Bublock.Tests.Modules;

public class LocationRegistryTests
{
  private static MovementLocation At(string name, float x = 0) =>
    new(name, new Vector3(x, 0, 0), Vector3.Zero);

  [Fact]
  public void Lookup_ignores_case()
  {
    var registry = new LocationRegistry();
    registry.Register(At("draft"));

    Assert.True(registry.TryGet("DRAFT", out var location));
    Assert.Equal("draft", location.Name);
  }

  [Fact]
  public void Unknown_name_is_not_found()
  {
    var registry = new LocationRegistry();

    Assert.False(registry.TryGet("nowhere", out _));
  }

  [Fact]
  public void Register_replaces_existing_name()
  {
    var registry = new LocationRegistry();
    registry.Register(At("draft", 1));
    registry.Register(At("Draft", 2));

    Assert.True(registry.TryGet("draft", out var location));
    Assert.Equal(2, location.Position.X);
    Assert.Single(registry.List());
  }

  [Theory]
  [InlineData("draft")]
  [InlineData("green.sapphire")]
  [InlineData("spot_2-b")]
  public void Valid_names_are_accepted(string name)
  {
    Assert.True(LocationRegistry.IsValidName(name));
  }

  [Theory]
  [InlineData("")]
  [InlineData("two words")]
  [InlineData("bad/name")]
  public void Invalid_names_are_rejected(string name)
  {
    var registry = new LocationRegistry();

    Assert.False(LocationRegistry.IsValidName(name));
    Assert.Throws<ArgumentException>(() => registry.Register(At(name)));
  }

  [Fact]
  public void Code_locations_cannot_be_removed()
  {
    var registry = new LocationRegistry();
    registry.Register(At("draft"));

    Assert.False(registry.Unregister("draft"));
    Assert.False(registry.IsSaved("draft"));
    Assert.True(registry.TryGet("draft", out _));
  }

  [Fact]
  public void Saved_locations_can_be_removed()
  {
    var registry = new LocationRegistry();
    registry.Register(At("mine"), saved: true);

    Assert.True(registry.IsSaved("MINE"));
    Assert.True(registry.Unregister("MINE"));
    Assert.False(registry.TryGet("mine", out _));
    Assert.False(registry.Unregister("mine"));
  }

  [Fact]
  public void List_is_sorted_by_name_and_marks_saved()
  {
    var registry = new LocationRegistry();
    registry.Register(At("yellow_amber"));
    registry.Register(At("Draft"));
    registry.Register(At("mine"), saved: true);

    var listing = registry.List();

    Assert.Equal(["Draft", "mine", "yellow_amber"], listing.Select(entry => entry.Location.Name));
    Assert.Equal([false, true, false], listing.Select(entry => entry.Saved));
  }
}
