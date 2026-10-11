using DeadworksManaged.Api;

namespace RiftRoulette.Round;

public static class ChargeFill
{
  public static readonly SchemaAccessor<int> RemainingCharges = new("CCitadelBaseAbility"u8, "m_iRemainingCharges"u8);

  private static readonly EAbilitySlot[] Slots =
    [EAbilitySlot.Signature1, EAbilitySlot.Signature2, EAbilitySlot.Signature3, EAbilitySlot.Signature4];

  public static string Fill(CCitadelPlayerPawn pawn)
  {
    var heldItems = pawn.AbilityComponent.Abilities
      .Where(ability => ability.IsValid && ability.IsItem)
      .Select(ability => ability.AbilityName)
      .ToList();
    var filled = new List<string>();

    foreach (var slot in Slots)
    {
      var ability = pawn.AbilityComponent.GetAbilityBySlot(slot);

      if (ability == null || !ability.IsValid || ChargeTable.Find(ability.AbilityName) is not { } entry || !ChargeTable.ShouldFill(entry, slot))
        continue;

      var max = ChargeTable.Max(entry, ability.UpgradeBits, heldItems, ChargeTable.Data.Items);
      var current = ability.RemainingCharges;

      if (current >= max)
        continue;

      RemainingCharges.Set(ability.Handle, max);
      filled.Add($"{ability.AbilityName} {current}->{max}");
    }

    return string.Join("; ", filled);
  }
}
