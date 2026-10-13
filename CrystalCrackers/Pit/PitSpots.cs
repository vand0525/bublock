using System.Numerics;
using System.Text.Json;
using Bublock.Modules.Movement;
using Bublock.Shared;

namespace CrystalCrackers.Pit;

public sealed record PitSpotOffsets(IReadOnlyList<Vector3> Watch, IReadOnlyList<Vector3> Pit, IReadOnlyList<Vector3> Lane);

public static class PitSpots
{
  public const string ResourceName = "CrystalCrackers.Pit.spots.json";

  private static readonly Lazy<PitSpotOffsets> Embedded = new(LoadEmbedded);

  private static readonly HashSet<int> WarnedSlots = [];

  public static PitSpotOffsets Offsets => Embedded.Value;

  public static MovementLocation Watch(int slot) => At(PitLocations.Watch, Offsets.Watch, slot);

  public static MovementLocation? Pit(int team, int slot) =>
    PitLocations.PitFor(team) is { } anchor ? At(anchor, Offsets.Pit, slot) : null;

  public static MovementLocation? Lane(int team, int slot) =>
    PitLocations.LaneFor(team) is { } anchor ? At(anchor, Offsets.Lane, slot) : null;

  public static MovementLocation At(MovementLocation anchor, IReadOnlyList<Vector3> offsets, int slot)
  {
    var index = Index(slot, offsets.Count);

    if (index != slot && WarnedSlots.Add(slot))
      BublockLog.For("Pit").Warn("Slot outside the spot table Slot={Slot} Used={Used}", slot, index);

    return anchor.Offset(offsets[index], $"{anchor.Name}#{slot}");
  }

  public static int Index(int slot, int count) =>
    ((slot % count) + count) % count;

  public static PitSpotOffsets Parse(string json)
  {
    var root = JsonSerializer.Deserialize<Dictionary<string, float[][]>>(json)
      ?? throw new InvalidOperationException("spots.json is empty.");

    return new PitSpotOffsets(Read(root, "watch"), Read(root, "pit"), Read(root, "lane"));
  }

  private static IReadOnlyList<Vector3> Read(Dictionary<string, float[][]> root, string key)
  {
    if (!root.TryGetValue(key, out var rows) || rows.Length == 0)
      throw new InvalidOperationException($"spots.json has no '{key}' offsets.");

    return rows
      .Select(row => row.Length == 3
        ? new Vector3(row[0], row[1], row[2])
        : throw new InvalidOperationException($"spots.json '{key}' entry needs [forward, right, up]."))
      .ToList();
  }

  private static PitSpotOffsets LoadEmbedded()
  {
    using var stream = typeof(PitSpots).Assembly.GetManifestResourceStream(ResourceName)
      ?? throw new InvalidOperationException($"Embedded resource {ResourceName} not found.");
    using var reader = new StreamReader(stream);

    return Parse(reader.ReadToEnd());
  }
}
