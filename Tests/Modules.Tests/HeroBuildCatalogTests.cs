using Bublock.Modules.Loadout;
using DeadworksManaged.Api;

namespace Bublock.Tests.Modules;

public class HeroBuildCatalogTests
{
  private const string Sample = """
    {
      "fetchedAt": "2026-09-27T00:00:00Z", "source": "test", "windowDays": 14,
      "heroes": [
        { "id": 13, "className": "hero_haze", "name": "Haze",
          "builds": [{ "buildId": 1, "version": 2, "name": "Fast", "rank": "matches", "matches": 10, "wins": 6,
            "favorites": 0, "items": ["upgrade_a", "upgrade_b"], "imbues": {"upgrade_b": "ability_x"},
            "sellPriority": {"upgrade_a": 100},
            "abilities": [{ "ability": "ability_x", "kind": "unlock" }] }] },
        { "id": 9999, "className": "hero_future", "name": "Future", "builds": [{ "buildId": 2, "name": "x", "rank": "matches" }] },
        { "id": 19, "className": "hero_shiv", "name": "Shiv", "builds": [] }
      ],
      "components": { "upgrade_b": ["upgrade_a"] }
    }
    """;

  [Fact]
  public void Parse_reads_builds_items_and_components()
  {
    var catalog = new HeroBuildCatalog(HeroBuildData.Parse(Sample));
    var build = Assert.Single(catalog.BuildsFor(Heroes.Haze));

    Assert.Equal("Fast", build.Name);
    Assert.Equal(["upgrade_a", "upgrade_b"], build.Items);
    Assert.Equal("ability_x", build.Imbues!["upgrade_b"]);
    Assert.Equal(new AbilityStep("ability_x", AbilityStep.Unlock), Assert.Single(build.Abilities!));
    Assert.Equal(["upgrade_a"], catalog.ComponentsOf("upgrade_b"));
    Assert.Empty(catalog.ComponentsOf("upgrade_a"));
    Assert.Equal(["upgrade_b"], catalog.UpgradesOf("upgrade_a"));
    Assert.Empty(catalog.UpgradesOf("upgrade_b"));
    Assert.Equal(100, build.SellPriorityOf("upgrade_a"));
    Assert.Equal(0, build.SellPriorityOf("upgrade_b"));
  }

  [Fact]
  public void Builds_without_sell_priority_parse_with_none()
  {
    var catalog = new HeroBuildCatalog(HeroBuildData.Parse(Sample.Replace("\"sellPriority\": {\"upgrade_a\": 100},", "")));
    var build = Assert.Single(catalog.BuildsFor(Heroes.Haze));

    Assert.Null(build.SellPriority);
    Assert.Equal(0, build.SellPriorityOf("upgrade_a"));
  }

  [Fact]
  public void Plan_follows_the_budget()
  {
    const string json = """
      {
        "fetchedAt": "x", "source": "test", "windowDays": 14,
        "heroes": [{ "id": 13, "className": "hero_haze", "name": "Haze", "builds": [
          { "buildId": 1, "name": "a", "rank": "matches", "items": ["cheap", "mid", "big"] }
        ] }],
        "itemCosts": { "cheap": 800, "mid": 3200, "big": 6400 }
      }
      """;

    var catalog = new HeroBuildCatalog(HeroBuildData.Parse(json));
    var build = Assert.Single(catalog.BuildsFor(Heroes.Haze));

    Assert.Equal(800, catalog.PlannedValue(build, budget: 1000));
    Assert.Equal(4000, catalog.PlannedValue(build, budget: 5000));
    Assert.Equal(10400, catalog.PlannedValue(build));
  }

  [Fact]
  public void Plan_fills_from_optional_items_then_upgrades_components()
  {
    const string json = """
      {
        "fetchedAt": "x", "source": "test", "windowDays": 14,
        "heroes": [{ "id": 13, "className": "hero_haze", "name": "Haze", "builds": [
          { "buildId": 1, "name": "a", "rank": "matches", "categories": [
            { "name": "Early", "optional": false, "items": ["t1"] },
            { "name": "Options", "optional": true, "items": ["o1", "o2"] }
          ] }
        ] }],
        "components": { "t2": ["t1"] },
        "itemCosts": { "t1": 800, "t2": 1600, "o1": 800, "o2": 3200 }
      }
      """;

    var catalog = new HeroBuildCatalog(HeroBuildData.Parse(json));
    var plan = catalog.Plan(Assert.Single(catalog.BuildsFor(Heroes.Haze)), budget: 1_000_000);

    Assert.Equal(["o1", "o2", "t2"], plan.Items);
    Assert.Equal(["o2"], plan.Filled);
    Assert.Equal(["t2"], plan.Upgraded);
    Assert.Equal(5600, plan.Value);
  }

