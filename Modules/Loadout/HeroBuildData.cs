using System.Text.Json;

namespace Bublock.Modules.Loadout;

public sealed record AbilityStep(string Ability, string Kind)
{
  public const string Unlock = "unlock";
  public const string Upgrade = "upgrade";
}

public sealed record BuildCategory(string Name, bool Optional, IReadOnlyList<string>? Items);

public sealed record HeroBuild(
  int BuildId,
  int? Version,
  string Name,
  string Rank,
  int Matches,
  int Wins,
  int Favorites,
  IReadOnlyList<string>? Items,
  IReadOnlyDictionary<string, string>? Imbues,
  IReadOnlyList<AbilityStep>? Abilities,
  IReadOnlyList<BuildCategory>? Categories = null,
  IReadOnlyDictionary<string, int>? SellPriority = null)
{
  public int SellPriorityOf(string item) =>
    SellPriority != null && SellPriority.TryGetValue(item, out var priority) ? priority : 0;
}

public sealed record HeroBuildSet(int Id, string ClassName, string Name, IReadOnlyList<HeroBuild>? Builds);

public sealed record HeroBuildData(
  string FetchedAt,
  string Source,
  int WindowDays,
  IReadOnlyList<HeroBuildSet>? Heroes,
  IReadOnlyDictionary<string, IReadOnlyList<string>>? Components,
  IReadOnlyDictionary<string, int>? ItemCosts = null)
{
  private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

  public static HeroBuildData Parse(Stream json) =>
    JsonSerializer.Deserialize<HeroBuildData>(json, Options)
      ?? throw new InvalidDataException("Hero build data is empty.");

  public static HeroBuildData Parse(string json) =>
    JsonSerializer.Deserialize<HeroBuildData>(json, Options)
      ?? throw new InvalidDataException("Hero build data is empty.");
}
