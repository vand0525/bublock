using Bublock.Modules.Loadout;

namespace Bublock.Tests.Modules;

public class LoadoutPlannerTests
{
  private static readonly Dictionary<string, IReadOnlyList<string>> Components = new()
  {
    ["t2"] = ["t1"],
    ["t3"] = ["t2"]
  };

  private static IReadOnlyList<string> ComponentsOf(string item) =>
    Components.TryGetValue(item, out var components) ? components : [];

  [Fact]
  public void FirstSlots_takes_items_in_order_up_to_the_slot_count()
  {
    var order = Enumerable.Range(1, 12).Select(i => $"i{i}").ToList();

    Assert.Equal(order.Take(9), LoadoutPlanner.FirstSlots(order, ComponentsOf));
  }

  [Fact]
  public void FirstSlots_skips_duplicates()
  {
    Assert.Equal(["a", "b"], LoadoutPlanner.FirstSlots(["a", "a", "b"], ComponentsOf, slots: 9));
  }

  [Fact]
  public void FirstSlots_upgrade_replaces_its_component()
  {
    Assert.Equal(["a", "t2"], LoadoutPlanner.FirstSlots(["t1", "a", "t2"], ComponentsOf));
  }

  [Fact]
  public void FirstSlots_upgrade_frees_a_slot_for_the_next_item()
  {
    Assert.Equal(["a", "t2", "b"], LoadoutPlanner.FirstSlots(["t1", "a", "t2", "b"], ComponentsOf, slots: 3));
  }

  [Fact]
  public void FirstSlots_stops_once_slots_are_full()
  {
    Assert.Equal(["t1", "a"], LoadoutPlanner.FirstSlots(["t1", "a", "t2", "b"], ComponentsOf, slots: 2));
  }

  [Fact]
  public void FirstSlots_removes_only_direct_components()
  {
    Assert.Equal(["t1", "t3"], LoadoutPlanner.FirstSlots(["t1", "t3"], ComponentsOf));
  }

  [Fact]
  public void FirstSlots_skips_items_that_do_not_exist()
  {
    Assert.Equal(["a", "c"], LoadoutPlanner.FirstSlots(["a", "gone", "c"], ComponentsOf, exists: item => item != "gone"));
  }

  [Fact]
  public void FirstSlots_skips_banned_items_and_the_next_item_fills_the_slot()
  {
    Assert.Equal(
      ["a", "b", "c"],
      LoadoutPlanner.FirstSlots(["a", "upgrade_goose_egg", "b", "upgrade_non_player_bonus", "c", "d"], ComponentsOf, slots: 3));
  }

  [Fact]
  public void ItemOrder_takes_every_required_item_and_one_per_optional_group()
  {
    var categories = new[]
    {
      new BuildCategory("Early", false, ["a", "b"]),
      new BuildCategory("Options", true, ["x", "y", "z"]),
      new BuildCategory("Core", false, ["c"])
    };

    for (var seed = 0; seed < 20; seed++)
    {
      var order = LoadoutPlanner.ItemOrder(categories, null, new Random(seed));

      Assert.Equal(4, order.Count);
      Assert.Equal(["a", "b"], order.Take(2));
      Assert.Contains(order[2], new[] { "x", "y", "z" });
      Assert.Equal("c", order[3]);
    }
  }

  [Fact]
  public void ItemOrder_without_rng_takes_the_first_optional_item()
  {
    var categories = new[] { new BuildCategory("Options", true, ["x", "y"]) };

    Assert.Equal(["x"], LoadoutPlanner.ItemOrder(categories, null));
  }

  [Fact]
  public void ItemOrder_optional_group_skips_items_already_chosen_and_banned()
  {
    var categories = new[]
    {
      new BuildCategory("Early", false, ["a"]),
      new BuildCategory("Options", true, ["a", "upgrade_goose_egg", "y"]),
      new BuildCategory("Empty", true, ["a"])
    };

    for (var seed = 0; seed < 10; seed++)
      Assert.Equal(["a", "y"], LoadoutPlanner.ItemOrder(categories, null, new Random(seed)));
  }

  [Fact]
  public void ItemOrder_falls_back_to_flat_items_without_categories()
  {
    Assert.Equal(["a", "b"], LoadoutPlanner.ItemOrder(null, ["a", "upgrade_non_player_bonus", "b", "a"]));
  }

  private static int Cost(string item) => item switch
  {
    "big" => 6400,
    "mid" => 3200,
    _ => 800
  };

  [Fact]
  public void CapValue_keeps_loadouts_under_the_cap()
  {
    var (kept, removed) = LoadoutPlanner.CapValue(["a", "mid", "b"], Cost, cap: 20000);

    Assert.Equal(["a", "mid", "b"], kept);
    Assert.Empty(removed);
  }

  [Fact]
  public void CapValue_removes_the_most_expensive_items_until_under()
  {
    var items = new[] { "big", "a", "big", "mid", "big", "b", "c" };
    var (kept, removed) = LoadoutPlanner.CapValue(items, Cost, cap: 12000, minItems: 3);

    Assert.Equal(["big", "big"], removed);
    Assert.Equal(["big", "a", "mid", "b", "c"], kept);
    Assert.True(LoadoutPlanner.Value(kept, Cost) <= 12000);
  }

  [Fact]
  public void CapValue_never_goes_below_the_minimum_item_count()
  {
    var (kept, removed) = LoadoutPlanner.CapValue(["big", "big", "big"], Cost, cap: 100, minItems: 2);

    Assert.Equal(2, kept.Count);
    Assert.Single(removed);
  }

  [Fact]
  public void BitsFor_sets_unlock_then_one_bit_per_tier()
  {
    Assert.Equal(0b1, LoadoutPlanner.BitsFor(0));
    Assert.Equal(0b11, LoadoutPlanner.BitsFor(1));
    Assert.Equal(0b111, LoadoutPlanner.BitsFor(2));
    Assert.Equal(LoadoutPlanner.FullyUpgradedBits, LoadoutPlanner.BitsFor(3));
    Assert.Equal(LoadoutPlanner.FullyUpgradedBits, LoadoutPlanner.BitsFor(5));
  }

  [Fact]
  public void AbilityBits_keeps_first_seen_order_and_caps_upgrades()
  {
    var steps = new[]
    {
      new AbilityStep("b", AbilityStep.Unlock),
      new AbilityStep("a", AbilityStep.Unlock),
      new AbilityStep("a", AbilityStep.Upgrade),
      new AbilityStep("b", AbilityStep.Upgrade),
      new AbilityStep("b", AbilityStep.Upgrade),
      new AbilityStep("b", AbilityStep.Upgrade),
      new AbilityStep("b", AbilityStep.Upgrade),
      new AbilityStep("c", "refund")
    };

    Assert.Equal(
      [("b", LoadoutPlanner.FullyUpgradedBits), ("a", 0b11)],
      LoadoutPlanner.AbilityBits(steps));
  }

  [Fact]
  public void AbilityBits_upgrade_without_unlock_still_unlocks()
  {
    Assert.Equal([("a", 0b11)], LoadoutPlanner.AbilityBits([new AbilityStep("a", AbilityStep.Upgrade)]));
  }
}
