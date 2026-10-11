using DeadworksManaged.Api;

namespace RiftRoulette.Round;

public static class AbilityCharges
{
  private static readonly EAbilitySlot[] Slots =
    [EAbilitySlot.Signature1, EAbilitySlot.Signature2, EAbilitySlot.Signature3, EAbilitySlot.Signature4];

  public static string Describe(CCitadelPlayerPawn pawn)
  {
    var now = GlobalVars.CurTime;
    var parts = new List<string>();

    foreach (var slot in Slots)
    {
      var ability = pawn.AbilityComponent.GetAbilityBySlot(slot);

      if (ability == null || !ability.IsValid)
        continue;

      var cooldown = Math.Max(0f, ability.CooldownEnd - now);
      parts.Add($"{ability.AbilityName} charges={ability.RemainingCharges} max={ability.MaxCharges} cd={cooldown:0.0}s");
    }

    return string.Join("; ", parts);
  }
}
