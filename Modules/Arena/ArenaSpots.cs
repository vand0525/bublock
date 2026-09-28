using System.Numerics;
using System.Reflection;
using System.Text.Json;
using Bublock.Modules.Movement;
using Bublock.Modules.Teams;

namespace Bublock.Modules.Arena;

public sealed record ArenaBounds(Vector3 Min, Vector3 Max)
{
  public bool Contains(Vector3 point) =>
    point.X >= Min.X && point.X <= Max.X
    && point.Y >= Min.Y && point.Y <= Max.Y
    && point.Z >= Min.Z && point.Z <= Max.Z;
}

public sealed class ArenaSpots
{
  public ArenaSpots(
    string name,
    MovementLocation sapphire,
    MovementLocation amber,
    IReadOnlyList<Vector3> offsets,
    ArenaBounds? bounds = null,
    int? activeLane = null)
  {
    if (offsets.Count == 0)
      throw new ArgumentException("An arena needs at least one spot offset.", nameof(offsets));

    Name = name;
    Sapphire = sapphire;
    Amber = amber;
    Offsets = offsets;
    Bounds = bounds;
    ActiveLane = activeLane;
  }

  public string Name { get; }

  public MovementLocation Sapphire { get; }

  public MovementLocation Amber { get; }

  public IReadOnlyList<Vector3> Offsets { get; }

  // Players outside this box are sent back to their spot (ArenaService.ReturnStrays); null = no containment.
  public ArenaBounds? Bounds { get; }

  // The game's own single-lane setting (citadel_active_lane) for this arena; null = leave it alone.
  public int? ActiveLane { get; }

  public bool Contains(Vector3 point) => Bounds?.Contains(point) ?? true;

  // A player's own spot: their team's anchor plus their slot's (forward, right, up) offset.
  public MovementLocation? For(int team, int slot)
  {
    if (!DeadlockTeams.IsPlayable(team))
      return null;

    var anchor = team == DeadlockTeams.Sapphire ? Sapphire : Amber;
    return anchor.Offset(Offsets[Index(slot, Offsets.Count)], $"{anchor.Name}#{slot}");
  }

  public static int Index(int slot, int count) =>
    ((slot % count) + count) % count;

  // A game type embeds its own arena.json and loads it by logical name.
  public static ArenaSpots Load(Assembly assembly, string resourceName)
  {
    using var stream = assembly.GetManifestResourceStream(resourceName)
      ?? throw new InvalidOperationException($"Embedded resource {resourceName} not found.");
    using var reader = new StreamReader(stream);

    return Parse(reader.ReadToEnd());
  }

  public static ArenaSpots Parse(string json)
  {
    using var document = JsonDocument.Parse(json);
    var root = document.RootElement;
    var name = root.TryGetProperty("name", out var nameElement) ? nameElement.GetString() ?? "arena" : "arena";

    ArenaBounds? bounds = root.TryGetProperty("bounds", out var box)
      ? new ArenaBounds(Vector(box.GetProperty("min"), "bounds"), Vector(box.GetProperty("max"), "bounds"))
      : null;
    int? activeLane = root.TryGetProperty("active_lane", out var lane) ? lane.GetInt32() : null;

    return new ArenaSpots(
      name,
      Anchor(root, "sapphire", $"{name} sapphire"),
      Anchor(root, "amber", $"{name} amber"),
      root.GetProperty("offsets").EnumerateArray().Select(row => Vector(row, "offsets")).ToList(),
      bounds,
      activeLane);
  }

  private static MovementLocation Anchor(JsonElement root, string key, string name)
  {
    var anchor = root.GetProperty(key);
    return new MovementLocation(name, Vector(anchor.GetProperty("position"), key), Vector(anchor.GetProperty("angle"), key));
  }

  private static Vector3 Vector(JsonElement row, string key)
  {
    var values = row.EnumerateArray().Select(value => value.GetSingle()).ToArray();

    return values.Length == 3
      ? new Vector3(values[0], values[1], values[2])
      : throw new InvalidOperationException($"arena '{key}' entries need 3 numbers.");
  }
}
