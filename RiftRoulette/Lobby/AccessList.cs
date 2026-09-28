using System.Text.Json;
using System.Text.Json.Serialization;

namespace RiftRoulette.Lobby;

public sealed class AccessList
{
  private static readonly JsonSerializerOptions Options = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    NumberHandling = JsonNumberHandling.AllowReadingFromString,
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true,
    WriteIndented = true
  };

  public bool Private { get; set; }

  public SortedSet<ulong> Banned { get; } = [];

  public SortedSet<ulong> Allowed { get; } = [];

  public string? StatueModifier { get; set; }

  public AccessVerdict Check(ulong steamId, bool isAdmin) =>
    AccessRule.Check(Banned.Contains(steamId), Private, Allowed.Contains(steamId), isAdmin);

  public string ToJson() =>
    JsonSerializer.Serialize(new AccessFile(Private, [.. Banned], [.. Allowed], StatueModifier), Options) + "\n";

  public static AccessList Parse(string json)
  {
    var file = JsonSerializer.Deserialize<AccessFile>(json, Options)
      ?? throw new JsonException("Access file is empty.");

    var list = new AccessList
    {
      Private = file.Private,
      StatueModifier = string.IsNullOrWhiteSpace(file.StatueModifier) ? null : file.StatueModifier.Trim()
    };

    foreach (var steamId in file.Banned ?? [])
      list.Banned.Add(steamId);

    foreach (var steamId in file.Allowed ?? [])
      list.Allowed.Add(steamId);

    return list;
  }

  internal sealed record AccessFile(
    bool Private,
    ulong[]? Banned,
    ulong[]? Allowed,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? StatueModifier);
}
