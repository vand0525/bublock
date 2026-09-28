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

  private static int Cost(string item) => item switch
  {
    "big" => 6400,
    "mid" => 3200,
    "t3" => 3200,
    "t2" => 1600,
    _ => 800
  };

  private const int Plenty = 1_000_000;

  private static IReadOnlyList<string> UpgradesOf(string item) =>
    Components.Where(entry => entry.Value.Contains(item)).Select(entry => entry.Key).ToList();

  private static ShopPlan Plan(
    IEnumerable<string> order,
    int budget = Plenty,
    int slots = LoadoutPlanner.DefaultSlots,
    Func<string, int>? sellPriorityOf = null,
    Func<string, bool>? exists = null,
    IEnumerable<string>? fillers = null,
    bool upgrade = false) =>
    LoadoutPlanner.Plan(order, ComponentsOf, Cost, budget, slots, sellPriorityOf, exists, fillers, upgrade ? UpgradesOf : null);

  [Fact]
  public void Plan_fills_empty_slots_with_the_most_expensive_optional_items()
  {
    var plan = Plan(["a"], slots: 3, fillers: ["x", "mid", "big"]);

    Assert.Equal(["a", "big", "mid"], plan.Items);
    Assert.Equal(["big", "mid"], plan.Filled);
  }

  [Fact]
  public void Plan_fill_takes_the_priciest_optional_item_that_fits_the_budget()
  {
    var plan = Plan(["a"], budget: 4000, fillers: ["big", "x", "mid"]);

    Assert.Equal(["a", "mid"], plan.Items);
    Assert.Equal(4000, plan.Value);
  }

  [Fact]
  public void Plan_fill_never_sells()
  {
    var plan = Plan(["a", "b"], slots: 2, fillers: ["big"]);

    Assert.Equal(["a", "b"], plan.Items);
    Assert.Empty(plan.Filled);
    Assert.Empty(plan.Sold);
  }

  [Fact]
  public void Plan_upgrades_every_component_item_as_the_final_pass()
  {
    var plan = Plan(["t1", "a"], upgrade: true);

    Assert.Equal(["a", "t3"], plan.Items);
    Assert.Equal(["t2", "t3"], plan.Upgraded);
    Assert.Equal(800 + 3200, plan.Value);
  }

  [Fact]
  public void Plan_upgrade_pass_stays_within_the_budget()
  {
    var plan = Plan(["t1", "a"], budget: 2400, upgrade: true);

    Assert.Equal(["a", "t2"], plan.Items);
    Assert.Equal(["t2"], plan.Upgraded);
    Assert.Equal(2400, plan.Value);
  }

  [Fact]
  public void Plan_upgrades_after_filling()
  {
    var plan = Plan(["t1"], budget: 4800, slots: 2, fillers: ["mid"], upgrade: true);

    Assert.Equal(["mid", "t2"], plan.Items);
    Assert.Equal(["mid"], plan.Filled);
    Assert.Equal(["t2"], plan.Upgraded);
  }

  [Fact]
  public void OptionalItems_lists_only_optional_groups_without_banned_items()
  {
    var categories = new[]
    {
      new BuildCategory("Early", false, ["a"]),
      new BuildCategory("Options", true, ["x", "upgrade_health_stimpak", "y"]),
      new BuildCategory("More", true, ["y", "z"])
    };

    Assert.Equal(["x", "y", "z"], LoadoutPlanner.OptionalItems(categories));
  }

  [Fact]
  public void Plan_fills_12_slots_in_order()
  {
    var order = Enumerable.Range(1, 14).Select(i => $"i{i}").ToList();
    var plan = Plan(order);

    Assert.Equal(12, LoadoutPlanner.DefaultSlots);
    Assert.Equal(order.Take(12), plan.Items);
    Assert.Equal(["i13", "i14"], plan.Skipped);
    Assert.Empty(plan.Sold);
  }

  [Fact]
  public void Plan_cap_1000_buys_one_800_item()
  {
    var plan = Plan(["a", "b", "big"], budget: 1000);

    Assert.Equal(["a"], plan.Items);
    Assert.Equal(800, plan.Value);
    Assert.Equal(["b", "big"], plan.Skipped);
  }

  [Theory]
  [InlineData(1000)]
  [InlineData(5000)]
  [InlineData(20000)]
  [InlineData(60000)]
  public void Plan_never_goes_over_the_budget(int budget)
  {
    var order = new[] { "t1", "a", "big", "t2", "b", "mid", "c", "t3", "d", "e", "f", "g", "h", "i", "j", "k" };
    var plan = Plan(order, budget, slots: 6);

    Assert.True(plan.Value <= budget);
    Assert.Equal(LoadoutPlanner.Value(plan.Items, Cost), plan.Value);
    Assert.True(plan.Items.Count <= 6);
  }

  [Fact]
  public void Plan_skips_an_unaffordable_item_and_buys_a_later_cheaper_one()
  {
    var plan = Plan(["a", "big", "b"], budget: 1600);

    Assert.Equal(["a", "b"], plan.Items);
    Assert.Equal(["big"], plan.Skipped);
  }

  [Fact]
  public void Plan_upgrade_costs_the_difference_and_frees_its_component_slot()
  {
    var plan = Plan(["t1", "a", "t2", "b"], budget: 3200, slots: 3);

    Assert.Equal(["a", "t2", "b"], plan.Items);
    Assert.Equal(3200, plan.Value);
    Assert.Empty(plan.Sold);
  }

  [Fact]
  public void Plan_upgrade_with_full_slots_needs_no_sale()
  {
    var plan = Plan(["t1", "a", "t2"], slots: 2);

    Assert.Equal(["a", "t2"], plan.Items);
    Assert.Empty(plan.Sold);
  }

  [Fact]
  public void Plan_removes_only_direct_components()
  {
    Assert.Equal(["t1", "t3"], Plan(["t1", "t3"]).Items);
  }

  [Fact]
  public void Plan_with_full_slots_sells_the_cheapest_earliest_item_for_a_pricier_one()
  {
    var plan = Plan(["mid", "a", "b", "big"], slots: 3);

    Assert.Equal(["mid", "b", "big"], plan.Items);
    Assert.Equal(["a"], plan.Sold);
    Assert.Equal(3200 + 800 + 6400, plan.Value);
  }

  [Fact]
  public void Plan_does_not_sell_for_an_item_that_is_not_pricier()
  {
    var plan = Plan(["mid", "a", "b"], slots: 2);

    Assert.Equal(["mid", "a"], plan.Items);
    Assert.Equal(["b"], plan.Skipped);
    Assert.Empty(plan.Sold);
  }

  [Fact]
  public void Plan_does_not_sell_when_the_replacement_is_not_affordable()
  {
    var plan = Plan(["a", "b", "big"], budget: 6000, slots: 2);

    Assert.Equal(["a", "b"], plan.Items);
    Assert.Equal(["big"], plan.Skipped);
    Assert.Empty(plan.Sold);
  }

  [Fact]
  public void Plan_sells_marked_items_first_highest_priority_first()
  {
    var priorities = new Dictionary<string, int> { ["a"] = 1, ["mid"] = 4 };
    var plan = Plan(["a", "mid", "b", "big"], slots: 3, sellPriorityOf: item => priorities.GetValueOrDefault(item));

    Assert.Equal(["a", "b", "big"], plan.Items);
    Assert.Equal(["mid"], plan.Sold);
  }

  [Fact]
  public void Plan_skips_duplicates_missing_and_banned_items_including_healing_rite()
  {
    var plan = Plan(
      ["a", "a", "upgrade_health_stimpak", "gone", "upgrade_goose_egg", "b"],
      exists: item => item != "gone");

    Assert.Contains("upgrade_health_stimpak", LoadoutPlanner.Banned);
    Assert.Equal(["a", "b"], plan.Items);
    Assert.Empty(plan.Skipped);
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

  private static AbilityStep Unlock(string ability) => new(ability, AbilityStep.Unlock);

  private static AbilityStep Upgrade(string ability) => new(ability, AbilityStep.Upgrade);

  [Fact]
  public void AbilityPrefix_with_the_full_budget_matches_AbilityBits()
  {
    var steps = new[]
    {
      Unlock("a"), Upgrade("a"), Unlock("b"), Unlock("c"), Unlock("d"),
      Upgrade("b"), Upgrade("a"), Upgrade("a"), Upgrade("c"), Upgrade("b"),
      Upgrade("c"), Upgrade("b"), Upgrade("c"), Upgrade("d"), Upgrade("d"), Upgrade("d")
    };

    var plan = LoadoutPlanner.AbilityPrefix(steps, Progression.Max.Unlocks, Progression.Max.AbilityPoints);

    Assert.Equal(LoadoutPlanner.AbilityBits(steps), plan.Bits);
    Assert.Equal(16, plan.StepsTaken);
    Assert.Equal(16, plan.StepsTotal);
    Assert.Equal(4, plan.UnlocksUsed);
    Assert.Equal(32, plan.PointsUsed);
  }

  [Fact]
  public void AbilityPrefix_stops_when_the_next_tier_costs_more_than_is_left()
  {
    var steps = new[] { Unlock("a"), Upgrade("a"), Upgrade("a"), Upgrade("a"), Unlock("b") };

    var plan = LoadoutPlanner.AbilityPrefix(steps, unlocks: 2, points: 7);

    Assert.Equal([("a", 0b111)], plan.Bits);
    Assert.Equal(3, plan.StepsTaken);
    Assert.Equal(3, plan.PointsUsed);
  }

  [Fact]
  public void AbilityPrefix_never_skips_ahead_to_a_cheaper_step()
  {
    var steps = new[] { Unlock("a"), Upgrade("a"), Upgrade("a"), Upgrade("a"), Upgrade("b") };

    var plan = LoadoutPlanner.AbilityPrefix(steps, unlocks: 2, points: 4);

    Assert.Equal([("a", 0b111)], plan.Bits);
    Assert.Equal(1, plan.UnlocksUsed);
  }

  [Fact]
  public void AbilityPrefix_stops_when_unlocks_run_out()
  {
    var steps = new[] { Unlock("a"), Unlock("b"), Upgrade("c"), Upgrade("a") };

    var plan = LoadoutPlanner.AbilityPrefix(steps, unlocks: 2, points: 10);

    Assert.Equal([("a", 0b1), ("b", 0b1)], plan.Bits);
    Assert.Equal(2, plan.StepsTaken);
  }

  [Fact]
  public void AbilityPrefix_upgrade_on_a_locked_ability_pays_the_unlock_first()
  {
    var plan = LoadoutPlanner.AbilityPrefix([Upgrade("a")], unlocks: 1, points: 1);

    Assert.Equal([("a", 0b11)], plan.Bits);
    Assert.Equal(1, plan.UnlocksUsed);
    Assert.Equal(1, plan.PointsUsed);
  }

  [Fact]
  public void AbilityPrefix_repeats_and_unknown_kinds_cost_nothing()
  {
    var steps = new[]
    {
      Unlock("a"), Unlock("a"), Upgrade("a"), Upgrade("a"), Upgrade("a"), Upgrade("a"),
      new AbilityStep("a", "refund")
    };

    var plan = LoadoutPlanner.AbilityPrefix(steps, unlocks: 1, points: 8);

    Assert.Equal([("a", LoadoutPlanner.FullyUpgradedBits)], plan.Bits);
    Assert.Equal(6, plan.StepsTaken);
    Assert.Equal(6, plan.StepsTotal);
    Assert.Equal(8, plan.PointsUsed);
  }

  [Theory]
  [InlineData("15000", 15000)]
  [InlineData(" 1000 ", 1000)]
  [InlineData("200000", 200000)]
  [InlineData("default", LoadoutPlanner.DefaultCap)]
  [InlineData("DEFAULT", LoadoutPlanner.DefaultCap)]
  public void TryParseCap_accepts_whole_numbers_in_range_and_default(string text, int expected)
  {
    Assert.True(LoadoutPlanner.TryParseCap(text, out var souls));
    Assert.Equal(expected, souls);
  }

  [Theory]
  [InlineData("999")]
  [InlineData("200001")]
  [InlineData("-5000")]
  [InlineData("15,000")]
  [InlineData("15000.5")]
  [InlineData("lots")]
  [InlineData("")]
  [InlineData(null)]
  public void TryParseCap_rejects_out_of_range_and_non_numbers(string? text)
  {
    Assert.False(LoadoutPlanner.TryParseCap(text, out _));
  }
}
