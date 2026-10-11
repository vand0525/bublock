using DeadworksManaged.Api;
using RiftRoulette.Round;

namespace Bublock.Tests.RiftRoulette;

public class ChargeTableTests
{
  private const int Unlocked = 0b1;
  private const int OneTier = 0b11;
  private const int TwoTiers = 0b111;
  private const int AllTiers = 0b11111;

  [Fact]
  public void Embedded_table_loads_abilities_and_items()
  {
    Assert.NotEmpty(ChargeTable.Data.Abilities);
    Assert.Equal(1, ChargeTable.Data.Items["upgrade_extra_charge"]);
    Assert.Equal(2, ChargeTable.Data.Items["upgrade_rapid_recharge"]);
  }

  [Fact]
  public void Baba_brew_fully_upgraded_with_rapid_recharge_holds_four()
  {
    Assert.Equal(4, ChargeTable.Max("ability_baba_bubbling_brew", AllTiers, ["upgrade_healbane", "upgrade_rapid_recharge"]));
  }

  [Fact]
  public void Baba_brew_with_one_tier_and_no_charge_items_holds_one()
  {
    Assert.Equal(1, ChargeTable.Max("ability_baba_bubbling_brew", OneTier, ["upgrade_boxing_glove"]));
  }

  [Theory]
  [InlineData("mirage_fire_beetles", TwoTiers, 3)]
  [InlineData("mirage_fire_beetles", OneTier, 2)]
  [InlineData("ability_priest_beartrap", Unlocked, 2)]
  [InlineData("citadel_ability_hornet_snipe", Unlocked, 2)]
  public void Upgrade_tiers_add_charges_only_when_unlocked(string ability, int bits, int expected)
  {
    Assert.Equal(expected, ChargeTable.Max(ability, bits, []));
  }

  [Fact]
  public void Wraith_cards_and_graves_skulls_are_filled()
  {
    var cards = ChargeTable.Find("citadel_ability_card_toss");
    var skulls = ChargeTable.Find("ability_necro_hauntingskull");

    Assert.NotNull(cards);
    Assert.NotNull(skulls);
    Assert.True(cards.Conditional);
    Assert.True(skulls.Conditional);
    Assert.Equal(2, ChargeTable.Max("citadel_ability_card_toss", Unlocked, []));
    Assert.Equal(4, ChargeTable.Max("citadel_ability_card_toss", OneTier, []));
    Assert.Equal(4, ChargeTable.Max("ability_necro_hauntingskull", Unlocked, []));
    Assert.True(ChargeTable.ShouldFill(cards, EAbilitySlot.Signature1));
    Assert.True(ChargeTable.ShouldFill(skulls, EAbilitySlot.Signature1));
  }

  [Fact]
  public void Werewolf_wolf_form_is_never_filled()
  {
    Assert.Null(ChargeTable.Find("ability_werewolf_transformation"));
    Assert.Null(ChargeTable.Max("ability_werewolf_transformation", AllTiers, ["upgrade_rapid_recharge"]));
  }

  [Fact]
  public void Resource_pool_ultimates_are_never_filled()
  {
    var pool = new ChargeEntry(3, [0, 0, 0], Conditional: true);
    var plain = new ChargeEntry(1, [0, 0, 0], Conditional: false);

    Assert.False(ChargeTable.ShouldFill(pool, EAbilitySlot.Signature4));
    Assert.True(ChargeTable.ShouldFill(pool, EAbilitySlot.Signature3));
    Assert.True(ChargeTable.ShouldFill(plain, EAbilitySlot.Signature4));
  }

  [Fact]
  public void Charge_items_do_not_stack()
  {
    Assert.Equal(4, ChargeTable.Max("citadel_ability_hornet_snipe", Unlocked, ["upgrade_extra_charge", "upgrade_rapid_recharge"]));
    Assert.Equal(3, ChargeTable.Max("citadel_ability_hornet_snipe", Unlocked, ["upgrade_extra_charge"]));
  }

  [Fact]
  public void Unknown_ability_has_no_max()
  {
    Assert.Null(ChargeTable.Max("ability_does_not_exist", AllTiers, []));
  }

  [Fact]
  public void Parse_refuses_a_table_without_items()
  {
    Assert.Throws<InvalidOperationException>(() => ChargeTable.Parse("""{"abilities": {}}"""));
  }
}