  [Fact]
  public void Plan_below_every_slot_open_skips_fill_and_upgrade()
  {
    const string json = """
      {
        "fetchedAt": "x", "source": "test", "windowDays": 14,
        "heroes": [{ "id": 13, "className": "hero_haze", "name": "Haze", "builds": [
          { "buildId": 1, "name": "a", "rank": "matches", "categories": [
            { "name": "Early", "optional": false, "items": ["t1"] },
            { "name": "Options", "optional": true, "items": ["o1", "o2"] }
          ] }
        ] }],
        "components": { "t2": ["t1"] },
        "itemCosts": { "t1": 800, "t2": 1600, "o1": 800, "o2": 3200 }
      }
      """;

    var catalog = new HeroBuildCatalog(HeroBuildData.Parse(json));
    var plan = catalog.Plan(Assert.Single(catalog.BuildsFor(Heroes.Haze)), budget: 20000);

    Assert.Equal(["t1", "o1"], plan.Items);
    Assert.Empty(plan.Filled);
    Assert.Empty(plan.Upgraded);
  }

  [Theory]
  [InlineData(8000)]
  [InlineData(14000)]
  [InlineData(20000)]
  [InlineData(26000)]
  [InlineData(60000)]
  public void Default_plans_never_exceed_the_slots_at_the_cap(int budget)
  {
    var catalog = HeroBuildCatalog.Default;

    Assert.All(
      catalog.Heroes.SelectMany(catalog.BuildsFor),
      build => Assert.InRange(catalog.Plan(build, budget).Items.Count, 0, ItemSlots.ForSouls(budget)));
  }

  [Fact]
  public void Heroes_skips_unknown_ids_and_heroes_without_builds()
  {
    var catalog = new HeroBuildCatalog(HeroBuildData.Parse(Sample));

    Assert.Equal([Heroes.Haze], catalog.Heroes);
    Assert.Empty(catalog.BuildsFor(Heroes.Shiv));
  }

  [Fact]
  public void TryParseHero_accepts_enum_and_display_names()
  {
    var catalog = new HeroBuildCatalog(HeroBuildData.Parse(Sample));

    Assert.True(catalog.TryParseHero("haze", out var byEnum));
    Assert.Equal(Heroes.Haze, byEnum);
    Assert.False(catalog.TryParseHero("nobody", out _));
    Assert.Equal("Haze", catalog.DisplayName(Heroes.Haze));
    Assert.Equal("Shiv", catalog.DisplayName(Heroes.Shiv));
  }

  [Fact]
  public void Default_loads_the_embedded_data_with_builds_for_every_hero()
  {
    var catalog = HeroBuildCatalog.Default;

    Assert.True(catalog.Heroes.Count >= 20);

    foreach (var hero in catalog.Heroes)
    {
      var builds = catalog.BuildsFor(hero);

      Assert.InRange(builds.Count, 1, 3);
      Assert.All(builds, build => Assert.NotEmpty(build.Items!));
    }
  }

  [Fact]
  public void Default_has_no_banned_items_and_loads_categories_and_costs()
  {
    var catalog = HeroBuildCatalog.Default;
    var builds = catalog.Heroes.SelectMany(catalog.BuildsFor).ToList();

    Assert.Contains("upgrade_non_player_bonus_sacrifice", LoadoutPlanner.Banned);

    Assert.All(builds, build =>
    {
      Assert.DoesNotContain(build.Items!, LoadoutPlanner.Banned.Contains);
      Assert.NotEmpty(build.Categories!);
      Assert.All(build.Categories!, category => Assert.DoesNotContain(category.Items!, LoadoutPlanner.Banned.Contains));
    });

    Assert.Contains(builds, build => build.Categories!.Any(category => category.Optional));
    Assert.All(builds.SelectMany(build => build.Items!), item => Assert.True(catalog.CostOf(item) > 0, item));
  }

  [Fact]
  public void Default_baseline_sits_between_the_cheapest_and_priciest_build()
  {
    var catalog = HeroBuildCatalog.Default;
    var values = catalog.Heroes.SelectMany(catalog.BuildsFor).Select(build => catalog.PlannedValue(build)).ToList();

    Assert.InRange(catalog.BaselineValue, values.Min(), values.Max());
    Assert.True(catalog.BaselineValue > 0);
  }

  [Fact]
  public void Baseline_is_the_median_planned_value()
  {
    const string json = """
      {
        "fetchedAt": "x", "source": "test", "windowDays": 14,
        "heroes": [{ "id": 13, "className": "hero_haze", "name": "Haze", "builds": [
          { "buildId": 1, "name": "a", "rank": "matches", "items": ["cheap"] },
          { "buildId": 2, "name": "b", "rank": "matches", "items": ["mid"] },
          { "buildId": 3, "name": "c", "rank": "matches", "items": ["big"] }
        ] }],
        "itemCosts": { "cheap": 800, "mid": 3200, "big": 6400 }
      }
      """;

    var catalog = new HeroBuildCatalog(HeroBuildData.Parse(json));

    Assert.Equal(3200, catalog.BaselineValue);
    Assert.Equal(6400, catalog.CostOf("big"));
    Assert.Equal(0, catalog.CostOf("unknown"));
  }

  [Fact]
  public void Default_display_name_uses_the_game_name()
  {
    Assert.True(HeroBuildCatalog.Default.TryParseHero("Infernus", out var hero));
    Assert.Equal(Heroes.Inferno, hero);
  }
}
