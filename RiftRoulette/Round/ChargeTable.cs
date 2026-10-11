using System.Text.Json;
using System.Text.Json.Serialization;
using DeadworksManaged.Api;

namespace RiftRoulette.Round;

public sealed record ChargeEntry(
  [property: JsonPropertyName("base")] int Base,
  [property: JsonPropertyName("tiers")] int[] Tiers,
  [property: JsonPropertyName("conditional")] bool Conditional);

public sealed record ChargeData(
  [property: JsonPropertyName("abilities")] Dictionary<string, ChargeEntry> Abilities,
  [property: JsonPropertyName("items")] Dictionary<string, int> Items);

public static class ChargeTable
{
  public const string ResourceName = "RiftRoulette.Round.ability-charges.json";

  private static readonly Lazy<ChargeData> Embedded = new(LoadEmbedded);

  public static ChargeData Data => Embedded.Value;

  public static ChargeEntry? Find(string ability) =>
    Data.Abilities.TryGetValue(ability, out var entry) ? entry : null;

  public static int? Max(string ability, int upgradeBits, IEnumerable<string> heldItems) =>
    Find(ability) is { } entry ? Max(entry, upgradeBits, heldItems, Data.Items) : null;

  // Tier i is unlocked when bit i + 1 of UpgradeBits is set (bit 0 = the ability itself).
  public static int Max(ChargeEntry entry, int upgradeBits, IEnumerable<string> heldItems, IReadOnlyDictionary<string, int> items)
  {
    var tiers = entry.Tiers.Where((_, tier) => (upgradeBits & (1 << (tier + 1))) != 0).Sum();
    var itemBonus = heldItems.Select(item => items.TryGetValue(item, out var bonus) ? bonus : 0).DefaultIfEmpty(0).Max();

    return entry.Base + tiers + itemBonus;
  }

  // A resource-pool ultimate (Werewolf's wolf form) must be earned in the fight.
  public static bool ShouldFill(ChargeEntry entry, EAbilitySlot slot) =>
    !(entry.Conditional && slot == EAbilitySlot.Signature4);

  public static ChargeData Parse(string json) =>
    JsonSerializer.Deserialize<ChargeData>(json) is { Abilities: not null, Items: not null } data
      ? data
      : throw new InvalidOperationException("ability-charges.json has no abilities or items.");

  private static ChargeData LoadEmbedded()
  {
    using var stream = typeof(ChargeTable).Assembly.GetManifestResourceStream(ResourceName)
      ?? throw new InvalidOperationException($"Embedded resource {ResourceName} not found.");
    using var reader = new StreamReader(stream);

    return Parse(reader.ReadToEnd());
  }
}
